using Autodesk.Revit.DB;
namespace Vebram.Revit;
public sealed record SheetSelection(long SheetId,long ViewId,bool CreateNext=false);
public static class LegalSheets
{
 sealed class SheetFitException(string message):InvalidOperationException(message);
 public static bool IsLegalSheet(ViewSheet sheet)=>System.Text.RegularExpressions.Regex.IsMatch(sheet.SheetNumber,@"^LEG-\d{2,}$");
 public static ViewSheet CreateNext(Document doc,ViewSheet reference)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  if(!IsLegalSheet(reference))throw new InvalidOperationException("Escolha uma folha legal como referência de carimbo.");
  var title=new FilteredElementCollector(doc,reference.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().OfType<FamilyInstance>().FirstOrDefault()??throw new InvalidOperationException("A folha de referência não possui carimbo.");
  int number=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(IsLegalSheet).Select(s=>int.Parse(s.SheetNumber[4..],System.Globalization.CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max()+1;
  using var tx=new Transaction(doc,"VEBRAM — nova folha legal");tx.Start();
  var sheet=ViewSheet.Create(doc,title.GetTypeId());sheet.SheetNumber="LEG-"+number.ToString("00");sheet.Name="Projeto legal — complementos";
  foreach(var id in new[]{LegalCatalog.StageId,LegalCatalog.FunctionId,LegalCatalog.StatusId})
   if(sheet.get_Parameter(id) is {IsReadOnly:false} parameter)parameter.Set(LegalCatalog.Text(reference,id));
  doc.Regenerate();
  if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Criação da folha revertida.");
  return sheet;
 }
 public static ModuleResult Apply(Document doc,SheetSelection input)
 {
  var selected=doc.GetElement(new ElementId(input.SheetId)) as ViewSheet??throw new InvalidOperationException("Escolha a folha legal.");
  var view=doc.GetElement(new ElementId(input.ViewId)) as View??throw new InvalidOperationException("Escolha o entregável.");
  if(!IsLegalSheet(selected)||view.IsTemplate||!LegalCatalog.IsLegal(view))throw new InvalidOperationException("Escolha um entregável legal e uma folha LEG.");
  var sheets=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(IsLegalSheet).OrderBy(s=>s.Id==selected.Id?0:1).ThenBy(s=>s.SheetNumber,StringComparer.Ordinal).ToList();
  foreach(var sheet in sheets)if(new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Any(v=>v.ViewId==view.Id)||new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Any(s=>s.ScheduleId==view.Id))return new(sheet.Id.Value,0,0,"Este entregável já está na "+sheet.SheetNumber+". Sua posição foi preservada. Use Revisar composição para redistribuir o conjunto.");
  var failures=new List<string>();
  foreach(var sheet in sheets)
  {
   if(!new FilteredElementCollector(doc,sheet.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().Any())continue;
   try{return Place(doc,new(sheet.Id.Value,input.ViewId));}catch(SheetFitException ex){failures.Add(sheet.SheetNumber+": "+ex.Message);}
  }
  if(!input.CreateNext)throw new InvalidOperationException("Não há espaço nas folhas LEG existentes sem alterar escala. Use Revisar composição ou permita uma folha complementar.\n"+string.Join("\n",failures));
  return Place(doc,input);
 }
 internal static ModuleResult Place(Document doc,SheetSelection input)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  var sheet=doc.GetElement(new ElementId(input.SheetId)) as ViewSheet??throw new InvalidOperationException("Escolha a folha legal.");var view=doc.GetElement(new ElementId(input.ViewId)) as View??throw new InvalidOperationException("Escolha o entregável.");
  if(!IsLegalSheet(sheet)||view.IsTemplate||!LegalCatalog.IsLegal(view))throw new InvalidOperationException("Escolha um entregável legal e uma folha LEG.");
  if(input.CreateNext)
  {
   using var group=new TransactionGroup(doc,"VEBRAM — nova folha com entregável");group.Start();
   try{var created=CreateNext(doc,sheet);var result=Place(doc,new(created.Id.Value,input.ViewId));if(group.Assimilate()!=TransactionStatus.Committed)throw new InvalidOperationException("Nova folha revertida.");return result with{Report="As folhas LEG existentes não comportaram o entregável. Nova folha "+created.SheetNumber+" criada com o mesmo tipo de carimbo. "+result.Report};}
   catch{if(group.GetStatus()==TransactionStatus.Started)group.RollBack();throw;}
  }
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
   var candidate=usable.FindFree(occupied,rect,Implantation.Feet(.01),Implantation.Feet(.006))??throw new SheetFitException($"Sem espaço em 1:{view.Scale}: vista e título {Implantation.Meters(rect.Right-rect.Left)*1000:0} × {Implantation.Meters(rect.Top-rect.Bottom)*1000:0} mm.");
   ElementTransformUtils.MoveElement(doc,placed.Id,new(candidate.Left-rect.Left,candidate.Top-rect.Top,0));doc.Regenerate();var actual=Rect(placed,sheet)??throw new InvalidOperationException("Limites da vista indisponíveis.");
   var refreshedOccupied=existing.Where(e=>e is Viewport or ScheduleSheetInstance or TextNote).Select(e=>Rect(e,sheet)).Where(r=>r!=null).Select(r=>r!.Value).ToList();
   if(!usable.Contains(actual,.001)||refreshedOccupied.Any(r=>actual.Intersects(r,Implantation.Feet(.006))))throw new SheetFitException("A vista não coube sem sobreposição. Operação revertida.");
   DrawingOwnership.MarkAnnotation(placed);if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Colocação revertida.");return new(sheet.Id.Value,0,1,"Entregável colocado na "+sheet.SheetNumber+" sem sobrepor vistas ou quadros existentes, com escala preservada. Ao concluir as colocações, use Preparar legal > Revisar composição para revisar o conjunto V02.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
 internal static LegalPaperRect? Rect(Element e,ViewSheet sheet)
 {
  if(e is Viewport vp){var b=vp.GetBoxOutline();var label=vp.GetLabelOutline();return new(Math.Min(b.MinimumPoint.X,label.MinimumPoint.X),Math.Min(b.MinimumPoint.Y,label.MinimumPoint.Y),Math.Max(b.MaximumPoint.X,label.MaximumPoint.X),Math.Max(b.MaximumPoint.Y,label.MaximumPoint.Y));}
  var box=e.get_BoundingBox(sheet);return box==null?null:new(box.Min.X,box.Min.Y,box.Max.X,box.Max.Y);
 }
}
