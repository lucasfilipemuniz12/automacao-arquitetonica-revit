using Autodesk.Revit.DB;
using System.IO;
namespace Vebram.Revit;
internal static class MemoryAreasValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);
  var source=Implantation.ModelPlans(doc,true).First(v=>v.Name.StartsWith("PRE - Layout / Nível 1"));
  var first=MemoryAreas.Apply(doc,source);var second=MemoryAreas.Apply(doc,source);
  if(first.ViewId!=second.ViewId)throw new Exception("Segunda execução criou outra vista.");
  if(new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Count(v=>v.Name==MemoryAreas.ViewName)!=1)throw new Exception("Vista duplicada.");
  if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo alterado.");
  var view=(ViewPlan)doc.GetElement(new ElementId(second.ViewId));
  // Diagnostic for unexplained colours: what is visible in the view, by category, and floors that are not on the source level.
  var visible=new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType().Where(e=>e.Category!=null&&e.Category.CategoryType==CategoryType.Model).ToList();
  var diagnostic="\n\nELEMENTOS DE MODELO VISÍVEIS NA VISTA:\n"+string.Join("\n",visible.GroupBy(e=>e.Category!.Name).OrderBy(g=>g.Key).Select(g=>"• "+g.Key+": "+g.Count()))+
   "\n\nPISOS NA VISTA FORA DO NÍVEL DE ORIGEM:\n"+string.Join("\n",visible.OfType<Floor>().Where(f=>f.LevelId!=source.GenLevel.Id).Select(f=>"• "+f.Id.Value+" "+(doc.GetElement(f.GetTypeId())?.Name)+" nível "+doc.GetElement(f.LevelId)?.Name))+
   "\n\nTODOS OS PISOS: tipo | nível | área m² | oculto\n"+string.Join("\n",new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().Select(f=>"• "+f.Id.Value+" | "+doc.GetElement(f.GetTypeId())?.Name+" | "+doc.GetElement(f.LevelId)?.Name+" | "+Math.Round((f.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble()??0)*0.09290304,2)+" | "+f.IsHidden(view)));
  File.WriteAllText(Path.Combine(output,"RELATORIO.txt"),second.Report+diagnostic);
  using var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,"MEMORIA"),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2400};
  options.SetViewsAndSheets(new List<ElementId>{view.Id});doc.ExportImage(options);
  doc.Save();
 }
}
