using Autodesk.Revit.DB;
using System.IO;
using System.Text.Json;
namespace Vebram.Revit;
internal static class OpeningSchedulesValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);
  var source=Implantation.ModelPlans(doc,true).First(v=>v.Name.StartsWith("PRE - Layout / Nível 1"));
  var marks=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e.Category?.Id.Value is (long)BuiltInCategory.OST_Doors or (long)BuiltInCategory.OST_Windows).ToDictionary(e=>e.UniqueId,e=>e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString());
  var footprint=Implantation.Saved(doc)?.FootprintId??Implantation.Footprints(doc,source).First().Id.Value;
  var prepared=LegalModules.Apply(doc,new("planta",source.Id.Value,footprint));
  var planView=(ViewPlan)doc.GetElement(new ElementId(prepared.ViewId));
  var codes=marks.Values.Where(s=>!string.IsNullOrEmpty(s)).ToHashSet();
  List<TextNote> Labels()=>new FilteredElementCollector(doc,planView.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>DrawingOwnership.IsAnnotation(n)&&codes.Contains(n.Text.Trim())).ToList();
  int labels=Labels().Count;if(labels==0)throw new Exception("Nenhuma identificação de esquadria.");
  LegalModules.Apply(doc,new("planta",source.Id.Value,footprint));if(Labels().Count!=labels)throw new Exception("Identificações duplicadas ou instáveis.");
  using(var group=new TransactionGroup(doc,"Teste de proteção da identificação editada"))
  {
   group.Start();using(var tx=new Transaction(doc,"Editar identificação apenas no teste")){tx.Start();Labels().First().Text="P-EDITADO";tx.Commit();}
   bool blocked=false;try{LegalModules.Apply(doc,new("planta",source.Id.Value,footprint));}catch(InvalidOperationException){blocked=true;}
   if(!blocked)throw new Exception("Identificação editada foi sobrescrita.");group.RollBack();
  }
  File.WriteAllText(Path.Combine(output,"RELATORIO.txt"),OpeningSchedules.Apply(doc,source).Report);
  string checkedReport=OpeningSchedules.Apply(doc,source).Report;
  if(!checkedReport.Contains("P08")||!checkedReport.Contains("P13"))throw new Exception("Medidas vazias da tabela não foram identificadas.");
  var first=new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().Where(s=>s.Name==OpeningSchedules.Doors||s.Name==OpeningSchedules.Windows).Select(s=>s.Id.Value).Order().ToArray();
  File.WriteAllText(Path.Combine(output,"RELATORIO.txt"),OpeningSchedules.Apply(doc,source).Report);
  var second=new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().Where(s=>s.Name==OpeningSchedules.Doors||s.Name==OpeningSchedules.Windows).Select(s=>s.Id.Value).Order().ToArray();
  if(first.Length!=2||!first.SequenceEqual(second))throw new Exception("Quadros duplicados.");
  var items=SheetPlanner.Items(doc);if(items.Count(i=>i.Key is "portas" or "janelas")!=2)throw new Exception("Quadros ausentes do organizador.");
  var ids=items.Select(i=>i.ViewId).ToList();var plan=SheetPlanner.Preview(doc,"preferencial",ids);SheetPlanner.Apply(doc,plan);
  if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo alterado.");
  if(marks.Any(p=>(doc.GetElement(p.Key)?.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString())!=p.Value))throw new Exception("Marcas do modelo alteradas.");
  foreach(var id in first)
  {var schedule=(ViewSchedule)doc.GetElement(new ElementId(id));if(!new FilteredElementCollector(doc,schedule.Id).WhereElementIsNotElementType().OfType<FamilyInstance>().Any())throw new Exception("Tabela vazia: "+schedule.Name);}
  doc.Save();
  File.WriteAllText(Path.Combine(output,"ESQUADRIAS_021.json"),JsonSerializer.Serialize(new{Version="0.5.21",Labels=labels,EditedLabelProtected=true,MarksPreserved=true,EmptyCellsDetected=true,RepeatedWithoutDuplicates=true,PhysicalPreserved=true,PlantReport=prepared.Report,Plan=plan},new JsonSerializerOptions{WriteIndented=true}));
  foreach(var page in plan.Pages)
  {var sheet=SheetPlanner.Sheets(doc).First(s=>s.SheetNumber==page.Number);using var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,"FOLHA"),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2800};options.SetViewsAndSheets(new List<ElementId>{sheet.Id});doc.ExportImage(options);}
 }
}
