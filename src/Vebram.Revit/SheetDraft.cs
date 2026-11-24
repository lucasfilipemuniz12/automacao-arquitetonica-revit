namespace Vebram.Revit;

internal static class SheetDraft
{
 const double Gap=.006/.3048;
 internal static LegalPaperRect Bounds(PlannerPage page)=>page.SheetBounds??page.Area;
 internal static LegalPaperRect At(LegalPaperRect rect,double left,double top)=>rect.Move(left-rect.Left,top-rect.Top);
 internal static LegalPaperRect Scale(LegalPaperRect rect,int from,int to)
 {
  double ratio=(double)from/to;
  return new(rect.Left,rect.Top-(rect.Top-rect.Bottom)*ratio,rect.Left+(rect.Right-rect.Left)*ratio,rect.Top);
 }
 internal static string ZoneAt(PlannerPage page,LegalPaperRect rect,bool table)
 {
  if(table)
   foreach(var zone in page.Zones??[])
    if(zone.Writable&&(zone.Key is "quadros" or "legendas")&&zone.Rect.Contains(rect,1e-6))return zone.Key;
  return "desenhos";
 }
 internal static HashSet<long> Conflicts(PlannerPage page)
 {
  var invalid=new HashSet<long>();
  for(int i=0;i<page.Drawings.Count;i++)
  {
   var d=page.Drawings[i];
   var zone=page.Zones?.FirstOrDefault(z=>z.Key==d.Zone);
   if(!(zone?.Rect??page.Area).Contains(d.Rect,1e-6)||page.Obstacles.Any(o=>o.Intersects(d.Rect,Gap)))invalid.Add(d.ViewId);
   for(int j=i+1;j<page.Drawings.Count;j++)
   {
    var other=page.Drawings[j];
    if(!d.Rect.Intersects(other.Rect,Gap))continue;
    invalid.Add(d.ViewId);invalid.Add(other.ViewId);
   }
  }
  return invalid;
 }
 internal static PlannerPage ChangeFormat(PlannerPage page,PlannerFormat format)
 {
  var paper=Bounds(page);
  var area=format.Zones.First(z=>z.Key=="desenhos").Rect;
  var drawings=page.Drawings.Select(d=>
  {
   var oldZone=page.Zones?.FirstOrDefault(z=>z.Key==d.Zone);
   var newZone=format.Zones.FirstOrDefault(z=>z.Key==d.Zone&&z.Writable);
   if(oldZone!=null&&newZone!=null)
    return d with{Rect=d.Rect.Move(newZone.Rect.Left-oldZone.Rect.Left,newZone.Rect.Top-oldZone.Rect.Top)};
   return d with{Rect=d.Rect.Move(format.Paper.Left-paper.Left,format.Paper.Top-paper.Top),Zone="desenhos"};
  }).ToList();
  return page with{Area=area,SheetBounds=format.Paper,Zones=format.Zones,FormatSymbolId=format.SymbolId,Drawings=drawings};
 }
}
