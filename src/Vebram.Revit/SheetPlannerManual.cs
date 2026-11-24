using Autodesk.Revit.DB;

namespace Vebram.Revit;

public sealed record PlannerFormat(string Label,long SymbolId,LegalPaperRect Paper,List<PlannerZone> Zones);

public static partial class SheetPlanner
{
 static readonly (string Name,int Width,int Height)[] PaperSizes=
  [("A0",1189,841),("A1",841,594),("A2",594,420),("A3",420,297),("A4",297,210)];

 internal static List<PlannerFormat> Formats(Document doc)
 {
  var symbols=new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).OfClass(typeof(FamilySymbol))
   .Cast<FamilySymbol>().OrderByDescending(s=>s.FamilyName.StartsWith("ARQ",StringComparison.OrdinalIgnoreCase)).ToList();
  var formats=new List<PlannerFormat>();
  foreach(var symbol in symbols)
  {
   try
   {
    var profile=SheetZones.Read(doc,symbol);
    using var tx=new Transaction(doc,"VEBRAM — medir formato disponível");
    tx.Start();
    var sheet=ViewSheet.Create(doc,symbol.Id);
    doc.Regenerate();
    var mapped=SheetZones.Map(doc,sheet,profile);
    var width=(mapped.Paper.Right-mapped.Paper.Left)*304.8;
    var height=(mapped.Paper.Top-mapped.Paper.Bottom)*304.8;
    var size=PaperSizes.FirstOrDefault(p=>Math.Abs(Math.Max(width,height)-p.Width)<8&&Math.Abs(Math.Min(width,height)-p.Height)<8);
    if(size.Name!=null)
    {
     var label=size.Name+(width>=height?" horizontal":" vertical");
     formats.Add(new(label+" · "+symbol.FamilyName+" / "+symbol.Name,symbol.Id.Value,mapped.Paper,mapped.Zones));
    }
    tx.RollBack();
   }
   catch(Autodesk.Revit.Exceptions.ApplicationException)
   {
    // A type Revit cannot place or inspect is unavailable to the editor.
   }
  }
  return formats;
 }

 static void PlaceManualPages(Document doc,SheetPlan draft,List<ViewSheet> sheets,ViewSheet reference,
  List<PlannerItem> items,Dictionary<long,ElementId> types,Dictionary<long,TitleZoneProfile> profiles,List<PlannerPage> pages)
 {
  var placed=draft.Pages.SelectMany(p=>p.Drawings).Select(d=>d.ViewId).Order().ToList();
  if(!placed.SequenceEqual(items.Select(i=>i.ViewId).Order()))
   throw new InvalidOperationException("A prévia manual tem vistas faltando ou repetidas. Calcule a prévia de novo.");
  foreach(var desired in draft.Pages.Where(p=>p.Drawings.Count>0))
  {
   var key=desired.LayoutKey??desired.Number;
   var sheet=sheets.Find(s=>s.SheetNumber==key);
   if(sheet==null)
   {
    if(!desired.IsNew)throw new InvalidOperationException("A folha "+key+" mudou desde a prévia. Calcule novamente.");
    sheet=NewSheet(doc,reference,key);
    sheets.Add(sheet);
   }
   var title=Title(doc,sheet)??throw new InvalidOperationException(sheet.SheetNumber+": folha sem carimbo.");
   if(desired.FormatSymbolId is long symbolId&&title.GetTypeId().Value!=symbolId)
   {
    if(title.Pinned)throw new InvalidOperationException(sheet.SheetNumber+": desfixe o carimbo antes de trocar o formato.");
    if(new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Any(n=>SheetNumberStamp.IsHistory(n)))
     throw new InvalidOperationException(sheet.SheetNumber+": há uma revisão no carimbo. Remova ou reposicione o histórico antes de trocar o formato.");
    foreach(var note in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Where(SheetNumberStamp.IsNumber).ToList())
     doc.Delete(note.Id);
    title.ChangeTypeId(new ElementId(symbolId));
   }
   HideGuides(doc,sheet);
   SheetNotes.ClearUnedited(doc,sheet.Id);
   doc.Regenerate();
   var titleId=Title(doc,sheet)!.GetTypeId().Value;
   var mapped=SheetZones.Map(doc,sheet,profiles[titleId]);
   var area=mapped.Zones.First(z=>z.Key=="desenhos").Rect;
   var obstacles=PlannerObstacles(doc,sheet);
   if(desired.FormatSymbolId.HasValue&&obstacles.Any(o=>!mapped.Paper.Contains(o,.001)))
    throw new InvalidOperationException(sheet.SheetNumber+": o formato escolhido deixa conteúdo existente fora da folha.");
   var page=new PlannerPage(sheet.SheetNumber,desired.Name,desired.IsNew,area,[],obstacles,mapped.Paper,mapped.Zones,[],key,titleId);
   int detail=1;
   foreach(var drawing in desired.Drawings)
   {
    var element=Place(doc,sheet,items.Single(i=>i.ViewId==drawing.ViewId),types);
    if(element is Viewport viewport)
    {
     detail=AssignDetailNumber(doc,viewport,detail);
     if(drawing.Scale>0)LegalSheetComposition.CenterLabel(doc,viewport);
    }
    doc.Regenerate();
    var rect=Bounds(element,sheet);
    var origin=desired.SheetBounds??desired.Area;
    var x=drawing.Rect.Left-origin.Left+mapped.Paper.Left;
    var y=drawing.Rect.Top-origin.Top+mapped.Paper.Top;
    ElementTransformUtils.MoveElement(doc,element.Id,new XYZ(x-rect.Left,y-rect.Top,0));
    doc.Regenerate();
    rect=Bounds(element,sheet);
    var zone=page.Zones!.FirstOrDefault(z=>z.Key==drawing.Zone&&z.Writable);
    if(zone==null||!zone.Rect.Contains(rect,1e-6)||obstacles.Concat(page.Drawings.Select(d=>d.Rect)).Any(o=>rect.Intersects(o,Implantation.Feet(.006))))
     throw new InvalidOperationException(desired.Number+" — "+drawing.Label+": com as medidas do Revit, a vista ultrapassa a área ou sobrepõe outro conteúdo. Ajuste a posição, a escala ou o formato na prévia.");
    DrawingOwnership.MarkAnnotation(element);
    page.Drawings.Add(drawing with{Rect=rect});
   }
   if(obstacles.Count==0)
   {
    var name=CombinedName(desired.Drawings.Select(d=>items.Single(i=>i.ViewId==d.ViewId)).ToList());
    sheet.Name=name;
    page=page with{Name=name};
   }
   pages.Add(page);
  }
 }
 static List<LegalPaperRect> PlannerObstacles(Document doc,ViewSheet sheet)=>
  new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType()
   .Where(e=>e.OwnerViewId==sheet.Id&&!e.IsHidden(sheet)&&!SheetNotes.IsUnedited(e)&&!SheetNumberStamp.Generated(e)&&e.Category?.Id.Value!=(long)BuiltInCategory.OST_TitleBlocks)
   .Select(e=>LegalSheets.Rect(e,sheet)).OfType<LegalPaperRect>().ToList();
}
