using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;
public sealed record FacadeSelection(long SourceId,long FloorId,double DistanceMeters=3,double RotationDegrees=0,bool ReplaceEdited=false);
record FacadeStamp(string Source,List<ElementStamp> Elements,string Frame,double[]? MarkerPosition=null);
public static class LegalFacades
{
 static readonly Guid SchemaId=new("ba677f68-d203-43ba-a5f5-b12ac5d49f34");
 public static IReadOnlyList<ModuleResult> Apply(Document doc,FacadeSelection input)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  using var group=new TransactionGroup(doc,"VEBRAM — fachadas e recuperação de referências");group.Start();
  try{var result=ApplyCore(doc,input);if(group.Assimilate()!=TransactionStatus.Committed)throw new InvalidOperationException("Preparação das fachadas revertida.");return result;}
  catch{if(group.GetStatus()==TransactionStatus.Started)group.RollBack();throw;}
 }
 static IReadOnlyList<ModuleResult> ApplyCore(Document doc,FacadeSelection input)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  if(!double.IsFinite(input.DistanceMeters)||input.DistanceMeters<.5||input.DistanceMeters>100||!double.IsFinite(input.RotationDegrees))throw new InvalidOperationException("Informe distância entre 0,5 e 100 m e giro válido.");
  var source=doc.GetElement(new ElementId(input.SourceId)) as ViewPlan??throw new InvalidOperationException("Escolha uma planta de modelagem.");
  var floor=doc.GetElement(new ElementId(input.FloorId)) as Floor??throw new InvalidOperationException("Escolha o piso base.");
  if(!Implantation.Footprints(doc,source).Any(f=>f.Id==floor.Id))throw new InvalidOperationException("Piso fora do nível escolhido.");
  var createdPositions=new Dictionary<long,XYZ>();
  var views=LegalCatalog.Items.Where(i=>i.Key.StartsWith("fachada-")).Select(i=>(Item:i,View:LegalCatalog.Reference(LegalCatalog.Views(doc),i) as ViewSection)).ToList();
  if(views.Any(p=>p.View is {IsTemplate:true} or {Pinned:true}))throw new InvalidOperationException("Desbloqueie as fachadas fixadas antes de prepará-las.");
  if(views.Any(p=>p.View==null))
  {
   var reference=views.Select(p=>p.View).FirstOrDefault(v=>v?.ViewType==ViewType.Elevation);
   var type=reference!=null?doc.GetElement(reference.GetTypeId()) as ViewFamilyType:new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().Where(t=>t.ViewFamily==ViewFamily.Elevation).OrderBy(t=>t.Name).FirstOrDefault();
   if(type==null)throw new InvalidOperationException("O projeto precisa de um tipo de elevação nativa para criar fachadas.");
   var graphic=reference?.ViewTemplateId??new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v=>v.IsTemplate&&v.Name=="ARQ VT - FACHADAS - VEBRAM 05")?.Id??ElementId.InvalidElementId;
   using var create=new Transaction(doc,"VEBRAM — recriar fachadas ausentes");create.Start();
   var bounds=floor.get_BoundingBox(null)!;var point=new XYZ((bounds.Min.X+bounds.Max.X)/2,(bounds.Min.Y+bounds.Max.Y)/2,source.GenLevel.Elevation);
   for(int index=0;index<views.Count;index++)if(views[index].View==null)
   {
    var item=views[index].Item;var marker=ElevationMarker.CreateElevationMarker(doc,type.Id,point,LegalPreferenceStore.Read(doc).Scale);var view=marker.CreateElevation(doc,source.Id,0);createdPositions[marker.Id.Value]=point;view.Name=item.ReferenceName;
    foreach(var pair in new[]{(LegalCatalog.StageId,LegalCatalog.Stage),(LegalCatalog.FunctionId,"FACHADAS")}){var parameter=view.get_Parameter(pair.Item1);if(parameter==null||parameter.IsReadOnly)throw new InvalidOperationException("Configure os parâmetros ARQ do projeto antes de criar as fachadas.");parameter.Set(pair.Item2);}
    if(graphic!=ElementId.InvalidElementId)view.ViewTemplateId=graphic;views[index]=(item,view);
   }
   if(create.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Criação das fachadas revertida.");
  }
  var markers=new FilteredElementCollector(doc).OfClass(typeof(ElevationMarker)).Cast<ElevationMarker>().ToList();
  var previous=views.ToDictionary(p=>p.View!.Id.Value,p=>Read(p.View!));
  foreach(var (_,v) in views)
  {
   var stamp=previous[v!.Id.Value];if(stamp!=null&&stamp.Source!=source.UniqueId)throw new InvalidOperationException("Fachada usa outra planta fonte.");
   if(stamp!=null&&!input.ReplaceEdited&&(stamp.Frame!=Frame(v)||stamp.Elements.Any(s=>doc.GetElement(s.UniqueId) is not {} e||DrawingOwnership.Fingerprint(e)!=s.Fingerprint)))throw new InvalidOperationException("Fachada ou marcações editadas; confira a opção de refazê-las.");
   var own=stamp?.Elements.Select(e=>e.UniqueId).ToHashSet()??[];
   if(new FilteredElementCollector(doc,v.Id).WhereElementIsNotElementType().Any(e=>e.OwnerViewId==v.Id&&e is TextNote or Dimension or CurveElement or IndependentTag&&!own.Contains(e.UniqueId)))throw new InvalidOperationException("Fachada contém marcações manuais; revise antes de reposicionar.");
   if(v.ViewType==ViewType.Elevation)
   {
    var marker=markers.FirstOrDefault(m=>Enumerable.Range(0,4).Any(index=>m.GetViewId(index)==v.Id))??throw new InvalidOperationException("Marcador nativo da fachada não encontrado.");
    if(marker.CurrentViewCount>1&&new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Any(p=>p.ViewId==v.Id))throw new InvalidOperationException("Esta fachada já está em uma folha. Abra-a pelo Navegador do Revit; a preparação automática precisa preservar essa colocação.");
   }
  }
  var points=Implantation.GetContour(doc,floor);double z=source.GenLevel.Elevation,angle=input.RotationDegrees*Math.PI/180;
  var rotate=Transform.CreateRotation(XYZ.BasisZ,angle);var center=new XYZ((points.Min(p=>p.X)+points.Max(p=>p.X))/2,(points.Min(p=>p.Y)+points.Max(p=>p.Y))/2,z);
  var frame=BuildingFrame.Read(doc,source,floor,LegalPreferenceStore.Read(doc).IncludeWholeTerrain);
  double top=frame.Top,bottom=frame.Bottom;var results=new List<ModuleResult>();
  using var tx=new Transaction(doc,"VEBRAM — preparar quatro fachadas");tx.Start();
  try
  {
   // Shared markers are normal in Revit. Keep their views as a recoverable reserve,
   // and create independent native elevations instead of rotating all hosted views.
   for(int index=0;index<views.Count;index++)
   {
    var entry=views[index];var oldView=entry.View!;if(oldView.ViewType!=ViewType.Elevation)continue;
    var shared=markers.First(m=>Enumerable.Range(0,4).Any(i=>m.GetViewId(i)==oldView.Id));if(shared.CurrentViewCount<=1)continue;
    var template=oldView.ViewTemplateId;var scale=LegalPreferenceStore.Read(doc).Scale;
    var outward=rotate.OfVector(entry.Item.Key switch{"fachada-norte"=>XYZ.BasisY,"fachada-sul"=>-XYZ.BasisY,"fachada-leste"=>XYZ.BasisX,_=>-XYZ.BasisX});
    var position=center+outward*(points.Max(p=>(p-center).DotProduct(outward))+Implantation.Feet(input.DistanceMeters));
    var marker=ElevationMarker.CreateElevationMarker(doc,oldView.GetTypeId(),position,scale);var independent=marker.CreateElevation(doc,source.Id,0);
    // The reserve keeps original annotations, IDs and native marker intact.
    var originalName=oldView.Name;oldView.ViewTemplateId=ElementId.InvalidElementId;
    var oldStage=oldView.get_Parameter(LegalCatalog.StageId);if(oldStage==null||oldStage.IsReadOnly)throw new InvalidOperationException("Não foi possível guardar a fachada original na área de reserva; operação revertida.");
    oldStage.Set("00 RESERVA VEBRAM");oldView.Name="RESERVA - "+originalName+" - "+oldView.Id.Value;oldView.ViewTemplateId=template;
    independent.Name=originalName;
    foreach(var pair in new[]{(LegalCatalog.StageId,LegalCatalog.Stage),(LegalCatalog.FunctionId,"FACHADAS")}){var parameter=independent.get_Parameter(pair.Item1);if(parameter==null||parameter.IsReadOnly)throw new InvalidOperationException("Classificação ARQ ausente na nova elevação; operação revertida.");parameter.Set(pair.Item2);}
    independent.ViewTemplateId=template;views[index]=(entry.Item,independent);previous[independent.Id.Value]=null;markers.Add(marker);createdPositions[marker.Id.Value]=position;
   }
   foreach(var (item,targetNullable) in views)
   {
    var target=targetNullable!;var stamp=previous[target.Id.Value];if(stamp!=null){var ids=stamp.Elements.Select(s=>doc.GetElement(s.UniqueId)).Where(e=>e!=null).Select(e=>e!.Id).ToList();if(ids.Count>0){var allowed=ids.ToHashSet();if(doc.Delete(ids).Any(id=>!allowed.Contains(id)))throw new InvalidOperationException("Há elementos dependentes das marcas da fachada.");}}
    // ViewDirection points towards the observer, so a north facade uses +Y.
    var outward=rotate.OfVector(item.Key switch{"fachada-norte"=>XYZ.BasisY,"fachada-sul"=>-XYZ.BasisY,"fachada-leste"=>XYZ.BasisX,_=>-XYZ.BasisX});var forward=outward;
    double distance=points.Max(p=>(p-center).DotProduct(outward))+Implantation.Feet(input.DistanceMeters);var origin=center+outward*distance;
    var savedTemplate=target.ViewTemplateId;target.ViewTemplateId=ElementId.InvalidElementId;target.Scale=LegalPreferenceStore.Read(doc).Scale;
    var scope=target.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP);if(scope is {IsReadOnly:false})scope.Set(ElementId.InvalidElementId);
    var old=target.ViewDirection;double turn=Math.Atan2(old.X*forward.Y-old.Y*forward.X,old.DotProduct(forward));
    if(target.ViewType==ViewType.Elevation)
    {
     var marker=markers.First(m=>Enumerable.Range(0,4).Any(i=>m.GetViewId(i)==target.Id));
     XYZ placement;if(createdPositions.TryGetValue(marker.Id.Value,out var createdPlacement))placement=createdPlacement;else if(stamp?.MarkerPosition is {Length:3} savedPlacement)placement=new XYZ(savedPlacement[0],savedPlacement[1],savedPlacement[2]);else if(marker.Location is LocationPoint located)placement=located.Point;else{var bounds=marker.get_BoundingBox(source)??marker.get_BoundingBox(null)??throw new InvalidOperationException("Posição do marcador não disponível.");placement=new XYZ((bounds.Min.X+bounds.Max.X)/2,(bounds.Min.Y+bounds.Max.Y)/2,source.GenLevel.Elevation);}
     if(Math.Abs(turn)>1e-7){ // Two rotations avoid the known native 180-degree marker edge case.
      ElementTransformUtils.RotateElement(doc,marker.Id,Line.CreateBound(placement,placement+XYZ.BasisZ),turn/2);doc.Regenerate();ElementTransformUtils.RotateElement(doc,marker.Id,Line.CreateBound(placement,placement+XYZ.BasisZ),turn/2);doc.Regenerate();}
     ElementTransformUtils.MoveElement(doc,marker.Id,origin-placement);
    }
    else{var crop=LegalSections.CropElement(doc,target);if(Math.Abs(turn)>1e-7)ElementTransformUtils.RotateElement(doc,crop,Line.CreateBound(target.Origin,target.Origin+XYZ.BasisZ),turn);doc.Regenerate();ElementTransformUtils.MoveElement(doc,crop,origin-target.Origin);}
    doc.Regenerate();var actualCrop=LegalSections.CropElement(doc,target);ElementTransformUtils.MoveElement(doc,actualCrop,origin-outward*Implantation.Feet(.1)-target.CropBox.Transform.Origin);doc.Regenerate();if(target.ViewDirection.DotProduct(forward)<.999)throw new InvalidOperationException("Orientação nativa divergente em "+item.Label+": "+target.ViewDirection+", esperada "+forward+"; operação revertida.");
    var box=target.CropBox;var inverse=box.Transform.Inverse;var corners=frame.Corners.Select(inverse.OfPoint).ToList();
    double left=corners.Min(p=>p.X),right=corners.Max(p=>p.X);double depth=Implantation.Feet(input.DistanceMeters*2)+(points.Max(p=>(p-center).DotProduct(outward))-points.Min(p=>(p-center).DotProduct(outward)));
    box.Min=new(left-Implantation.Feet(1.5),corners.Min(p=>p.Y),-depth);box.Max=new(right+Implantation.Feet(1.5),corners.Max(p=>p.Y),0);target.CropBox=box;target.CropBoxActive=true;target.CropBoxVisible=false;
    target.get_Parameter(BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING).Set(1);target.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).Set(depth);
    var phase=target.get_Parameter(BuiltInParameter.VIEW_PHASE);if(phase is {IsReadOnly:false})phase.Set(source.get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId());
    var annotationCrop=target.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);if(annotationCrop is {IsReadOnly:false})annotationCrop.Set(0);
    var made=new List<Element>();var pending=new List<string>();
    doc.Regenerate();foreach(var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Where(l=>l.Elevation>=bottom&&l.Elevation<=top))
    {
     if(level.IsHidden(target))target.UnhideElements(new List<ElementId>{level.Id});
     bool native=false;using(var datum=new SubTransaction(doc))
     {
      datum.Start();try
      {
       if(level.CanBeVisibleInView(target))
       {
        var line=level.GetCurvesInView(DatumExtentType.Model,target).OfType<Line>().FirstOrDefault();
        if(line!=null)
        {
         var dir=line.Direction;var basePoint=line.GetEndPoint(0);XYZ At(double xx)=>box.Transform.OfPoint(new(xx,level.Elevation-box.Transform.Origin.Z,0));double a=(At(left-Implantation.Feet(1.3))-basePoint).DotProduct(dir),b=(At(left-Implantation.Feet(.6))-basePoint).DotProduct(dir);
         level.SetDatumExtentType(DatumEnds.End0,target,DatumExtentType.ViewSpecific);level.SetDatumExtentType(DatumEnds.End1,target,DatumExtentType.ViewSpecific);level.SetCurveInView(DatumExtentType.ViewSpecific,target,Line.CreateBound(basePoint+dir*Math.Min(a,b),basePoint+dir*Math.Max(a,b)));var bubble=dir.DotProduct(target.RightDirection)>0?DatumEnds.End0:DatumEnds.End1;level.ShowBubbleInView(bubble,target);level.HideBubbleInView(bubble==DatumEnds.End0?DatumEnds.End1:DatumEnds.End0,target);native=true;
        }
       }
       if(native)datum.Commit();else datum.RollBack();
      }
      catch(Autodesk.Revit.Exceptions.ArgumentException){if(datum.GetStatus()==TransactionStatus.Started)datum.RollBack();native=false;}
     }
     if(!native)
     {
      if(level.CanBeHidden(target))target.HideElements(new List<ElementId>{level.Id});
      made.Add(TextNote.Create(doc,target.Id,box.Transform.OfPoint(new(left-Implantation.Feet(1.3),level.Elevation-box.Transform.Origin.Z,0)),level.Name+"\n"+Implantation.Meters(level.Elevation).ToString("0.00",Implantation.Pt)+" m",LegalContext.TextType(doc).Id));pending.Add(level.Name+": indicação textual; plano nativo indisponível nesta elevação.");
     }
    }
    var note=LegalPreferenceStore.Read(doc).AdditionalNote;if(!string.IsNullOrWhiteSpace(note))made.Add(TextNote.Create(doc,target.Id,box.Transform.OfPoint(new(left,corners.Min(p=>p.Y)+Implantation.Feet(.12),0)),note,LegalContext.TextType(doc).Id));
    // Keep the original graphic template, excluding parameters this module has to control.
    if(savedTemplate!=ElementId.InvalidElementId)
    {
     string name="ARQ VT - FACHADAS - VEBRAM 05";var preset=new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v=>v.IsTemplate&&v.Name==name);if(preset==null){preset=target.CreateViewTemplate();preset.Name=name;}
     var excluded=preset.GetNonControlledTemplateParameterIds().ToHashSet();foreach(var id in preset.GetTemplateParameterIds())if(new[]{"VIS_GRAPHICS_ANNOTATION","VIEW_SCALE","VIEW_PHASE","VIEWER_CROP_REGION","VIEWER_CROP_REGION_VISIBLE","VIEWER_BOUND_FAR_CLIPPING","VIEWER_BOUND_OFFSET_FAR","VIEWER_VOLUME_OF_INTEREST_CROP"}.Contains(((BuiltInParameter)id.Value).ToString()))excluded.Add(id);preset.SetNonControlledTemplateParameterIds(excluded.ToList());target.ViewTemplateId=preset.Id;
    }
    foreach(var category in new[]{BuiltInCategory.OST_Sections,BuiltInCategory.OST_Elev}){var id=new ElementId(category);if(target.CanCategoryBeHidden(id))target.SetCategoryHidden(id,true);}
    doc.Regenerate();foreach(var e in made)DrawingOwnership.MarkAnnotation(e);Write(target,new(source.UniqueId,made.Select(e=>new ElementStamp(e.UniqueId,DrawingOwnership.Fingerprint(e))).ToList(),Frame(target),new[]{origin.X,origin.Y,origin.Z}));results.Add(new(target.Id.Value,0,made.Count,item.Label+": elevação nativa recortada com níveis laterais. Revise materiais, esquadrias e informações adicionais antes de emitir.\n"+string.Join("\n",pending)));
   }
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Operação revertida.");return results;
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
 static string Frame(ViewSection v){var b=v.CropBox;return string.Join("/",new[]{v.Origin.X,v.Origin.Y,v.Origin.Z,v.ViewDirection.X,v.ViewDirection.Y,b.Min.X,b.Min.Y,b.Max.X,b.Max.Y,v.Scale}.Select(n=>Math.Round(n,6).ToString(System.Globalization.CultureInfo.InvariantCulture)));}
 static Schema Schema(){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s!=null)return s;var b=new SchemaBuilder(SchemaId);b.SetSchemaName("VebramLegalFacades05");b.AddSimpleField("Json",typeof(string));return b.Finish();}
 static FacadeStamp? Read(View v){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s==null)return null;var e=v.GetEntity(s);return e.IsValid()?JsonSerializer.Deserialize<FacadeStamp>(e.Get<string>(s.GetField("Json"))):null;}
 static void Write(View v,FacadeStamp data){var s=Schema();var e=new Entity(s);e.Set(s.GetField("Json"),JsonSerializer.Serialize(data));v.SetEntity(e);}
}







