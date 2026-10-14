using System.IO;
using System.Globalization;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Vebram.Revit;

// Explicit development handoff job; never called by a product button or without a job.
internal static class ModelReviewExport
{
 static bool Physical(Element e)=>e is HostObject or DirectShape or Level or Grid or SpatialElement||e is FamilyInstance&&e.Category?.CategoryType==CategoryType.Model||e is CurveElement c&&c.CurveElementType==CurveElementType.ModelCurve;
 static string Stamp(Element e)
 {
  var box=e.get_BoundingBox(null);
  var points=box==null?"none":string.Join("/",new[]{box.Min.X,box.Min.Y,box.Min.Z,box.Max.X,box.Max.Y,box.Max.Z}.Select(v=>Math.Round(v,6).ToString(CultureInfo.InvariantCulture)));
  return e.UniqueId+"/"+e.GetType().Name+"/"+e.GetTypeId().Value+"/"+points;
 }
 static List<string> Snapshot(Document doc)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(Physical).Select(Stamp).Order().ToList();
 static List<View> Views(Document doc)=>new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v=>!v.IsTemplate&&v.ViewType is not (ViewType.Internal or ViewType.ProjectBrowser or ViewType.SystemBrowser or ViewType.Undefined)).ToList();
 public static void Run(Document doc,string output)
 {
  if(doc.IsWorkshared||doc.IsReadOnly||Path.GetFileName(doc.PathName)!="CASA_TERRAS_SO_MODELAGEM.rvt"||!Path.GetFullPath(Path.GetDirectoryName(doc.PathName)!).Equals(Path.GetFullPath(output),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("A exportação exige uma cópia local com o nome e pasta de entrega explícitos.");
  var before=Snapshot(doc);
  var plans=Implantation.ModelPlans(doc).Where(v=>v.Name.StartsWith("PRE - Layout /",StringComparison.Ordinal)).GroupBy(v=>v.GenLevel.Id).Select(g=>g.OrderBy(v=>v.Id.Value).First()).OrderBy(v=>v.GenLevel.Elevation).ToList();
  if(plans.Count==0)throw new InvalidOperationException("Plantas de modelagem de origem ausentes.");
  var originalViews=Views(doc).Select(v=>new {v.Name,Type=v.ViewType.ToString()}).ToList();
  var sourcePhase=plans[0].get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId();
  View3D model;
  using(var tx=new Transaction(doc,"VEBRAM — cópia só de modelagem para revisão"))
  {
   tx.Start();
   var threeType=new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(t=>t.ViewFamily==ViewFamily.ThreeDimensional);
   model=View3D.CreateIsometric(doc,threeType.Id);model.Name="MODELAGEM - Casa Terras 3D";model.get_Parameter(BuiltInParameter.VIEW_PHASE).Set(sourcePhase);model.DetailLevel=ViewDetailLevel.Fine;model.DisplayStyle=DisplayStyle.Shading;model.IsSectionBoxActive=false;
   var forward=new XYZ(-1,1,-.7).Normalize();var right=forward.CrossProduct(XYZ.BasisZ).Normalize();var up=right.CrossProduct(forward).Normalize();model.SetOrientation(new ViewOrientation3D(new XYZ(80,-80,70),up,forward));
   var keep=plans.Select(v=>v.Id).Append(model.Id).ToHashSet();
   StartingViewSettings.GetStartingViewSettings(doc).ViewId=model.Id;
   foreach(var id in Views(doc).Where(v=>!keep.Contains(v.Id)).OrderByDescending(v=>v is ViewSheet).Select(v=>v.Id).ToList())if(doc.GetElement(id)!=null)doc.Delete(id);
   foreach(var view in plans.Cast<View>().Append(model))
   {
    view.ViewTemplateId=ElementId.InvalidElementId;
    if(view is ViewPlan p){view.Name="MODELAGEM - Planta / "+p.GenLevel.Name;p.CropBoxActive=false;p.CropBoxVisible=false;}
    view.get_Parameter(LegalCatalog.StageId)?.Set("00 MODELAGEM");view.get_Parameter(LegalCatalog.FunctionId)?.Set("MODELAGEM");view.get_Parameter(LegalCatalog.StatusId)?.Set("BASE PARA REVISÃO");
    // Internal view-owned controls (for example camera/sun-path helpers) are not
    // documentation and may be undeletable. Remove only actual 2D annotation types.
    var annotations=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==view.Id&&e.ViewSpecific&&
     (e is TextNote or Dimension or IndependentTag or SpatialElementTag or DetailCurve or FilledRegion or RevisionCloud||e is FamilyInstance&&e.Category?.CategoryType==CategoryType.Annotation)).Select(e=>e.Id).ToList();
    if(annotations.Count>0)doc.Delete(annotations);
   }
   var schemas=Schema.ListSchemas().Where(s=>s.SchemaName.StartsWith("Vebram",StringComparison.OrdinalIgnoreCase)).ToList();
   foreach(var e in new FilteredElementCollector(doc).WhereElementIsNotElementType().ToList())foreach(var schema in schemas)if(e.IsValidObject&&e.GetEntity(schema).IsValid())e.DeleteEntity(schema);
   doc.Regenerate();
   if(!before.SequenceEqual(Snapshot(doc)))throw new InvalidOperationException("A limpeza alteraria a modelagem física; cópia não salva.");
   if(Views(doc).Any(v=>!keep.Contains(v.Id)))throw new InvalidOperationException("Ainda há vistas de documentação na cópia.");
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Limpeza revertida pelo Revit.");
  }
  doc.Save();
  var remaining=Views(doc);
  var elements=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(Physical).ToList();
  var report=new {SourceIsSeparateCopy=true,PhysicalElementsPreserved=true,PhysicalCount=before.Count,Sheets=remaining.Count(v=>v is ViewSheet),RemainingViews=remaining.Select(v=>new {v.Name,Type=v.ViewType.ToString(),Id=v.Id.Value}),RemovedViews=originalViews,ViewTemplatesRetained=new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Count(v=>v.IsTemplate),Categories=elements.GroupBy(e=>e.Category?.Name??e.GetType().Name).Select(g=>new {Category=g.Key,Count=g.Count()}),Elements=elements.Select(e=>new {e.UniqueId,Id=e.Id.Value,e.Name,Class=e.GetType().Name,Category=e.Category?.Name,TypeId=e.GetTypeId().Value}),Notes="Somente plantas de modelagem e 3D. Sem folhas, cortes, fachadas, tabelas ou vistas legais. Famílias, materiais, parâmetros e templates de vista mantidos como recursos; referências de entregáveis legais foram removidas."};
  File.WriteAllText(Path.Combine(output,"INVENTARIO_MODELAGEM.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
  foreach(var view in remaining)
  {
   var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,view is View3D?"CASA_3D":"PLANTA_"+view.Id.Value),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2400,ImageResolution=ImageResolution.DPI_150};
   options.SetViewsAndSheets(new List<ElementId>{view.Id});doc.ExportImage(options);
  }
 }
}
