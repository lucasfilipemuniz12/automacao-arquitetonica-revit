using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
namespace Vebram.Revit;

/// <summary>Explicit drawing refresh using only saved project settings. Model geometry is unchanged.</summary>
public static class DrawingRefinement
{
 public static string Apply(UIApplication ui)
 {
  var doc=ui.ActiveUIDocument.Document;
  var lot=Implantation.Saved(doc)??throw new InvalidOperationException("Prepare a Implantação antes de revisar os desenhos.");
  var source=(ViewPlan)doc.GetElement(new ElementId(lot.SourceId));
  var physical=SectionValidation.Physical(doc);var reports=new List<string>();
  using var group=new TransactionGroup(doc,"VEBRAM — revisar representação e memória");group.Start();
  try
  {
   reports.Add(Implantation.Apply(doc,Implantation.Plan(doc,lot with{ReplaceEdited=false})).Report);
   foreach(var key in new[]{"situacao","urbanistico"})
    if(LegalContext.Saved(doc,key) is {} saved)reports.Add(LegalContext.Apply(doc,saved with{ReplaceEdited=false}).Report);
   reports.Add(LegalModules.Apply(doc,new("planta",lot.SourceId,lot.FootprintId)).Report);
   reports.Add(MemoryAreas.Apply(doc,source).Report);
   reports.Add(OpeningSchedules.Apply(doc,source).Report);
   var plan=SheetPlanner.Preview(doc,"preferencial",SheetPlanner.Items(doc).Select(i=>i.ViewId));
   reports.Add(SheetPlanner.Apply(doc,plan).Report);
   if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new InvalidOperationException("A revisão alteraria geometria; tudo foi revertido.");
   if(group.Assimilate()!=TransactionStatus.Committed)throw new InvalidOperationException("Revisão dos desenhos revertida pelo Revit.");
   return string.Join("\n\n",reports)+"\n\nModelagem da casa e da demonstração preservada.";
  }
  catch{if(group.GetStatus()==TransactionStatus.Started)group.RollBack();throw;}
 }
}
