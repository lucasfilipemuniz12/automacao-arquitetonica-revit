using System.IO;
using System.Globalization;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;

// Explicit development handoff: fresh user template + physical model only.
internal static class ModelReviewExport
{
 static List<View> Views(Document d)=>new FilteredElementCollector(d).OfClass(typeof(View)).Cast<View>().Where(v=>v.ViewType is not (ViewType.Internal or ViewType.ProjectBrowser or ViewType.SystemBrowser or ViewType.Undefined)).ToList();
 static List<Element> Physical(Document d)=>new FilteredElementCollector(d).WhereElementIsNotElementType().Where(e=>e.Category?.CategoryType==CategoryType.Model&&!e.ViewSpecific&&
  (e is HostObject or FamilyInstance or DirectShape or Opening or Group||e.GetType().Name is "Stairs" or "Railing" or "TopographySurface" or "PropertyLine")&&
  e is not View&&e is not Level&&e is not Grid&&e is not SketchPlane&&e is not Sketch&&e is not ElevationMarker&&e is not SpatialElement&&e is not CurveElement&&e is not BasePoint&&e is not RevitLinkInstance&&
  e.GroupId==ElementId.InvalidElementId&&(e is not FamilyInstance f||f.SuperComponent==null)&&!new[]{"Camera","StairsRun","StairsLanding","StairsSupport"}.Contains(e.GetType().Name)&&
  !e.Category.Name.Contains("Câmera",StringComparison.OrdinalIgnoreCase)&&!e.Category.Name.Contains("Camera",StringComparison.OrdinalIgnoreCase)).ToList();
 static List<Level> Levels(Document d)=>new FilteredElementCollector(d).OfClass(typeof(Level)).Cast<Level>().OrderBy(l=>l.Elevation).ToList();
 static string ViewKey(View v)=>v.Name+"/"+v.ViewType+"/"+v.IsTemplate;
 static string Geometry(Element e)
 {
  var b=e.get_BoundingBox(null);
  return e.Category!.Id.Value+"/"+e.GetType().Name+"/"+(b==null?"none":string.Join("/",new[]{b.Min.X,b.Min.Y,b.Min.Z,b.Max.X,b.Max.Y,b.Max.Z}.Select(v=>Math.Round(v,5).ToString(CultureInfo.InvariantCulture))));
 }
 public static void Run(Document source,string output,Application app,string? templatePath,string? referencePath)
 {
  if(source.IsWorkshared||source.IsReadOnly||Path.GetFileName(source.PathName)!="CASA_TERRAS_BASE_TEMPLATE_V02.rvt"||!Path.GetFullPath(Path.GetDirectoryName(source.PathName)!).Equals(Path.GetFullPath(output),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Exige cópia explícita da fonte.");
  if(string.IsNullOrWhiteSpace(templatePath)||string.IsNullOrWhiteSpace(referencePath))throw new InvalidOperationException("RTE do usuário e referência atual obrigatórios.");
  var destination=Path.Combine(output,"CASA_TERRAS_TEMPLATE_V02_PARA_AUTOMACAO.rvt");
  if(File.Exists(destination))throw new InvalidOperationException("Não substituir uma entrega existente.");
  var original=Physical(source);var geometry=original.Select(Geometry).Order().ToList();
  using(var reference=app.OpenDocumentFile(referencePath))
  {try{if(!geometry.SequenceEqual(Physical(reference).Select(Geometry).Order()))throw new InvalidOperationException("Modelagem fonte difere da referência atual.");}finally{reference.Close(false);}}
  using var target=app.NewProjectDocument(templatePath);
  try
  {
   var templateViews=Views(target).Select(ViewKey).Order().ToList();
   var notes=new List<string>();
   using(var tx=new Transaction(target,"Casa modelada no template original — sem documentação"))
   {
    tx.Start();tx.SetFailureHandlingOptions(tx.GetFailureHandlingOptions().SetFailuresPreprocessor(new Warnings(notes)).SetClearAfterRollback(true));
    if(!Levels(source).Select(l=>Math.Round(l.Elevation,6)).SequenceEqual(Levels(target).Select(l=>Math.Round(l.Elevation,6))))throw new InvalidOperationException("Níveis diferentes do template.");
    var options=new CopyPasteOptions();options.SetDuplicateTypeNamesHandler(new DuplicateTypes());
    ElementTransformUtils.CopyElements(source,original.Select(e=>e.Id).ToList(),target,Transform.Identity,options);target.Regenerate();
    foreach(var boundary in new FilteredElementCollector(source).OfCategory(BuiltInCategory.OST_RoomSeparationLines).WhereElementIsNotElementType().OfType<CurveElement>())
    {
     var level=Levels(target).OrderBy(l=>Math.Abs(l.Elevation-boundary.GeometryCurve.GetEndPoint(0).Z)).First();
     var plan=Views(target).OfType<ViewPlan>().First(v=>!v.IsTemplate&&v.ViewType==ViewType.FloorPlan&&v.GenLevel.Id==level.Id);
     var curves=new CurveArray();curves.Append(boundary.GeometryCurve);var plane=SketchPlane.Create(target,Plane.CreateByNormalAndOrigin(XYZ.BasisZ,new XYZ(0,0,level.Elevation)));
     target.Create.NewRoomBoundaryLines(plane,curves,plan);
    }
    foreach(var room in new FilteredElementCollector(source).OfClass(typeof(SpatialElement)).OfType<Room>())
    {
     if(room.Location is not LocationPoint point)throw new InvalidOperationException("Ambiente fonte sem posição.");
     var sourceLevel=(Level)source.GetElement(room.LevelId);var level=Levels(target).OrderBy(l=>Math.Abs(l.Elevation-sourceLevel.Elevation)).First();
     var created=target.Create.NewRoom(level,new UV(point.Point.X,point.Point.Y));created.Name=RoomNameText.Display(room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString());created.Number=room.Number;
     var upper=source.GetElement(room.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).AsElementId()) as Level;
     if(upper!=null)created.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).Set(Levels(target).OrderBy(l=>Math.Abs(l.Elevation-upper.Elevation)).First().Id);
     foreach(var bp in new[]{BuiltInParameter.ROOM_UPPER_OFFSET,BuiltInParameter.ROOM_LOWER_OFFSET})created.get_Parameter(bp).Set(room.get_Parameter(bp).AsDouble());
    }
    // Only the two modeling plans are framed. Legal and Executive references stay original.
    foreach(var plan in Views(target).OfType<ViewPlan>().Where(v=>!v.IsTemplate&&v.Name.StartsWith("PRE - Layout /",StringComparison.Ordinal)))
    {
     var preset=plan.ViewTemplateId;plan.ViewTemplateId=ElementId.InvalidElementId;plan.CropBoxActive=false;plan.CropBoxVisible=false;plan.ViewTemplateId=preset;
    }
    target.Regenerate();
    if(!geometry.SequenceEqual(Physical(target).Select(Geometry).Order()))throw new InvalidOperationException("Geometria transferida difere da fonte.");
    var rooms=new FilteredElementCollector(target).OfClass(typeof(SpatialElement)).OfType<Room>().ToList();
    foreach(var room in new FilteredElementCollector(source).OfClass(typeof(SpatialElement)).OfType<Room>())
    {var match=rooms.Single(r=>r.Number==room.Number);if(Math.Abs(match.Area-room.Area)>.01)throw new InvalidOperationException("Área de ambiente alterada: "+room.Number);}
    if(!templateViews.SequenceEqual(Views(target).Select(ViewKey).Order()))throw new InvalidOperationException("Conjunto de vistas do template alterado.");
    if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Transferência revertida.");
   }
   var views=Views(target);
   var generatedSchemas=new FilteredElementCollector(target).WhereElementIsNotElementType().SelectMany(e=>e.GetEntitySchemaGuids()).Distinct().Select(id=>Schema.Lookup(id)?.SchemaName).Where(n=>n?.StartsWith("Vebram",StringComparison.OrdinalIgnoreCase)==true).ToList();
   if(generatedSchemas.Count>0)throw new InvalidOperationException("Template contém estado VEBRAM: "+string.Join(", ",generatedSchemas));
   target.SaveAs(destination,new SaveAsOptions{OverwriteExistingFile=false,MaximumBackups=1});
   var report=new{CreatedFromUserRte=true,TemplateViewsAndSheetsPreserved=true,PhysicalGeometryMatchesCurrentModel=true,GeneratedVebramStateAbsent=true,ModelElementCount=original.Count,Rooms=new FilteredElementCollector(target).OfClass(typeof(SpatialElement)).OfType<Room>().Count(),Views=views.Select(v=>new{v.Name,Type=v.ViewType.ToString(),v.IsTemplate,Stage=LegalCatalog.Text(v,LegalCatalog.StageId),Role=LegalCatalog.Text(v,LegalCatalog.FunctionId)}),Warnings=notes,Notes="Projeto novo do RTE atual do usuário. Somente modelagem e ambientes transferidos. Legal e Executivo preservados sem preparação da automação. Elementos recebem IDs novos pela transferência; contagens/categorias/caixas físicas e áreas de ambientes verificadas."};
   File.WriteAllText(Path.Combine(output,"INVENTARIO_BASE_TEMPLATE.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
   foreach(var view in views.Where(v=>!v.IsTemplate&&v.Name.StartsWith("PRE - Layout /",StringComparison.Ordinal)&&v is ViewPlan))Export(target,view,output,"PLANTA_"+view.Id.Value);
   using(var group=new TransactionGroup(target,"Prévia temporária para revisão"))
   {
    group.Start();
    try
    {
     View3D preview;
     using(var tx=new Transaction(target,"Vista temporária somente para PNG"))
     {
      tx.Start();var type=new FilteredElementCollector(target).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(v=>v.ViewFamily==ViewFamily.ThreeDimensional);
      preview=View3D.CreateIsometric(target,type.Id);preview.Name="PREVIA_TEMPORARIA_NAO_SALVAR";preview.DisplayStyle=DisplayStyle.Shading;preview.DetailLevel=ViewDetailLevel.Fine;preview.SetCategoryHidden(new ElementId(BuiltInCategory.OST_Levels),true);
      var forward=new XYZ(-1,1,-.7).Normalize();var right=forward.CrossProduct(XYZ.BasisZ).Normalize();preview.SetOrientation(new ViewOrientation3D(new XYZ(80,-80,70),right.CrossProduct(forward).Normalize(),forward));tx.Commit();
     }
     Export(target,preview,output,"CASA_3D");
    }
    finally{if(group.GetStatus()==TransactionStatus.Started)group.RollBack();}
   }
   if(!templateViews.SequenceEqual(Views(target).Select(ViewKey).Order()))throw new InvalidOperationException("Prévia alterou o template.");
  }
  finally{target.Close(false);}
 }
 static void Export(Document doc,View view,string output,string name)
 {
  using var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,name),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2400,ImageResolution=ImageResolution.DPI_150};options.SetViewsAndSheets(new List<ElementId>{view.Id});doc.ExportImage(options);
 }
 sealed class DuplicateTypes:IDuplicateTypeNamesHandler
 {public DuplicateTypeAction OnDuplicateTypeNamesFound(DuplicateTypeNamesHandlerArgs args)=>DuplicateTypeAction.UseDestinationTypes;}
 sealed class Warnings(List<string> notes):IFailuresPreprocessor
 {
  public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
  {
   bool errors=false;foreach(var failure in accessor.GetFailureMessages()){notes.Add(failure.GetSeverity()+": "+failure.GetDescriptionText());if(failure.GetSeverity()==FailureSeverity.Warning)accessor.DeleteWarning(failure);else errors=true;}
   return errors?FailureProcessingResult.ProceedWithRollBack:FailureProcessingResult.Continue;
  }
 }
}
