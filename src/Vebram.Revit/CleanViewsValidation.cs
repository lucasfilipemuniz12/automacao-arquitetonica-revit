using Autodesk.Revit.DB;
using System.IO;
namespace Vebram.Revit;
/// <summary>Generates the floor plan and the roof plan on a copy and exports both as PNG to check that navigation marks are hidden as intended.</summary>
internal static class CleanViewsValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);
  var source=Implantation.ModelPlans(doc,true).First(v=>v.Name.StartsWith("PRE - Layout / Nível 1"));
  var footprint=Implantation.Saved(doc)?.FootprintId??Implantation.Footprints(doc,source).First().Id.Value;
  var report=new List<string>();
  foreach(var key in new[]{"planta","cobertura"})
  {
   var result=LegalModules.Apply(doc,new(key,source.Id.Value,footprint));
   var view=(ViewPlan)doc.GetElement(new ElementId(result.ViewId));
   string Visible(BuiltInCategory c){var cat=doc.Settings.Categories.get_Item(c);return cat==null?"n/d":view.GetCategoryHidden(cat.Id)?"oculto":"visível";}
   report.Add(key+": elevações "+Visible(BuiltInCategory.OST_Elev)+", chamadas "+Visible(BuiltInCategory.OST_Callouts)+", cortes "+Visible(BuiltInCategory.OST_Sections));
   using var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,key.ToUpperInvariant()),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2000};
   options.SetViewsAndSheets(new List<ElementId>{view.Id});doc.ExportImage(options);
  }
  if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo alterado.");
  File.WriteAllText(Path.Combine(output,"RELATORIO.txt"),string.Join("\n",report));
  doc.Save();
 }
}
