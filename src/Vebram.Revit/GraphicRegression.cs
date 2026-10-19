using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
namespace Vebram.Revit;

internal static class GraphicRegression
{
 internal static string Run(UIApplication ui,string output)
 {
  var doc=ui.ActiveUIDocument.Document;
  if(!Path.GetFileNameWithoutExtension(doc.PathName).Contains("_DIAGNOSTICO",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use uma cópia _DIAGNOSTICO.");
  var checks=new List<string>();
  void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
  var physical=SectionValidation.Physical(doc);var registration=JsonSerializer.Serialize(ProjectRegistrationStore.Read(doc));
  using var group=new TransactionGroup(doc,"VEBRAM — regressão gráfica (revertida)");group.Start();
  try
  {
   Solid Square(double x)
   {
    var p=new[]{new XYZ(x,0,0),new XYZ(x+1,0,0),new XYZ(x+1,1,0),new XYZ(x,1,0)};
    var loop=CurveLoop.Create(p.Select((a,i)=>(Curve)Line.CreateBound(a,p[(i+1)%4])).ToList());
    return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>{loop},XYZ.BasisZ,1);
   }
   var one=Square(0);var two=Square(.5);
   Check(Math.Abs(MemoryProjection.Area(MemoryProjection.Union(one,two))-1.5*.09290304)<1e-8,"Projeções sobrepostas são unidas sem dupla contagem");
   var report=DrawingRefinement.Apply(ui);
   var ids=SheetPlanner.Items(doc).Select(i=>i.ViewId).Order().ToList();
   DrawingRefinement.Apply(ui);
   Check(ids.SequenceEqual(SheetPlanner.Items(doc).Select(i=>i.ViewId).Order()),"Regeneração completa reutiliza os entregáveis");
   var lot=Implantation.Saved(doc)!;var view=(ViewPlan)LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="memoria"))!;
   var outside=new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().Where(f=>f.LevelId==view.GenLevel.Id&&f.get_BoundingBox(null) is {} b&&!MemoryProjection.Within(b,lot)).ToList();
   Check(outside.Count>0&&outside.All(f=>f.IsHidden(view)),"Pisos da demonstração fora do lote não aparecem na memória");
   var plan=(ViewPlan)doc.GetElement(new ElementId(lot.SourceId));
   var legalPlan=(ViewPlan)LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="planta-1"))!;
   var legalCuts=LegalCatalog.Items.Where(i=>i.Role=="CORTES").Select(i=>LegalCatalog.Reference(LegalCatalog.Views(doc),i)?.Name).ToHashSet();
   Check(!new FilteredElementCollector(doc,legalPlan.Id).OfCategory(BuiltInCategory.OST_Viewers).WhereElementIsNotElementType().Any(e=>e is not View&&!legalCuts.Contains(e.Name)),"Planta exibe somente referências dos cortes legais");
   var reference=LegalCatalog.Views(doc).Single(v=>v.Name==LegalCatalog.Items.First(i=>i.Key=="urbanistico").ReferenceName);
   Check(!new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Any(v=>v.ViewId==reference.Id),"Referência urbanística vazia sai da folha e sua vista é preservada");
   using(var manualGroup=new TransactionGroup(doc,"Regressão — referência editada"))
   {
    manualGroup.Start();var sheet=SheetPlanner.Sheets(doc).First();Viewport vp;
    using(var tx=new Transaction(doc,"Referência manual apenas no teste"))
    {
     tx.Start();var note=new FilteredElementCollector(doc,reference.Id).OfClass(typeof(TextNote)).Cast<TextNote>().First();note.Text+="\nCONTEÚDO MANUAL";
     vp=Viewport.Create(doc,sheet.Id,reference.Id,XYZ.Zero);tx.Commit();
    }
    using(var tx=new Transaction(doc,"Conferir preservação")){tx.Start();Check(UrbanisticReference.RemoveUnfilledPlacement(doc)==0&&vp.IsValidObject,"Referência editada manualmente não é removida");tx.RollBack();}
    manualGroup.RollBack();
   }
   using(var hiddenGroup=new TransactionGroup(doc,"Regressão — número de vista oculta"))
   {
    hiddenGroup.Start();var sheet=SheetPlanner.Sheets(doc).First();Viewport hidden;
    using(var tx=new Transaction(doc,"Vista oculta apenas no teste"))
    {
     tx.Start();var drafting=ViewDrafting.Create(doc,reference.GetTypeId());
     TextNote.Create(doc,drafting.Id,XYZ.Zero,"REGRESSÃO — VISTA OCULTA",LegalStyles.Text(doc,2).Id);doc.Regenerate();
     hidden=Viewport.Create(doc,sheet.Id,drafting.Id,XYZ.Zero)??throw new InvalidOperationException("Não foi possível colocar a vista de teste.");
     sheet.HideElements(new List<ElementId>{hidden.Id});tx.Commit();
    }
    var preview=SheetPlanner.Preview(doc,"preferencial",SheetPlanner.Items(doc).Select(i=>i.ViewId));SheetPlanner.Apply(doc,preview);
    var numbers=new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Where(v=>v.SheetId==sheet.Id).Select(v=>v.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.AsString()).ToList();
    Check(hidden.IsValidObject&&numbers.Count==numbers.Distinct().Count(),"Numeração inclui vistas ocultas sem conflitos");hiddenGroup.RollBack();
   }
   Check(registration==JsonSerializer.Serialize(ProjectRegistrationStore.Read(doc)),"Dados e fonte completos continuam preservados no cadastro");
   Check(physical.SequenceEqual(SectionValidation.Physical(doc)),"Casa e demonstração mantêm exatamente a geometria anterior");
   var json=JsonSerializer.Serialize(new{Checks=checks,GenerationReport=report},new JsonSerializerOptions{WriteIndented=true});Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"REGRESSAO_GRAFICA_0527.json"),json);return json;
  }
  finally{group.RollBack();}
 }
}
