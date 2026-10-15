using Autodesk.Revit.DB;
namespace Vebram.Revit;

internal sealed record TitleZoneProfile(List<PlannerZone> LocalZones,bool Recognized);
internal static class SheetZones
{
 internal static TitleZoneProfile Read(Document doc,FamilyInstance title)
 {
  var family=doc.EditFamily(title.Symbol.Family);
  try
  {
   var lines=new FilteredElementCollector(family).OfClass(typeof(CurveElement)).Cast<CurveElement>().Select(c=>c.GeometryCurve).OfType<Line>().ToList();
   var bands=lines.Where(l=>Math.Abs(l.GetEndPoint(0).Y-l.GetEndPoint(1).Y)<1e-6&&l.Length>Implantation.Feet(.15)&&l.Length<Implantation.Feet(.30)).GroupBy(l=>(Left:Math.Round(Math.Min(l.GetEndPoint(0).X,l.GetEndPoint(1).X),5),Right:Math.Round(Math.Max(l.GetEndPoint(0).X,l.GetEndPoint(1).X),5))).OrderByDescending(g=>g.Count()).FirstOrDefault();
   if(bands==null||bands.Count()<4)return new([],false);
   var ys=bands.Select(l=>l.GetEndPoint(0).Y).Append(lines.Max(l=>Math.Max(l.GetEndPoint(0).Y,l.GetEndPoint(1).Y))).Distinct().Order().ToList();var texts=new FilteredElementCollector(family).OfClass(typeof(TextNote)).Cast<TextNote>().ToList();var zones=new List<PlannerZone>();double margin=Implantation.Feet(.005);
   foreach(var spec in new[]{("notas","NOTAS GERAIS"),("quadros","QUADROS / INFORMAÇÕES"),("legendas","LEGENDAS / ATENÇÃO")})
   {
    var header=texts.FirstOrDefault(t=>t.Text.Contains(spec.Item2,StringComparison.OrdinalIgnoreCase));if(header==null)continue;
    var above=ys.Where(y=>y>=header.Coord.Y-1e-6).ToList();var below=ys.Where(y=>y<header.Coord.Y-Implantation.Feet(.015)).ToList();if(above.Count==0||below.Count==0)continue;
    double bottom=below.Max()+margin,top=Math.Min(above.Min()-margin,header.Coord.Y-header.Height-margin);
    if(top>bottom)zones.Add(new(spec.Item1,spec.Item2,new(bands.Key.Left+margin,bottom,bands.Key.Right-margin,top)));
   }
   return new(zones,zones.Any(z=>z.Key=="quadros")&&zones.Any(z=>z.Key=="notas"));
  }
  finally{family.Close(false);}
 }
 internal static (LegalPaperRect Paper,List<PlannerZone> Zones) Map(Document doc,ViewSheet sheet,TitleZoneProfile profile)
 {
  var title=new FilteredElementCollector(doc,sheet.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().OfType<FamilyInstance>().First();var box=title.get_BoundingBox(sheet);var paper=new LegalPaperRect(box.Min.X,box.Min.Y,box.Max.X,box.Max.Y);var transform=title.GetTransform();
  var zones=new List<PlannerZone>{new("desenhos","DESENHOS",LegalSheetComposition.Area(doc,sheet))};
  if(profile.Recognized)foreach(var zone in profile.LocalZones)
  {var corners=new[]{new XYZ(zone.Rect.Left,zone.Rect.Bottom,0),new XYZ(zone.Rect.Right,zone.Rect.Bottom,0),new XYZ(zone.Rect.Left,zone.Rect.Top,0),new XYZ(zone.Rect.Right,zone.Rect.Top,0)}.Select(transform.OfPoint).ToList();zones.Add(zone with{Rect=new(corners.Min(p=>p.X),corners.Min(p=>p.Y),corners.Max(p=>p.X),corners.Max(p=>p.Y))});}
  if(zones.Any(z=>z.Key=="legendas")){var lateral=zones.Where(z=>z.Key!="desenhos").ToList();zones.Add(new("identificacao","CARIMBO / HISTÓRICO",new(lateral.Min(z=>z.Rect.Left),paper.Bottom+Implantation.Feet(.01),lateral.Max(z=>z.Rect.Right),lateral.Min(z=>z.Rect.Bottom)-Implantation.Feet(.005)),false));}
  return(paper,zones);
 }
}
