using Autodesk.Revit.DB;
namespace Vebram.Revit;

public sealed record PlannerItem(string Key,string Label,long ViewId,bool Schedule,long? CurrentSheetId);
public sealed record PlannerDrawing(long ViewId,string Label,int Scale,LegalPaperRect Rect);
public sealed record PlannerPage(string Number,string Name,bool IsNew,LegalPaperRect Area,List<PlannerDrawing> Drawings,List<LegalPaperRect> Obstacles);
public sealed record SheetPlan(string Profile,List<long> ViewIds,List<PlannerPage> Pages,List<string> Warnings);

/// <summary>Preview and apply run the same native measurement/layout, atomically. Preview always rolls back.</summary>
public static class SheetPlanner
{
 public static List<ViewSheet> Sheets(Document doc)=>new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(LegalSheets.IsLegalSheet).OrderBy(s=>s.SheetNumber,StringComparer.Ordinal).ToList();
 public static List<PlannerItem> Items(Document doc)
 {
  var views=LegalCatalog.Views(doc);var placements=new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().ToList();var schedules=new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().ToList();var result=new List<PlannerItem>();
  foreach(var entry in LegalCatalog.Items)
  {
   var view=LegalCatalog.Reference(views,entry);if(view==null||!LegalCatalog.IsLegal(view)||result.Any(i=>i.ViewId==view.Id.Value))continue;
   // Empty reserve views are template references, not deliverables ready to issue.
   bool prepared=view is ViewSchedule||new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType().Any(e=>e.OwnerViewId==view.Id&&DrawingOwnership.IsAnnotation(e));
   if(!prepared)continue;
   var current=placements.FirstOrDefault(p=>p.ViewId==view.Id)?.SheetId??schedules.FirstOrDefault(s=>s.ScheduleId==view.Id)?.OwnerViewId;
   result.Add(new(entry.Key,entry.Label.Replace(" — preencher",""),view.Id.Value,view is ViewSchedule,current?.Value));
  }
  return result;
 }
 public static SheetPlan Preview(Document doc,string profile,IEnumerable<long> ids)=>Run(doc,profile,ids.Distinct().ToList(),null);
 public static ModuleResult Apply(Document doc,SheetPlan plan)
 {
  var result=Run(doc,plan.Profile,plan.ViewIds,plan);
  var first=Sheets(doc).First(s=>s.SheetNumber==result.Pages[0].Number);
  return new(first.Id.Value,0,result.Pages.Sum(p=>p.Drawings.Count),$"{result.Pages.Count} folha(s) organizada(s); {result.Pages.Count(p=>p.IsNew)} nova(s). Escalas preservadas. Conteúdo manual mantido.\n"+string.Join("\n",result.Warnings));
 }
 static SheetPlan Run(Document doc,string profile,List<long> ids,SheetPlan? expected)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  if(profile is not ("preferencial" or "r20"))throw new ArgumentException("Padrão de folhas desconhecido.");
  var items=Items(doc).Where(i=>ids.Contains(i.ViewId)).ToList();if(items.Count==0||items.Count!=ids.Count)throw new InvalidOperationException("Selecione entregáveis preparados e gere uma nova prévia.");
  var sheets=Sheets(doc);var reference=sheets.FirstOrDefault(s=>Title(doc,s)!=null)??throw new InvalidOperationException("É necessária uma folha LEG com carimbo para usar como referência.");
  var types=new Dictionary<long,ElementId>();var scales=items.Where(i=>!i.Schedule).ToDictionary(i=>i.ViewId,i=>((View)doc.GetElement(new ElementId(i.ViewId))).Scale);
  var pages=new List<PlannerPage>();var warnings=new List<string>();var initialNumbers=sheets.Select(s=>s.SheetNumber).ToHashSet();
  using var tx=new Transaction(doc,expected==null?"VEBRAM — calcular prévia (revertida)":"VEBRAM — distribuir folhas automaticamente");tx.Start();
  try
  {
   // Remove only placements of the selected deliverables, never the views themselves.
   foreach(var item in items)
   {
    foreach(var vp in new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Where(v=>v.ViewId.Value==item.ViewId).ToList())
    {
     if(doc.GetElement(vp.SheetId) is not ViewSheet sh||!LegalSheets.IsLegalSheet(sh))throw new InvalidOperationException(item.Label+": está em folha fora do conjunto LEG.");
     if(vp.Pinned)throw new InvalidOperationException(item.Label+": desfixe a vista antes de organizar.");types[item.ViewId]=vp.GetTypeId();DeletePlacement(doc,vp);
    }
    foreach(var si in new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Where(s=>s.ScheduleId.Value==item.ViewId).ToList())
    {if(doc.GetElement(si.OwnerViewId) is not ViewSheet sh||!LegalSheets.IsLegalSheet(sh))continue;if(si.Pinned)throw new InvalidOperationException(item.Label+": tabela fixada.");if(((ViewSchedule)doc.GetElement(si.ScheduleId)).IsSplit())throw new InvalidOperationException(item.Label+": reúna os segmentos da tabela antes de organizar.");DeletePlacement(doc,si);}
   }
   doc.Regenerate();
   // Real native bounds include annotation and viewport titles, measured in a temporary sheet.
   var measuring=NewSheet(doc,reference,"VEBRAM-TEMP-"+Guid.NewGuid().ToString("N"));var sizes=new Dictionary<long,PaperItem>();
   foreach(var item in items)
   {
    var e=Place(doc,measuring,item,types);doc.Regenerate();var r=Bounds(e,measuring);sizes[item.ViewId]=new(item.ViewId,r.Right-r.Left,r.Top-r.Bottom);DeletePlacement(doc,e);
   }
   doc.Delete(measuring.Id);doc.Regenerate();
   int extra=Math.Max(4,sheets.Select(s=>int.Parse(s.SheetNumber[4..])).DefaultIfEmpty(0).Max())+1;
   var reusable=new Queue<int>(sheets.Select(s=>int.Parse(s.SheetNumber[4..])).Where(n=>n>4).Order());
   int NextExtra()=>reusable.Count>0?reusable.Dequeue():extra++;
   foreach(var grouping in items.GroupBy(i=>SheetPacking.PreferredSheet(i.Key,profile)).OrderBy(g=>g.Key))
   {
    int group=grouping.Key;var remaining=grouping.ToList();int groupPageCount=0;
    ViewSheet GetSheet(int n){string number="LEG-"+n.ToString("00");var s=sheets.FirstOrDefault(s=>s.SheetNumber==number);if(s==null){s=NewSheet(doc,reference,number);sheets.Add(s);}return s;}
    var target=GetSheet(group);
    while(remaining.Count>0)
    {
     HideGuides(doc,target);doc.Regenerate();var area=LegalSheetComposition.Area(doc,target);
     var obstacles=new FilteredElementCollector(doc,target.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==target.Id&&!e.IsHidden(target)&&e.Category?.Id.Value!=(long)BuiltInCategory.OST_TitleBlocks).Select(e=>LegalSheets.Rect(e,target)).Where(r=>r!=null).Select(r=>r!.Value).ToList();
     var batch=remaining.ToList();var layout=PreferredLayout(group,area,obstacles,batch,sizes);
     if(layout==null)
     {
      // If the entire group fits on a clean sheet, retain the group and bypass a blocked base sheet.
      if(obstacles.Count>0&&SheetPacking.Pack(area,[],batch.Select(i=>sizes[i.ViewId]).ToList(),Implantation.Feet(.012))!=null){target=GetSheet(NextExtra());continue;}
      batch=[];layout=[];
      var pair=remaining.Where(i=>group==3?i.Key is "corte-a" or "corte-b":group==1&&profile=="preferencial"&&i.Key is "implantacao" or "situacao").ToList();
      if(pair.Count==2){var together=SheetPacking.Pack(area,obstacles,pair.Select(i=>sizes[i.ViewId]).ToList(),Implantation.Feet(.012));if(together!=null){batch=pair;layout=together;}else if(obstacles.Count>0&&SheetPacking.Pack(area,[],pair.Select(i=>sizes[i.ViewId]).ToList(),Implantation.Feet(.012))!=null){target=GetSheet(NextExtra());continue;}}
      foreach(var item in remaining.Where(i=>!batch.Contains(i)).OrderByDescending(i=>sizes[i.ViewId].Width*sizes[i.ViewId].Height))
      {var trial=SheetPacking.Pack(area,obstacles,batch.Append(item).Select(i=>sizes[i.ViewId]).ToList(),Implantation.Feet(.012));if(trial!=null){batch.Add(item);layout=trial;}}
      if(batch.Count==0)
      {
       if(obstacles.Count>0){target=GetSheet(NextExtra());continue;}
       var big=remaining[0];throw new InvalidOperationException($"{big.Label} não cabe no carimbo, incluindo o título, em 1:{((View)doc.GetElement(new ElementId(big.ViewId))).Scale}. Escolha um formato maior ou revise a vista; a escala não será reduzida.");
      }
     }
     var page=new PlannerPage(target.SheetNumber,SheetPacking.SheetName(group,profile)+(groupPageCount>0?" — continuação":""),!initialNumbers.Contains(target.SheetNumber),area,[],obstacles);
     int detail=1;
     foreach(var item in batch)
     {
      var e=Place(doc,target,item,types);if(e is Viewport vp){var parameter=vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);while(new FilteredElementCollector(doc,target.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Any(v=>v.Id!=vp.Id&&v.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.AsString()==detail.ToString()))detail++;parameter?.Set((detail++).ToString());LegalSheetComposition.CenterLabel(doc,vp);}
      doc.Regenerate();var actual=Bounds(e,target);var dest=layout!.First(p=>p.Id==item.ViewId).Rect;ElementTransformUtils.MoveElement(doc,e.Id,new XYZ(dest.Left-actual.Left,dest.Top-actual.Top,0));doc.Regenerate();actual=Bounds(e,target);
      if(!area.Contains(actual,1e-6)||obstacles.Concat(page.Drawings.Select(d=>d.Rect)).Any(o=>actual.Intersects(o,Implantation.Feet(.006))))throw new InvalidOperationException("A posição real divergiu do cálculo em "+target.SheetNumber+". Nada foi aplicado.");
      DrawingOwnership.MarkAnnotation(e);page.Drawings.Add(new(item.ViewId,item.Label,item.Schedule?0:scales[item.ViewId],actual));remaining.Remove(item);
     }
     // Never rename sheets that also contain unrelated manual material.
     if(obstacles.Count==0)target.Name=page.Name;pages.Add(page);groupPageCount++;
     if(remaining.Count>0)target=GetSheet(NextExtra());
    }
    if(groupPageCount>1)warnings.Add(SheetPacking.SheetName(group,profile)+": grupo dividido em "+groupPageCount+" folhas por falta de espaço, mantendo as escalas.");
   }
   if(scales.Any(p=>((View)doc.GetElement(new ElementId(p.Key))).Scale!=p.Value))throw new InvalidOperationException("Escala alterada; operação revertida.");
   var result=new SheetPlan(profile,ids,pages,warnings);
   if(expected==null){tx.RollBack();return result;}
   if(!Equivalent(expected,result))throw new InvalidOperationException("O projeto mudou desde a prévia. Gere uma nova prévia antes de aplicar.");
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Distribuição revertida pelo Revit.");return result;
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
 static List<PaperPlacement>? PreferredLayout(int group,LegalPaperRect area,List<LegalPaperRect> obstacles,List<PlannerItem> items,Dictionary<long,PaperItem> sizes)
 {
  double gap=Implantation.Feet(.012);
  if(obstacles.Count==0)
  {
   var rows=new List<IReadOnlyList<PaperItem>>();
   if(group==3)
   {
    var plans=items.Where(i=>i.Key is "implantacao" or "cobertura").Select(i=>sizes[i.ViewId]).ToList();var cuts=items.Where(i=>i.Key is "corte-a" or "corte-b").OrderBy(i=>i.Key).Select(i=>sizes[i.ViewId]).ToList();rows.Add(plans);rows.Add(cuts);
    var aligned=SheetPacking.Rows(area,rows,gap);if(aligned!=null)return aligned;
    rows=new(){plans};rows.AddRange(cuts.Select(c=>(IReadOnlyList<PaperItem>)new[]{c}));aligned=SheetPacking.Rows(area,rows,gap);if(aligned!=null)return aligned;
   }
   if(group==4)
   {
    var ordered=new[]{"fachada-norte","fachada-sul","fachada-leste","fachada-oeste"}.SelectMany(k=>items.Where(i=>i.Key==k)).Select(i=>sizes[i.ViewId]).ToList();rows=ordered.Chunk(2).Select(r=>(IReadOnlyList<PaperItem>)r).ToList();var aligned=SheetPacking.Rows(area,rows,gap);if(aligned!=null)return aligned;
   }
  }
  return SheetPacking.Pack(area,obstacles,items.Select(i=>sizes[i.ViewId]).ToList(),gap);
 }
 static bool Equivalent(SheetPlan a,SheetPlan b)=>a.Pages.Count==b.Pages.Count&&a.Pages.Zip(b.Pages).All(p=>p.First.Number==p.Second.Number&&p.First.Drawings.Count==p.Second.Drawings.Count&&p.First.Drawings.Zip(p.Second.Drawings).All(d=>d.First.ViewId==d.Second.ViewId&&d.First.Scale==d.Second.Scale&&Math.Abs(d.First.Rect.Left-d.Second.Rect.Left)<1e-6&&Math.Abs(d.First.Rect.Top-d.Second.Rect.Top)<1e-6&&Math.Abs(d.First.Rect.Right-d.Second.Rect.Right)<1e-6&&Math.Abs(d.First.Rect.Bottom-d.Second.Rect.Bottom)<1e-6));
 static FamilyInstance? Title(Document doc,ViewSheet sheet)=>new FilteredElementCollector(doc,sheet.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().OfType<FamilyInstance>().FirstOrDefault();
 static ViewSheet NewSheet(Document doc,ViewSheet reference,string number)
 {
  var title=Title(doc,reference)!;var sheet=ViewSheet.Create(doc,title.GetTypeId());sheet.SheetNumber=number;sheet.Name="Projeto legal — complementos";
  foreach(var id in new[]{LegalCatalog.StageId,LegalCatalog.FunctionId,LegalCatalog.StatusId})if(sheet.get_Parameter(id) is {IsReadOnly:false} p)p.Set(LegalCatalog.Text(reference,id));doc.Regenerate();return sheet;
 }
 static Element Place(Document doc,ViewSheet sheet,PlannerItem item,Dictionary<long,ElementId> types)
 {
  if(item.Schedule)return ScheduleSheetInstance.Create(doc,sheet.Id,new ElementId(item.ViewId),XYZ.Zero);
  var vp=Viewport.Create(doc,sheet.Id,new ElementId(item.ViewId),XYZ.Zero);if(types.TryGetValue(item.ViewId,out var type))vp.ChangeTypeId(type);
  vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.Set("99");doc.Regenerate();LegalSheetComposition.CenterLabel(doc,vp);return vp;
 }
 static LegalPaperRect Bounds(Element e,ViewSheet sheet)
 {var r=LegalSheets.Rect(e,sheet)??throw new InvalidOperationException("Não foi possível medir vista/título.");double p=Implantation.Feet(.002);return new(r.Left-p,r.Bottom-p,r.Right+p,r.Top+p);}
 static void DeletePlacement(Document doc,Element e)
 {var id=e.Id;if(doc.Delete(id).Any(d=>d!=id))throw new InvalidOperationException("Dependências impedem mover a vista com segurança.");}
 static void HideGuides(Document doc,ViewSheet sheet)
 {
  var ids=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==sheet.Id&&!e.IsHidden(sheet)&&e.CanBeHidden(sheet)&&(e is CurveElement c&&c.LineStyle?.Name=="ARQ Guias de composição"||e is TextNote n&&(doc.GetElement(n.GetTypeId()).Name=="ARQ Guia de composição 3.0"||n.Text.StartsWith("BASE DE PROJETO LEGAL")||n.Text.StartsWith("00 | Folha-base - preencher")||n.Text.StartsWith("Quadros complementares: inserir")||n.Text.StartsWith("PAR-01 cerâmico 9 cm;")))).Select(e=>e.Id).ToList();if(ids.Count>0)sheet.HideElements(ids);
 }
}
