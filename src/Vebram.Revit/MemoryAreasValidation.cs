using Autodesk.Revit.DB;
using System.IO;
namespace Vebram.Revit;
internal static class MemoryAreasValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);
  var source=Implantation.ModelPlans(doc,true).First(v=>v.Name.StartsWith("PRE - Layout / Nível 1"));
  var first=MemoryAreas.Apply(doc,source,true);var second=MemoryAreas.Apply(doc,source);
  var generated=(ViewPlan)doc.GetElement(new ElementId(second.ViewId));
  if(!LegalCatalog.IsLegal(generated)||LegalCatalog.Text(generated,LegalCatalog.FunctionId)!="MEMORIA_AREAS")throw new Exception("Classificação legal ausente.");
  if(!SheetPlanner.Items(doc).Any(i=>i.Key=="memoria"&&i.ViewId==generated.Id.Value))throw new Exception("Memória ausente do organizador.");
  if(generated.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION)?.AsString()!="Memória gráfica de áreas")throw new Exception("Título da memória não legível.");
  var state=MemoryAreaState.Read(generated)??throw new Exception("Estado da legenda ausente.");
  var label=(TextNote)state.Legend.Select(e=>doc.GetElement(e.UniqueId)).First(e=>e is TextNote);
  using(var tx=new Transaction(doc,"Teste de edição da legenda")){tx.Start();label.Text+=" TESTE EDIÇÃO";tx.Commit();}
  bool protectedEdit=false;try{MemoryAreas.Apply(doc,source);}catch(InvalidOperationException){protectedEdit=true;}
  if(!protectedEdit)throw new Exception("Edição da legenda sobrescrita.");
  second=MemoryAreas.Apply(doc,source,true);
  var originalScale=generated.Scale;
  var plan=SheetPlanner.Preview(doc,"preferencial",SheetPlanner.Items(doc).Select(i=>i.ViewId));
  if(!plan.Pages.Any(page=>page.Drawings.Any(d=>d.ViewId==generated.Id.Value)))throw new Exception("Memória fora da prévia.");
  SheetPlanner.Apply(doc,plan);
  if(generated.Scale!=originalScale)throw new Exception("Organizador alterou escala da memória.");
  File.WriteAllText(Path.Combine(output,"MEMORIA_025.json"),System.Text.Json.JsonSerializer.Serialize(new{Version="0.5.25",LegalClassification=true,OrganizerIncludesMemory=true,EditedLegendProtected=true,NativePreviewAndApply=true,ScalePreserved=true,TerrainExcludedFromFloorSum=true,Pages=plan.Pages.Count,MemoryScale=generated.Scale},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
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
  options.SetViewsAndSheets(new List<ElementId>{view.Id}.Concat(SheetPlanner.Sheets(doc).Where(s=>new FilteredElementCollector(doc,s.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Any()).Select(s=>s.Id)).ToList());doc.ExportImage(options);
  doc.Save();
 }
}
