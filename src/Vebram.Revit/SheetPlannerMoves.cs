using Autodesk.Revit.DB;

namespace Vebram.Revit;

public static partial class SheetPlanner
{
 static void ApplyPreviewMoves(Document doc,List<PlannerPage> pages,List<ViewSheet> sheets,List<PlannerItem> items,Dictionary<long,ElementId> types,List<PlannerMove> moves)
 {
  var edited=new HashSet<string>();
  ViewSheet Sheet(PlannerPage page)=>sheets.Single(s=>s.SheetNumber==page.Number);
  (PlannerPage Page,PlannerDrawing Drawing) Find(long id)
  {
   foreach(var page in pages)
   {
    var drawing=page.Drawings.Find(d=>d.ViewId==id);
    if(drawing!=null)return (page,drawing);
   }
   throw new InvalidOperationException("Uma vista movida deixou de fazer parte da prévia.");
  }
  void Remove(PlannerPage page,PlannerDrawing drawing)
  {
   var sheet=Sheet(page);
   var element=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().First(e=>
    e is Viewport vp&&vp.ViewId.Value==drawing.ViewId||
    e is ScheduleSheetInstance si&&si.ScheduleId.Value==drawing.ViewId);
   DeletePlacement(doc,element);
   page.Drawings.Remove(drawing);
   edited.Add(page.Number);
  }
  void Put(PlannerPage page,PlannerDrawing drawing,string zoneKey,double x,double y,bool center)
  {
   var zone=page.Zones?.SingleOrDefault(z=>z.Key==zoneKey&&z.Writable)
    ??throw new InvalidOperationException("O destino está numa área reservada.");
   var sheet=Sheet(page);
   var element=Place(doc,sheet,items.Single(i=>i.ViewId==drawing.ViewId),types);
   if(element is Viewport vp)AssignDetailNumber(doc,vp,1);
   doc.Regenerate();
   var rect=Bounds(element,sheet);
   var width=rect.Right-rect.Left;
   var height=rect.Top-rect.Bottom;
   if(center&&width<=zone.Rect.Right-zone.Rect.Left&&height<=zone.Rect.Top-zone.Rect.Bottom)
   {
    x=Math.Clamp(x-width/2,zone.Rect.Left,zone.Rect.Right-width);
    y=Math.Clamp(y+height/2,zone.Rect.Bottom+height,zone.Rect.Top);
   }
   ElementTransformUtils.MoveElement(doc,element.Id,new XYZ(x-rect.Left,y-rect.Top,0));
   doc.Regenerate();
   rect=Bounds(element,sheet);
   if(!zone.Rect.Contains(rect,1e-6)||page.Obstacles.Concat(page.Drawings.Select(d=>d.Rect)).Any(r=>rect.Intersects(r,Implantation.Feet(.006))))
    throw new InvalidOperationException(drawing.Label+": não cabe nessa posição sem sobrepor outro conteúdo. Tente outra área ou escala.");
   DrawingOwnership.MarkAnnotation(element);
   page.Drawings.Add(drawing with{Rect=rect,Zone=zoneKey});
   edited.Add(page.Number);
  }
  foreach(var move in moves)
  {
   var (from,drawing)=Find(move.ViewId);
   if(move.SwapWith is long otherId)
   {
    if(otherId==move.ViewId)continue;
    var (to,other)=Find(otherId);
    Remove(from,drawing);
    Remove(to,other);
    Put(to,drawing,other.Zone,other.Rect.Left,other.Rect.Top,false);
    Put(from,other,drawing.Zone,drawing.Rect.Left,drawing.Rect.Top,false);
   }
   else
   {
    var to=pages.Find(p=>p.Number==move.TargetPage)
     ??throw new InvalidOperationException("A folha de destino mudou com o novo cálculo. Desfaça o movimento e tente novamente.");
    if(!double.IsFinite(move.X)||!double.IsFinite(move.Y)||move.X<to.Area.Left||move.X>to.Area.Right||move.Y<to.Area.Bottom||move.Y>to.Area.Top)
     throw new InvalidOperationException("Escolha uma posição na área de desenhos da folha.");
    Remove(from,drawing);
    Put(to,drawing,"desenhos",move.X,move.Y,true);
   }
  }
  for(int i=pages.Count-1;i>=0;i--)
  {
   var page=pages[i];
   if(!edited.Contains(page.Number))continue;
   var sheet=Sheet(page);
   if(page.Drawings.Count==0)
   {
    SheetNotes.ClearUnedited(doc,sheet.Id);
    pages.RemoveAt(i);
   }
   else if(page.Obstacles.Count==0)
   {
    const string name="Composição personalizada";
    sheet.Name=name;
    pages[i]=page with{Name=name};
   }
  }
 }
}
