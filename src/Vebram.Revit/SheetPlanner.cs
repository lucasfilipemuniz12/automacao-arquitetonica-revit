using Autodesk.Revit.DB;
namespace Vebram.Revit;

public sealed record PlannerSheet(long Id,string Number,string Name);
public sealed record PlannerItem(string Key,string Label,long ViewId,bool Schedule,long? CurrentSheetId);
/// <summary>Chosen destination per view: view id → sheet id. Views left out are not touched.</summary>
public sealed record SheetPlan(IReadOnlyDictionary<long,long> ViewToSheet);

/// <summary>Places several legal deliverables on several LEG sheets in one command. All or nothing.</summary>
public static class SheetPlanner
{
 // Same split as the V02 composition (LegalSheetComposition.Layout), plus the optional second floor.
 static readonly Dictionary<string,string> DefaultSheetNumber=new()
 {
  ["planta-1"]="LEG-01",["planta-2"]="LEG-01",["situacao"]="LEG-01",["urbanistico"]="LEG-01",["areas"]="LEG-01",
  ["cobertura"]="LEG-02",["fachada-norte"]="LEG-02",["fachada-sul"]="LEG-02",["fachada-leste"]="LEG-02",["fachada-oeste"]="LEG-02",
  ["implantacao"]="LEG-03",["corte-a"]="LEG-03",["corte-b"]="LEG-03"
 };
 public static List<ViewSheet> Sheets(Document doc)=>new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
  .Where(LegalSheets.IsLegalSheet).OrderBy(s=>s.SheetNumber,StringComparer.Ordinal).ToList();
 public static Dictionary<string,long> Defaults(IEnumerable<ViewSheet> sheets)
 {
  var byNumber=sheets.ToDictionary(s=>s.SheetNumber,s=>s.Id.Value);var result=new Dictionary<string,long>();
  foreach(var pair in DefaultSheetNumber)if(byNumber.TryGetValue(pair.Value,out long id))result[pair.Key]=id;
  return result;
 }
 public static List<PlannerItem> Items(Document doc)
 {
  var views=LegalCatalog.Views(doc);var sheets=Sheets(doc);var items=new List<PlannerItem>();
  foreach(var item in LegalCatalog.Items)
  {
   var view=LegalCatalog.Reference(views,item);
   if(view==null||items.Any(i=>i.ViewId==view.Id.Value))continue;
   items.Add(new(item.Key,item.Label,view.Id.Value,view is ViewSchedule,CurrentSheet(doc,sheets,view)));
  }
  return items;
 }
 static long? CurrentSheet(Document doc,IEnumerable<ViewSheet> sheets,View view)
 {
  foreach(var sheet in sheets)
   if(new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Any(v=>v.ViewId==view.Id)||
      new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Any(s=>s.ScheduleId==view.Id))return sheet.Id.Value;
  return null;
 }
 public static ModuleResult Apply(Document doc,SheetPlan plan)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  var sheets=Sheets(doc);var items=Items(doc).Where(i=>plan.ViewToSheet.ContainsKey(i.ViewId)).ToList();
  var failures=new List<string>();int placed=0,moved=0,kept=0;long firstSheet=0;
  using var group=new TransactionGroup(doc,"VEBRAM — organizar folhas");group.Start();
  try
  {
   // Free the space of everything that changes sheet first, so other items can use it.
   foreach(var item in items)
   {
    long target=plan.ViewToSheet[item.ViewId];
    if(item.CurrentSheetId==target||item.CurrentSheetId is not {} origin)continue;
    try{Remove(doc,sheets.First(s=>s.Id.Value==origin),item.ViewId);}
    catch(Exception ex){failures.Add(item.Label+": "+ex.Message);}
   }
   if(failures.Count==0)
    foreach(var item in items)
    {
     long target=plan.ViewToSheet[item.ViewId];var sheet=sheets.FirstOrDefault(s=>s.Id.Value==target);
     if(sheet==null){failures.Add(item.Label+": folha não encontrada.");continue;}
     if(item.CurrentSheetId==target){kept++;continue;}
     try{LegalSheets.Place(doc,new SheetSelection(target,item.ViewId));if(item.CurrentSheetId.HasValue)moved++;else placed++;if(firstSheet==0)firstSheet=target;}
     catch(Exception ex){failures.Add(item.Label+" → "+sheet.SheetNumber+": "+ex.Message);}
    }
   if(failures.Count>0){group.RollBack();throw new InvalidOperationException("Nada foi alterado. Revise a escolha:\n"+string.Join("\n",failures));}
   if(group.Assimilate()!=TransactionStatus.Committed)throw new InvalidOperationException("Organização das folhas revertida pelo Revit.");
   if(firstSheet==0)firstSheet=sheets.FirstOrDefault()?.Id.Value??0;
   return new(firstSheet,0,placed+moved,$"{placed} colocado(s), {moved} movido(s), {kept} já estava(m) na folha escolhida. Escalas preservadas. Para alinhar e centralizar no padrão V02, use Revisar composição.");
  }
  catch{if(group.GetStatus()==TransactionStatus.Started)group.RollBack();throw;}
 }
 static void Remove(Document doc,ViewSheet sheet,long viewId)
 {
  using var tx=new Transaction(doc,"VEBRAM — retirar entregável da folha");tx.Start();
  try
  {
   foreach(var viewport in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Where(v=>v.ViewId.Value==viewId).ToList())
   {if(viewport.Pinned)throw new InvalidOperationException("a vista está fixada na "+sheet.SheetNumber+". Desfixe para mover.");LegalSheetComposition.DeleteViewport(doc,viewport);}
   foreach(var schedule in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Where(s=>s.ScheduleId.Value==viewId).ToList())
   {if(schedule.Pinned)throw new InvalidOperationException("a tabela está fixada na "+sheet.SheetNumber+". Desfixe para mover.");var id=schedule.Id;if(doc.Delete(id).Any(d=>d!=id))throw new InvalidOperationException("dependências da tabela impedem mover com segurança.");}
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("retirada revertida pelo Revit.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
}
