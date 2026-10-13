using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;
public sealed record ModuleSelection(string Key,long SourceId,long FloorId=0,bool ReplaceEdited=false);
public sealed record ModuleResult(long ViewId,int Dimensions,int Annotations,string Report);
record ModuleStamp(string Source,List<ElementStamp> Elements);
public static class LegalModules
{
 static readonly Guid SchemaId=new("20b3b538-6c86-4ef5-b245-56849fbf0480");
 public static ModuleResult Apply(Document doc,ModuleSelection request)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  var source=doc.GetElement(new ElementId(request.SourceId)) as ViewPlan??throw new InvalidOperationException("Escolha a planta de modelagem.");
  if(source.IsTemplate||source.ViewType!=ViewType.FloorPlan)throw new InvalidOperationException("Fonte de planta inválida.");
  if(request.Key=="areas")
  {
   var schedule=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="areas")) as ViewSchedule??throw new InvalidOperationException("Q04 do V02 não encontrado. Use o template compatível.");
   var rooms=new FilteredElementCollector(doc).OfClass(typeof(Room)).Cast<Room>().ToList();
   return new(schedule.Id.Value,0,0,$"Q04 nativo do template: {rooms.Count(r=>r.Area>0)} ambientes com área; {rooms.Count(r=>r.Area<=0)} sem área.\nO quadro acompanha o modelo. Confira os ambientes e as pendências de modelagem antes de emitir.");
  }
  if(request.Key is not ("planta" or "cobertura"))throw new InvalidOperationException("Módulo ainda não habilitado.");
  var floor=doc.GetElement(new ElementId(request.FloorId)) as Floor??throw new InvalidOperationException("Escolha o piso base.");
  if(!Implantation.Footprints(doc,source).Any(f=>f.Id==floor.Id))throw new InvalidOperationException("O piso deve pertencer ao nível escolhido.");
  var contour=Implantation.GetContour(doc,floor);var bounds=floor.get_BoundingBox(null)!;
  var roofs=new FilteredElementCollector(doc).OfClass(typeof(RoofBase)).Cast<Element>().Where(e=>e.get_BoundingBox(null) is { } b&&b.Max.X>=bounds.Min.X&&b.Min.X<=bounds.Max.X&&b.Max.Y>=bounds.Min.Y&&b.Min.Y<=bounds.Max.Y).ToList();
  var waters=RoofWaters.Read(doc,roofs);if(request.Key=="cobertura"&&waters.Count==0)throw new InvalidOperationException("Não há águas de cobertura modeladas sobre esse piso.");
  var item=request.Key=="cobertura"?LegalCatalog.Items.First(i=>i.Key=="cobertura"):
   LegalCatalog.Items.Where(i=>i.Key is "planta-1" or "planta-2").FirstOrDefault(i=>LegalCatalog.Reference(LegalCatalog.Views(doc),i) is ViewPlan p&&p.GenLevel.Id==source.GenLevel.Id);
  if(item==null)throw new InvalidOperationException("Não há referência de planta legal do V02 para esse nível.");
  var target=LegalCatalog.Reference(LegalCatalog.Views(doc),item) as ViewPlan;
  if(target==null)throw new InvalidOperationException("Vista legal de referência ausente. Crie o projeto a partir do V02.");
  var previous=Read(target);
  if(previous!=null&&previous.Source!=source.UniqueId)throw new InvalidOperationException("Essa vista foi preparada com outra fonte. Revise a migração antes de substituir.");
  if(previous!=null&&!request.ReplaceEdited&&previous.Elements.Any(s=>doc.GetElement(s.UniqueId) is not { } e||!DrawingOwnership.IsAnnotation(e)||DrawingOwnership.Fingerprint(e)!=s.Fingerprint))
   throw new InvalidOperationException("Marcações geradas foram editadas. Marque a opção de refazê-las após conferir suas alterações.");
  using var tx=new Transaction(doc,"VEBRAM — "+item.Label);tx.Start();
  try
  {
   if(previous!=null){var ids=previous.Elements.Select(s=>doc.GetElement(s.UniqueId)).Where(e=>e!=null&&e.OwnerViewId==target.Id&&DrawingOwnership.IsAnnotation(e)).Select(e=>e!.Id).ToList();if(ids.Count>0)doc.Delete(ids);}
   var prefs=LegalPreferenceStore.Read(doc);Preset(doc,target,source,item,prefs.Scale);
   var made=new List<Element>();var textType=TextType(doc);var dimensionType=LegalPreferenceStore.Dimension(doc);
   var z=target.GenLevel.Elevation;var drawing=new Drawing(doc,target,made,textType,dimensionType,z);
   double x0=contour.Min(p=>p.X),x1=contour.Max(p=>p.X),y0=contour.Min(p=>p.Y),y1=contour.Max(p=>p.Y);
   if(request.Key=="planta")
   {
    // Linhas auxiliares nativas de referência; o contorno segue o piso e as paredes adjacentes.
    var c=drawing.Polygon(new[]{new XYZ(x0,y0,z),new XYZ(x1,y0,z),new XYZ(x1,y1,z),new XYZ(x0,y1,z)});
    drawing.Dimension(new[]{c[3],c[1]},new(x0,y0-Implantation.Feet(.8),z),new(x1,y0-Implantation.Feet(.8),z));
    drawing.Dimension(new[]{c[0],c[2]},new(x0-Implantation.Feet(.8),y0,z),new(x0-Implantation.Feet(.8),y1,z));
    foreach(var room in new FilteredElementCollector(doc).OfClass(typeof(Room)).Cast<Room>().Where(r=>r.LevelId==source.GenLevel.Id&&r.Area>0))
     if(room.Location is LocationPoint p)drawing.Text(new XYZ(p.Point.X,p.Point.Y,z),room.Name+"\n"+(room.Area*.09290304).ToString("0.00",Implantation.Pt)+" m²",true);
   }
   else
   {
    var minZ=roofs.Min(r=>r.get_BoundingBox(null)!.Min.Z);var maxZ=roofs.Max(r=>r.get_BoundingBox(null)!.Max.Z);SetRange(target,minZ,maxZ);
    int index=0;foreach(var water in waters)
    {
     index++;var point=new XYZ(water.Center.X,water.Center.Y,z);
     drawing.Text(point,"ÁGUA "+index+"\nInclinação do modelo: "+water.Slope.ToString("0.0",Implantation.Pt)+"%",true);
     if(water.Direction!=null)drawing.Arrow(point-new XYZ(0,Implantation.Feet(1),0),water.Direction);
    }
   }
   if(!string.IsNullOrWhiteSpace(prefs.AdditionalNote))drawing.Text(new(x0,y0-Implantation.Feet(2),z),prefs.AdditionalNote);
   var box=target.CropBox;var inverse=box.Transform.Inverse;
   var corners=new[]{inverse.OfPoint(new XYZ(x0-Implantation.Feet(3),y0-Implantation.Feet(4),z)),inverse.OfPoint(new XYZ(x1+Implantation.Feet(3),y1+Implantation.Feet(3),z))};
   box.Min=new XYZ(corners.Min(p=>p.X),corners.Min(p=>p.Y),box.Min.Z);box.Max=new XYZ(corners.Max(p=>p.X),corners.Max(p=>p.Y),box.Max.Z);target.CropBox=box;
   doc.Regenerate();foreach(var e in made)DrawingOwnership.MarkAnnotation(e);Write(target,new(source.UniqueId,made.Select(e=>new ElementStamp(e.UniqueId,DrawingOwnership.Fingerprint(e))).ToList()));
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Operação revertida pelo Revit.");
   return new(target.Id.Value,made.OfType<Dimension>().Count(),made.Count,item.Label+" preparada na vista do V02.\n"+made.OfType<Dimension>().Count()+" cotas e "+made.Count+" marcações geradas.\n"+
    (request.Key=="planta"?"Inclui cotas gerais e identificação de ambientes; cotas internas e tags de esquadrias serão a próxima etapa.":waters.Count+" águas planas reconhecidas; inclinações lidas da geometria, sem alterar o telhado.")+"\nExecute novamente após mudanças de modelo. Revise antes de colocar na folha.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
 static void Preset(Document doc,ViewPlan target,ViewPlan source,Deliverable item,int scale)
 {
  var name="ARQ VT - "+item.Role+" - VEBRAM 03";var preset=new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v=>v.IsTemplate&&v.Name==name);
  if(preset==null){preset=target.CreateViewTemplate();preset.Name=name;}
  var excluded=preset.GetNonControlledTemplateParameterIds().ToHashSet();var controlled=preset.GetTemplateParameterIds();
  foreach(var id in controlled)if(new[]{"PLAN_VIEW_RANGE","VIEW_SCALE","VIEWER_CROP_REGION","VIEWER_CROP_REGION_VISIBLE","VIEW_PHASE"}.Contains(((BuiltInParameter)id.Value).ToString()))excluded.Add(id);
  foreach(var guid in new[]{LegalCatalog.StageId,LegalCatalog.FunctionId})if(SharedParameterElement.Lookup(doc,guid) is { } p&&controlled.Contains(p.Id))excluded.Add(p.Id);
  preset.SetNonControlledTemplateParameterIds(excluded.ToList());target.ViewTemplateId=preset.Id;target.Scale=scale;
  var phase=target.get_Parameter(BuiltInParameter.VIEW_PHASE);if(phase is {IsReadOnly:false})phase.Set(source.get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId());
  foreach(var pair in new[]{(LegalCatalog.StageId,LegalCatalog.Stage),(LegalCatalog.FunctionId,item.Role)}){var p=target.get_Parameter(pair.Item1);if(p==null)throw new InvalidOperationException("Parâmetro ARQ ausente.");if(!LegalCatalog.Same(p.AsString(),pair.Item2)){if(p.IsReadOnly)throw new InvalidOperationException("Classificação ARQ bloqueada.");p.Set(pair.Item2);}}
 }
 static void SetRange(ViewPlan view,double min,double max)
 {using var r=view.GetViewRange();foreach(var p in new[]{PlanViewPlane.TopClipPlane,PlanViewPlane.CutPlane,PlanViewPlane.BottomClipPlane,PlanViewPlane.ViewDepthPlane})r.SetLevelId(p,view.GenLevel.Id);r.SetOffset(PlanViewPlane.TopClipPlane,max-view.GenLevel.Elevation+Implantation.Feet(1));r.SetOffset(PlanViewPlane.CutPlane,max-view.GenLevel.Elevation+Implantation.Feet(.5));r.SetOffset(PlanViewPlane.BottomClipPlane,min-view.GenLevel.Elevation-Implantation.Feet(.1));r.SetOffset(PlanViewPlane.ViewDepthPlane,min-view.GenLevel.Elevation-Implantation.Feet(.2));view.SetViewRange(r);}
 static TextNoteType TextType(Document doc)
 {const string name="ARQ - Legal 2,5 mm - VEBRAM";var type=new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault(t=>t.Name==name);if(type!=null)return type;type=(TextNoteType)new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().First().Duplicate(name);type.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(Implantation.Feet(.0025));return type;}
 static Schema Schema(){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s!=null)return s;var b=new SchemaBuilder(SchemaId);b.SetSchemaName("VebramLegalModules03");b.AddSimpleField("Json",typeof(string));return b.Finish();}
 static ModuleStamp? Read(View view){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s==null)return null;var e=view.GetEntity(s);return e.IsValid()?JsonSerializer.Deserialize<ModuleStamp>(e.Get<string>(s.GetField("Json"))):null;}
 static void Write(View view,ModuleStamp state){var s=Schema();var e=new Entity(s);e.Set(s.GetField("Json"),JsonSerializer.Serialize(state));view.SetEntity(e);}
 class Drawing(Document doc,View view,List<Element> made,TextNoteType textType,DimensionType dimensionType,double z)
 {
  DetailCurve Line(XYZ a,XYZ b){var c=doc.Create.NewDetailCurve(view,Autodesk.Revit.DB.Line.CreateBound(new(a.X,a.Y,z),new(b.X,b.Y,z)));made.Add(c);return c;}
  public List<DetailCurve> Polygon(IReadOnlyList<XYZ> p)=>p.Select((a,i)=>Line(a,p[(i+1)%p.Count])).ToList();
  public void Text(XYZ p,string value,bool center=false){var n=TextNote.Create(doc,view.Id,p,value,new TextNoteOptions(textType.Id){HorizontalAlignment=center?HorizontalTextAlignment.Center:HorizontalTextAlignment.Left});made.Add(n);}
  public void Dimension(IEnumerable<DetailCurve> curves,XYZ a,XYZ b){var refs=new ReferenceArray();foreach(var c in curves)refs.Append(c.GeometryCurve.Reference??new Reference(c));doc.Regenerate();made.Add(doc.Create.NewDimension(view,Autodesk.Revit.DB.Line.CreateBound(a,b),refs,dimensionType));}
  public void Arrow(XYZ center,XYZ d){var a=center-d*Implantation.Feet(.5);var b=center+d*Implantation.Feet(.5);Line(a,b);var cross=new XYZ(-d.Y,d.X,0);Line(b,b-d*Implantation.Feet(.2)+cross*Implantation.Feet(.1));Line(b,b-d*Implantation.Feet(.2)-cross*Implantation.Feet(.1));}
 }
}
