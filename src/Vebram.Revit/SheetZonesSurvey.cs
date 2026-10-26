using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class SheetZonesSurvey
{
 internal static void Run(Document doc,string output)
 {
  var sheet=SheetPlanner.Sheets(doc).First();var title=new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().First(e=>e.Category.Id.Value==(long)BuiltInCategory.OST_TitleBlocks);
  var curves=new List<Curve>();void Read(GeometryElement geometry){foreach(var g in geometry){if(g is GeometryInstance i)Read(i.GetInstanceGeometry());else if(g is Curve c)curves.Add(c);}}
  // Title blocks expose 2D geometry through the editable family, not reliably through the sheet instance.
  var family=doc.EditFamily(title.Symbol.Family);
  try
  {
   var texts=new FilteredElementCollector(family).OfClass(typeof(TextNote)).Cast<TextNote>().Select(t=>new{t.Text,X=t.Coord.X,Y=t.Coord.Y,Width=t.Width,Height=t.Height}).ToList();
   var lines=new FilteredElementCollector(family).OfClass(typeof(CurveElement)).Cast<CurveElement>().Select(c=>c.GeometryCurve).OfType<Line>().Select(l=>new{X1=l.GetEndPoint(0).X,Y1=l.GetEndPoint(0).Y,X2=l.GetEndPoint(1).X,Y2=l.GetEndPoint(1).Y}).ToList();
   var box=title.get_BoundingBox(sheet);
   File.WriteAllText(Path.Combine(output,"ZONAS_CARIMBO.json"),JsonSerializer.Serialize(new{Family=title.Symbol.Family.Name,Type=title.Symbol.Name,Min=new[]{box.Min.X,box.Min.Y},Max=new[]{box.Max.X,box.Max.Y},Texts=texts,Lines=lines,InstanceLines=curves.OfType<Line>().Select(l=>new{X1=l.GetEndPoint(0).X,Y1=l.GetEndPoint(0).Y,X2=l.GetEndPoint(1).X,Y2=l.GetEndPoint(1).Y})},new JsonSerializerOptions{WriteIndented=true}));
  }
  finally{family.Close(false);}
 }
}
