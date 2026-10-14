using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;

public sealed record SectionSelection(string Key,long SourceId,long FloorId,bool AlongX=true,double Position=50,bool Reverse=false,double DepthMeters=15,bool ReplaceEdited=false);
record SectionStamp(string Source,List<ElementStamp> Elements,string ViewState);

public static class LegalSections
{
 static readonly Guid SchemaId=new("4aabf71b-8772-4aa9-aa02-92f3d8c05b75");
 public static ModuleResult Apply(Document doc,SectionSelection request)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use uma cópia RVT local editável.");
  if(request.Key is not ("corte-a" or "corte-b"))throw new InvalidOperationException("Corte inválido.");
  if(!double.IsFinite(request.Position)||request.Position<5||request.Position>95||!double.IsFinite(request.DepthMeters)||request.DepthMeters<=.1||request.DepthMeters>200)throw new InvalidOperationException("Posição entre 5 e 95%; profundidade entre 0,1 e 200 m.");
  var source=doc.GetElement(new ElementId(request.SourceId)) as ViewPlan??throw new InvalidOperationException("Escolha sua planta de modelagem.");
  if(source.IsTemplate||source.ViewType!=ViewType.FloorPlan)throw new InvalidOperationException("Fonte inválida.");
  var floor=doc.GetElement(new ElementId(request.FloorId)) as Floor??throw new InvalidOperationException("Escolha o piso base.");
  if(!Implantation.Footprints(doc,source).Any(f=>f.Id==floor.Id))throw new InvalidOperationException("Piso fora do nível escolhido.");
  var item=LegalCatalog.Items.First(i=>i.Key==request.Key);
  var target=LegalCatalog.Reference(LegalCatalog.Views(doc),item) as ViewSection??throw new InvalidOperationException("Corte de referência ausente no template V02.");
  if(target.IsTemplate||target.ViewType!=ViewType.Section||target.Pinned)throw new InvalidOperationException("Vista de corte inválida ou fixada.");
  var previous=Read(target);
  if(previous!=null&&previous.Source!=source.UniqueId)throw new InvalidOperationException("O corte foi preparado com outra planta. Confira antes de mudar sua fonte.");
  if(previous!=null&&!request.ReplaceEdited&&(previous.ViewState!=ViewState(target)||previous.Elements.Any(s=>doc.GetElement(s.UniqueId) is not {} e||DrawingOwnership.Fingerprint(e)!=s.Fingerprint)))throw new InvalidOperationException("O corte ou suas marcações foram editados. Confira e marque a opção de refazê-los.");
  var registered=previous?.Elements.Select(s=>s.UniqueId).ToHashSet()??new HashSet<string>();
  var manual=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==target.Id&&e is TextNote or Dimension or CurveElement or IndependentTag).Where(e=>!registered.Contains(e.UniqueId)).ToList();
  if(manual.Count>0)throw new InvalidOperationException("Este corte contém marcações manuais. Sua posição não será alterada automaticamente. Use uma cópia de teste sem essas marcações.");
  var contour=Implantation.GetContour(doc,floor);double x0=contour.Min(p=>p.X),x1=contour.Max(p=>p.X),y0=contour.Min(p=>p.Y),y1=contour.Max(p=>p.Y);
  var center=new XYZ((x0+x1)/2,(y0+y1)/2,source.GenLevel.Elevation);
  var point=request.AlongX?new XYZ(center.X,y0+(y1-y0)*request.Position/100,center.Z):new XYZ(x0+(x1-x0)*request.Position/100,center.Y,center.Z);
  var forward=(request.AlongX?XYZ.BasisY:XYZ.BasisX)*(request.Reverse?-1:1);
  var hosts=new FilteredElementCollector(doc).WhereElementIsNotElementType().OfType<HostObject>().Where(e=>e.get_BoundingBox(null) is {} b&&b.Max.X>=x0&&b.Min.X<=x1&&b.Max.Y>=y0&&b.Min.Y<=y1).ToList();
  double minZ=source.GenLevel.Elevation-Implantation.Feet(.7),maxZ=hosts.Count>0?hosts.Max(e=>e.get_BoundingBox(null)!.Max.Z)+Implantation.Feet(.7):source.GenLevel.Elevation+Implantation.Feet(4);
  var made=new List<Element>();var pending=new List<string>();
  using var tx=new Transaction(doc,"VEBRAM — "+item.Label);tx.Start();
  try
  {
   if(previous!=null){var ids=previous.Elements.Select(s=>doc.GetElement(s.UniqueId)).Where(e=>e!=null).Select(e=>e!.Id).ToList();if(ids.Count>0){var allowed=ids.ToHashSet();if(doc.Delete(ids).Any(id=>!allowed.Contains(id)))throw new InvalidOperationException("Atualização revertida: existem elementos dependentes das marcações geradas.");}}
   var prefs=LegalPreferenceStore.Read(doc);Preset(doc,target,source,prefs.Scale);
   var cropId=CropElement(doc,target);
   var old=target.ViewDirection;double angle=Math.Atan2(old.X*forward.Y-old.Y*forward.X,old.X*forward.X+old.Y*forward.Y);
   if(Math.Abs(angle)>1e-7)ElementTransformUtils.RotateElement(doc,cropId,Line.CreateBound(target.Origin,target.Origin+XYZ.BasisZ),angle);
   doc.Regenerate();ElementTransformUtils.MoveElement(doc,cropId,point-target.Origin);doc.Regenerate();
   if(target.ViewDirection.DotProduct(forward)<.999||Math.Abs(target.UpDirection.DotProduct(XYZ.BasisZ))<.999)throw new InvalidOperationException($"{request.Key}: orientação não aceita: direção {target.ViewDirection}; vertical {target.UpDirection}. Operação revertida.");
   var box=target.CropBox;var inverse=box.Transform.Inverse;var corners=new List<XYZ>();foreach(var x in new[]{x0,x1})foreach(var y in new[]{y0,y1})foreach(var z in new[]{minZ,maxZ})corners.Add(inverse.OfPoint(new XYZ(x,y,z)));
   double left=corners.Min(p=>p.X)-Implantation.Feet(2),right=corners.Max(p=>p.X)+Implantation.Feet(1);
   box.Min=new XYZ(left,corners.Min(p=>p.Y),-Implantation.Feet(request.DepthMeters));box.Max=new XYZ(right,corners.Max(p=>p.Y),0);target.CropBox=box;target.CropBoxActive=true;target.CropBoxVisible=false;
   Set(target,BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING,1);Set(target,BuiltInParameter.VIEWER_BOUND_OFFSET_FAR,Implantation.Feet(request.DepthMeters));
   doc.Regenerate();
   var textType=TextType(doc);var transform=target.CropBox.Transform;
   XYZ At(double x,double elevation)=>transform.OfPoint(new XYZ(x,elevation-transform.Origin.Z,0));
   var levels=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Where(l=>l.Elevation>=minZ&&l.Elevation<=maxZ).OrderBy(l=>l.Elevation).ToList();
   foreach(var level in levels)
   {
    // Datum changes are local to this section; level elevations are never modified.
    try
    {
     var curve=level.GetCurvesInView(DatumExtentType.Model,target).OfType<Line>().FirstOrDefault();
     if(curve!=null){var direction=curve.Direction;var origin=curve.GetEndPoint(0);double a=(At(left+.3,level.Elevation)-origin).DotProduct(direction),b=(At(right-.3,level.Elevation)-origin).DotProduct(direction);level.SetDatumExtentType(DatumEnds.End0,target,DatumExtentType.ViewSpecific);level.SetDatumExtentType(DatumEnds.End1,target,DatumExtentType.ViewSpecific);level.SetCurveInView(DatumExtentType.ViewSpecific,target,Line.CreateBound(origin+direction*Math.Min(a,b),origin+direction*Math.Max(a,b)));level.ShowBubbleInView(DatumEnds.End0,target);}
    }
    catch(Autodesk.Revit.Exceptions.ArgumentException){pending.Add("Nível "+level.Name+": conferir extensão do símbolo nativo.");}
    var note=TextNote.Create(doc,target.Id,At(left+.4,level.Elevation+Implantation.Feet(.15)),level.Name+" · "+((level.Elevation-source.GenLevel.Elevation)*.3048).ToString("+0.00;-0.00;0.00",Implantation.Pt)+" m (relativo ao nível fonte)",textType.Id);made.Add(note);
   }
   var lower=HorizontalFaces(floor,true).Where(f=>Contains(f.Face,point)).OrderByDescending(f=>f.Face.Origin.Z).FirstOrDefault();
   var upper=hosts.Where(h=>h.Id!=floor.Id&&h is Floor or Ceiling).SelectMany(h=>HorizontalFaces(h,false)).Where(f=>lower!=null&&f.Face.Origin.Z>lower.Face.Origin.Z+Implantation.Feet(.3)&&Contains(f.Face,point)).OrderBy(f=>f.Face.Origin.Z).FirstOrDefault();
   if(lower!=null&&upper!=null)
   {
    var refs=new ReferenceArray();refs.Append(lower.Reference);refs.Append(upper.Reference);doc.Regenerate();
    var dimension=doc.Create.NewDimension(target,Line.CreateBound(At(left+Implantation.Feet(.7),lower.Face.Origin.Z),At(left+Implantation.Feet(.7),upper.Face.Origin.Z)),refs,LegalPreferenceStore.Dimension(doc));made.Add(dimension);
   }
   else pending.Add("Não foi encontrada uma dupla de faces horizontais válidas para cotar a altura livre nesta posição.");
   if(!string.IsNullOrWhiteSpace(prefs.AdditionalNote))made.Add(TextNote.Create(doc,target.Id,At(left+.4,minZ+Implantation.Feet(.15)),prefs.AdditionalNote,textType.Id));
   doc.Regenerate();foreach(var element in made)DrawingOwnership.MarkAnnotation(element);Write(target,new(source.UniqueId,made.Select(e=>new ElementStamp(e.UniqueId,DrawingOwnership.Fingerprint(e))).ToList(),ViewState(target)));
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Operação revertida pelo Revit.");
   return new(target.Id.Value,made.OfType<Dimension>().Count(),made.Count,$"{item.Label} preparado na mesma vista do V02.\nPosição: {request.Position:0.##}%; profundidade: {request.DepthMeters:0.##} m.\n{levels.Count} níveis identificados; {made.OfType<Dimension>().Count()} cota vertical entre faces reais.\n"+string.Join("\n",pending)+"\nNão há detalhamento construtivo nem cotagem completa automática. Revise o corte antes de colocá-lo na folha.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
 sealed record HorizontalFace(PlanarFace Face,Reference Reference);
 static IEnumerable<HorizontalFace> HorizontalFaces(HostObject host,bool top)
 {
  IList<Reference> refs;try{refs=top?HostObjectUtils.GetTopFaces(host):HostObjectUtils.GetBottomFaces(host);}catch(Autodesk.Revit.Exceptions.ArgumentException){yield break;}
  foreach(var reference in refs)if(host.GetGeometryObjectFromReference(reference) is PlanarFace face&&Math.Abs(face.FaceNormal.Z)>.999)yield return new(face,reference);
 }
 static bool Contains(PlanarFace face,XYZ point){var projected=face.Project(point);return projected!=null&&face.IsInside(projected.UVPoint);}
 static void Set(Element element,BuiltInParameter id,int value){var p=element.get_Parameter(id);if(p is {IsReadOnly:false})p.Set(value);else throw new InvalidOperationException("Parâmetro do corte bloqueado: "+id);}
 static void Set(Element element,BuiltInParameter id,double value){var p=element.get_Parameter(id);if(p is {IsReadOnly:false})p.Set(value);else throw new InvalidOperationException("Parâmetro do corte bloqueado: "+id);}
 static TextNoteType TextType(Document doc){const string name="ARQ - Legal 2,5 mm - VEBRAM";var type=new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault(t=>t.Name==name);if(type!=null)return type;type=(TextNoteType)new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().First().Duplicate(name);type.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(Implantation.Feet(.0025));return type;}
 static void Preset(Document doc,ViewSection view,ViewPlan source,int scale)
 {
  const string name="ARQ VT - CORTES - VEBRAM 04";var preset=new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v=>v.IsTemplate&&v.Name==name);
  if(preset==null){preset=view.CreateViewTemplate();preset.Name=name;}
  var excluded=preset.GetNonControlledTemplateParameterIds().ToHashSet();
  foreach(var id in preset.GetTemplateParameterIds())if(new[]{"VIEW_SCALE","VIEW_PHASE","VIEWER_CROP_REGION","VIEWER_CROP_REGION_VISIBLE","VIEWER_BOUND_FAR_CLIPPING","VIEWER_BOUND_OFFSET_FAR","VIEWER_VOLUME_OF_INTEREST_CROP"}.Contains(((BuiltInParameter)id.Value).ToString()))excluded.Add(id);
  preset.SetNonControlledTemplateParameterIds(excluded.ToList());view.ViewTemplateId=preset.Id;view.Scale=scale;
  SetPhase(view,source);foreach(var pair in new[]{(LegalCatalog.StageId,LegalCatalog.Stage),(LegalCatalog.FunctionId,"CORTES")}){var p=view.get_Parameter(pair.Item1);if(p==null)throw new InvalidOperationException("Parâmetro ARQ ausente no corte.");if(!LegalCatalog.Same(p.AsString(),pair.Item2)){if(p.IsReadOnly)throw new InvalidOperationException("Classificação ARQ bloqueada.");p.Set(pair.Item2);}}
 }
 static ElementId CropElement(Document doc,View view)
 {
  var matching=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e.Id!=view.Id&&e.GetTypeId()==view.GetTypeId()&&e.Name==view.Name).Select(e=>e.Id).ToList();
  if(matching.Count==1)return matching[0];
  // Resolve the crop region from its actual visibility; never infer IDs from neighboring elements.
  using var sub=new SubTransaction(doc);sub.Start();
  try
  {
   view.CropBoxVisible=false;doc.Regenerate();var hidden=new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType().ToElementIds().ToHashSet();
   view.CropBoxVisible=true;doc.Regenerate();var visible=new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType().Where(e=>!hidden.Contains(e.Id)&&e.OwnerViewId==view.Id).Select(e=>e.Id).ToList();
   sub.RollBack();if(visible.Count!=1)throw new InvalidOperationException("Não foi possível identificar com segurança o recorte do corte: "+visible.Count+" candidatos.");return visible[0];
  }
  catch{if(sub.GetStatus()==TransactionStatus.Started)sub.RollBack();throw;}
 }
 static void SetPhase(View target,View source){var p=target.get_Parameter(BuiltInParameter.VIEW_PHASE);if(p is {IsReadOnly:false})p.Set(source.get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId());}
 static string ViewState(ViewSection view){var b=view.CropBox;return string.Join("/",new[]{view.Origin.X,view.Origin.Y,view.Origin.Z,view.ViewDirection.X,view.ViewDirection.Y,b.Min.X,b.Min.Y,b.Max.X,b.Max.Y,view.Scale,view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR)?.AsDouble()??0}.Select(v=>Math.Round(v,6).ToString(System.Globalization.CultureInfo.InvariantCulture)));}
 static Schema Schema(){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s!=null)return s;var b=new SchemaBuilder(SchemaId);b.SetSchemaName("VebramLegalSections04");b.AddSimpleField("Json",typeof(string));return b.Finish();}
 static SectionStamp? Read(View view){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s==null)return null;var e=view.GetEntity(s);return e.IsValid()?JsonSerializer.Deserialize<SectionStamp>(e.Get<string>(s.GetField("Json"))):null;}
 static void Write(View view,SectionStamp state){var s=Schema();var e=new Entity(s);e.Set(s.GetField("Json"),JsonSerializer.Serialize(state));view.SetEntity(e);}
}

