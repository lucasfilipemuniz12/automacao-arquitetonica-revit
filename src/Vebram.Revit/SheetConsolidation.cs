using Autodesk.Revit.DB;
namespace Vebram.Revit;
public static partial class SheetPlanner
{
 sealed record Merge(PlannerPage Source,PlannerPage Target,List<PaperPlacement> Layout,Dictionary<long,string> Zones,double Score);
 static double RectArea(LegalPaperRect r)=>Math.Max(0,r.Right-r.Left)*Math.Max(0,r.Top-r.Bottom);
 static List<string> Consolidate(Document doc,List<PlannerPage> pages,List<PlannerItem> items,Dictionary<long,PaperItem> sizes,Dictionary<long,ElementId> types,List<ViewSheet> sheets,HashSet<string> initialNumbers,Dictionary<long,HashSet<ElementId>> newSheetDependencies,List<string> warnings)
 {
  var reserved=new List<string>();var byId=items.ToDictionary(i=>i.ViewId);
  while(pages.Count>1)
  {
   var candidates=new List<Merge>();
   foreach(var source in pages.Where(p=>p.Obstacles.Count==0))foreach(var target in pages.Where(p=>p!=source))
   {
    var all=source.Drawings.Concat(target.Drawings).Select(d=>byId[d.ViewId]).ToList();var zones=new Dictionary<long,string>();var lateral=new List<PaperPlacement>();
    foreach(var item in all.Where(i=>i.Key is "urbanistico" or "areas" or "portas" or "janelas").OrderBy(i=>i.Key=="portas"?0:i.Key=="janelas"?1:i.Key=="areas"?2:3))
    {
     foreach(var zone in (target.Zones??[]).Where(z=>z.Key is "quadros" or "legendas").OrderBy(z=>z.Key=="quadros"?0:1))
     {var trial=SheetPacking.Pack(zone.Rect,target.Obstacles.Concat(lateral.Select(l=>l.Rect)).ToList(),new[]{sizes[item.ViewId]},Implantation.Feet(.006));if(trial==null)continue;lateral.Add(trial[0]);zones[item.ViewId]=zone.Key;break;}
    }
    var main=all.Where(i=>!zones.ContainsKey(i.ViewId)).Select(i=>new GroupedPaperItem(i.ViewId,i.Key,sizes[i.ViewId].Width,sizes[i.ViewId].Height)).ToList();
    var layout=SheetPacking.Grouped(target.Area,target.Obstacles,main,Implantation.Feet(.012));if(layout==null)continue;layout.AddRange(lateral);
    // Fewer sheets first; among feasible merges, avoid one crowded sheet beside nearly empty ones.
    double score=pages.Where(p=>p!=source&&p!=target).Sum(p=>Math.Pow(p.Drawings.Where(d=>d.Zone=="desenhos").Sum(d=>RectArea(d.Rect))/RectArea(p.Area),2));
    score+=Math.Pow(main.Sum(m=>m.Width*m.Height)/RectArea(target.Area),2);
    candidates.Add(new(source,target,layout,zones,score));
   }
   var best=candidates.OrderBy(c=>c.Score).ThenBy(c=>c.Target.Number,StringComparer.Ordinal).ThenBy(c=>c.Source.Number,StringComparer.Ordinal).FirstOrDefault();if(best==null)break;
   var sourceSheet=sheets.First(s=>s.SheetNumber==best.Source.Number);var targetSheet=sheets.First(s=>s.SheetNumber==best.Target.Number);
   var mergedItems=best.Target.Drawings.Concat(best.Source.Drawings).Select(d=>byId[d.ViewId]).OrderBy(i=>i.Key.StartsWith("planta-")?0:i.Key=="implantacao"?1:i.Key=="situacao"?2:i.Key=="cobertura"?3:i.Key.StartsWith("corte-")?4:i.Key.StartsWith("fachada-")?5:6).ThenBy(i=>i.Key,StringComparer.Ordinal).ToList();
   var mergedIds=mergedItems.Select(i=>i.ViewId).ToHashSet();
   foreach(var sheet in new[]{sourceSheet,targetSheet})
   {
    foreach(var vp in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Where(v=>mergedIds.Contains(v.ViewId.Value)).ToList())DeletePlacement(doc,vp);
    foreach(var schedule in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Where(s=>mergedIds.Contains(s.ScheduleId.Value)).ToList())DeletePlacement(doc,schedule);
    SheetNotes.ClearUnedited(doc,sheet.Id);
   }
   var name=CombinedName(mergedItems);var merged=best.Target with{Name=name,Drawings=[],Notes=[]};int number=1;
   foreach(var item in mergedItems)
   {
    var element=Place(doc,targetSheet,item,types);
    if(element is Viewport viewport)
    {number=AssignDetailNumber(doc,viewport,number);if(item.Key!="urbanistico")LegalSheetComposition.CenterLabel(doc,viewport);}
    doc.Regenerate();var actual=Bounds(element,targetSheet);var destination=best.Layout.First(p=>p.Id==item.ViewId).Rect;ElementTransformUtils.MoveElement(doc,element.Id,new XYZ(destination.Left-actual.Left,destination.Top-actual.Top,0));doc.Regenerate();DrawingOwnership.MarkAnnotation(element);
    merged.Drawings.Add(new(item.ViewId,item.Label,item.Schedule||item.Key=="urbanistico"?0:((View)doc.GetElement(new ElementId(item.ViewId))).Scale,Bounds(element,targetSheet),best.Zones.GetValueOrDefault(item.ViewId,"desenhos")));
   }
   if(merged.Obstacles.Count==0)targetSheet.Name=name;SheetNotes.Draw(doc,targetSheet,merged,warnings);
   pages[pages.IndexOf(best.Target)]=merged;pages.Remove(best.Source);
   if(!initialNumbers.Contains(sourceSheet.SheetNumber))
   {
    var permitted=new HashSet<ElementId>(newSheetDependencies[sourceSheet.Id.Value]);
    permitted.UnionWith(new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==sourceSheet.Id).Select(e=>e.Id));
    if(doc.Delete(sourceSheet.Id).Any(id=>!permitted.Contains(id)))throw new InvalidOperationException("Dependências impediram retirar uma folha nova vazia.");sheets.Remove(sourceSheet);
   }
   else{sourceSheet.Name="Reserva — sem entregáveis selecionados";reserved.Add(sourceSheet.SheetNumber);}
   warnings.Add(best.Source.Number+" incorporada à "+best.Target.Number+" para aproveitar espaço; escalas e grupos de cortes/fachadas mantidos.");
  }
  pages.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Number,b.Number));return reserved;
 }
 static string CombinedName(List<PlannerItem> items)
 {
  var names=new List<string>();void Add(bool present,string name){if(present)names.Add(name);}
  Add(items.Any(i=>i.Key.StartsWith("planta-")),"Plantas");Add(items.Any(i=>i.Key=="implantacao"),"implantação");Add(items.Any(i=>i.Key=="situacao"),"situação");Add(items.Any(i=>i.Key=="cobertura"),"cobertura");Add(items.Any(i=>i.Key.StartsWith("corte-")),"cortes");Add(items.Any(i=>i.Key.StartsWith("fachada-")),"fachadas");
  var text=names.Count==0?"Quadros":names.Count==1?names[0]:string.Join(", ",names.Take(names.Count-1))+" e "+names[^1];
  // Sheet names always start with a capital letter ("Fachadas", not "fachadas").
  return char.ToUpperInvariant(text[0])+text[1..];
 }
 static void Renumber(Document doc,List<PlannerPage> pages,List<ViewSheet> sheets,List<string> reserved,List<string> warnings,NumberStampProfile? stampProfile)
 {
  // Issue all occupied LEG sheets in order, including unselected/manual sheets. Empty references
  // remain in the document, outside the issued LEG set, so they cannot create holes or inflate its total.
  bool Occupied(ViewSheet s)=>pages.Any(p=>p.Number==s.SheetNumber)||new FilteredElementCollector(doc,s.Id).WhereElementIsNotElementType().Any(e=>e is Viewport or ScheduleSheetInstance);
  var issued=sheets.Where(Occupied).OrderBy(s=>s.SheetNumber,StringComparer.Ordinal).ToList();
  var spare=sheets.Except(issued).ToList();var old=sheets.ToDictionary(s=>s.Id.Value,s=>s.SheetNumber);
  reserved.Clear();
  foreach(var s in sheets)s.SheetNumber="VEBRAM-TEMP-"+Guid.NewGuid().ToString("N");
  foreach(var s in spare){s.SheetNumber="RESERVA-"+old[s.Id.Value]+"-"+s.Id.Value;if(!reserved.Contains(s.SheetNumber))reserved.Add(s.SheetNumber);}
  for(int i=0;i<issued.Count;i++)
  {
   var s=issued[i];s.SheetNumber="LEG-"+(i+1).ToString("00");int index=pages.FindIndex(p=>p.Number==old[s.Id.Value]);
   if(stampProfile!=null)SheetNumberStamp.Draw(doc,s,stampProfile,issued.Count);
   if(index>=0)pages[index]=pages[index] with{Number=s.SheetNumber};
   foreach(var e in new Element?[]{s,Title(doc,s)})if(e!=null)foreach(Parameter p in e.Parameters)
   {
    string n=p.Definition.Name.ToLowerInvariant();
    if(p.IsReadOnly||!(n.Contains("total")&&(n.Contains("folha")||n.Contains("prancha"))||n.Contains("nº de folhas")||n.Contains("n° de folhas")))continue;
    if(p.StorageType==StorageType.String)p.Set(issued.Count.ToString("00"));else if(p.StorageType==StorageType.Integer)p.Set(issued.Count);
   }
  }
  warnings.Add("Folhas emitidas renumeradas em sequência; total "+issued.Count+". Referências vazias preservadas como RESERVA.");
  var user=SheetNotes.ReadUser(doc);var map=sheets.ToDictionary(s=>old[s.Id.Value],s=>s.SheetNumber);
  SheetNotes.SaveUser(doc,new(user.General,user.Sheets.ToDictionary(p=>map.GetValueOrDefault(p.Key,p.Key),p=>p.Value)));
  foreach(var page in pages){var s=sheets.First(s=>s.SheetNumber==page.Number);SheetNotes.ClearUnedited(doc,s.Id);page.Notes?.Clear();SheetNotes.Draw(doc,s,page,warnings);}
  pages.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Number,b.Number));
 }
 static void RefreshBounds(Document doc,List<PlannerPage> pages)
 {
  doc.Regenerate();foreach(var page in pages)
  {
   var sheet=Sheets(doc).First(s=>s.SheetNumber==page.Number);var elements=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().Where(e=>e is Viewport or ScheduleSheetInstance).ToList();
   for(int i=0;i<page.Drawings.Count;i++)
   {
    var drawing=page.Drawings[i];var element=elements.Single(e=>e is Viewport vp?vp.ViewId.Value==drawing.ViewId:((ScheduleSheetInstance)e).ScheduleId.Value==drawing.ViewId);var actual=Bounds(element,sheet);var zone=page.Zones!.First(z=>z.Key==drawing.Zone);
    if(!zone.Rect.Contains(actual,1e-6)||page.Obstacles.Any(o=>actual.Intersects(o,Implantation.Feet(.006))))throw new InvalidOperationException("A composição final ultrapassou a área livre em "+page.Number);
    page.Drawings[i]=drawing with{Rect=actual};
   }
   for(int a=0;a<page.Drawings.Count;a++)for(int b=a+1;b<page.Drawings.Count;b++)if(page.Drawings[a].Rect.Intersects(page.Drawings[b].Rect,Implantation.Feet(.006)))throw new InvalidOperationException("Sobreposição detectada ao conferir "+page.Number);
  }
 }
}
