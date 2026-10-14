using Autodesk.Revit.DB;
namespace Vebram.Revit;
public sealed record SheetSelection(long SheetId,long ViewId);
public static class LegalSheets
{
 public static ModuleResult Apply(Document doc,SheetSelection input)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  var sheet=doc.GetElement(new ElementId(input.SheetId)) as ViewSheet??throw new InvalidOperationException("Escolha a folha legal.");var view=doc.GetElement(new ElementId(input.ViewId)) as View??throw new InvalidOperationException("Escolha o entregável.");
  if(sheet.SheetNumber is not ("LEG-01" or "LEG-02")||view.IsTemplate||!LegalCatalog.IsLegal(view))throw new InvalidOperationException("Escolha um entregável legal e a folha LEG-01 ou LEG-02.");
  var existing=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().ToList();
  if(existing.OfType<Viewport>().Any(v=>v.ViewId==view.Id)||existing.OfType<ScheduleSheetInstance>().Any(s=>s.ScheduleId==view.Id))return new(sheet.Id.Value,0,0,"Este entregável já está nesta folha. Sua posição foi preservada.");
  if(view is not ViewSchedule&&!Viewport.CanAddViewToSheet(doc,sheet.Id,view.Id))throw new InvalidOperationException("Esta vista já está em outra folha ou não pode ser colocada aqui. Confira sua organização.");
  var title=existing.FirstOrDefault(e=>e.Category?.Id.Value==(long)BuiltInCategory.OST_TitleBlocks)?.get_BoundingBox(sheet)??throw new InvalidOperationException("A folha não tem carimbo com limites disponíveis.");
  double margin=Implantation.Feet(.015);var usable=new LegalPaperRect(title.Min.X+margin,title.Min.Y+margin,title.Min.X+(title.Max.X-title.Min.X)*.72-margin,title.Max.Y-margin);
  var occupied=existing.Where(e=>e is Viewport or ScheduleSheetInstance or TextNote).Select(e=>Rect(e,sheet)).Where(r=>r!=null).Select(r=>r!.Value).ToList();
  using var tx=new Transaction(doc,"VEBRAM — colocar entregável na folha");tx.Start();
  try
  {
   Element placed=view is ViewSchedule?ScheduleSheetInstance.Create(doc,sheet.Id,view.Id,new(usable.Left,usable.Top,0)):Viewport.Create(doc,sheet.Id,view.Id,new((usable.Left+usable.Right)/2,(usable.Bottom+usable.Top)/2,0));doc.Regenerate();
   var rect=Rect(placed,sheet)??throw new InvalidOperationException("Não foi possível medir a vista na folha.");
   // Grid spacing is measured on paper. Includes viewport label and preserves every existing placement.
   var candidate=usable.FindFree(occupied,rect,Implantation.Feet(.01),Implantation.Feet(.006))??throw new InvalidOperationException("Não há espaço livre na área de desenhos desta folha com a escala atual. Escolha a outra folha, revise a escala nas Preferências legais ou organize manualmente.");
   ElementTransformUtils.MoveElement(doc,placed.Id,new(candidate.Left-rect.Left,candidate.Top-rect.Top,0));doc.Regenerate();var actual=Rect(placed,sheet)??throw new InvalidOperationException("Limites da vista indisponíveis.");
   if(!usable.Contains(actual,.001)||occupied.Any(r=>actual.Intersects(r,Implantation.Feet(.006))))throw new InvalidOperationException("A vista não coube sem sobreposição. Operação revertida.");
   DrawingOwnership.MarkAnnotation(placed);if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Colocação revertida.");return new(sheet.Id.Value,0,1,"Entregável colocado sem sobrepor vistas ou quadros existentes. A faixa direita do carimbo foi reservada. Revise a composição, os espaços-guia e os textos do template antes de emitir.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
 internal static LegalPaperRect? Rect(Element e,ViewSheet sheet)
 {
  if(e is Viewport vp){var b=vp.GetBoxOutline();var label=vp.GetLabelOutline();return new(Math.Min(b.MinimumPoint.X,label.MinimumPoint.X),Math.Min(b.MinimumPoint.Y,label.MinimumPoint.Y),Math.Max(b.MaximumPoint.X,label.MaximumPoint.X),Math.Max(b.MaximumPoint.Y,label.MaximumPoint.Y));}
  var box=e.get_BoundingBox(sheet);return box==null?null:new(box.Min.X,box.Min.Y,box.Max.X,box.Max.Y);
 }
}
