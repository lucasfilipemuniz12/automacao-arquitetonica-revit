using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>
/// Arranges the native level heads (symbol + name + elevation) already placed to the left of the building
/// in a section or elevation. Only the view-specific datum lines move; level elevations are never changed.
/// </summary>
internal static class LevelHeads
{
 /// <summary>
 /// 1. Heads whose drawn text overlaps vertically (e.g. Terreno 0,00 and Térreo 0,10) are staggered into
 ///    columns, the later one further left, so both stay readable.
 /// 2. The whole set slides left until its right edge keeps <paramref name="clearance"/> from the building
 ///    (a long name such as "Cobertura 5,30" used to run over the drawing).
 /// 3. The crop grows on the left only if a head would be cut.
 /// </summary>
 internal static void Arrange(Document doc,View view,List<(Level Level,Line Line)> placed,double buildingLeft,double clearance,List<string> pending)
 {
  if(placed.Count==0)return;
  var right=view.RightDirection;
  // Extent of each drawn head in view coordinates: X along the view, Y vertical.
  (double MinX,double MaxX,double MinY,double MaxY)? Extent(Level level)
  {
   var bb=level.get_BoundingBox(view);if(bb==null)return null;var inv=view.CropBox.Transform.Inverse;
   var pts=new[]{bb.Min.X,bb.Max.X}.SelectMany(x=>new[]{bb.Min.Y,bb.Max.Y}.SelectMany(y=>new[]{bb.Min.Z,bb.Max.Z}.Select(z=>inv.OfPoint(bb.Transform.OfPoint(new XYZ(x,y,z)))))).ToList();
   return (pts.Min(p=>p.X),pts.Max(p=>p.X),pts.Min(p=>p.Y),pts.Max(p=>p.Y));
  }
  doc.Regenerate();
  var items=placed.Select(p=>(p.Level,p.Line,Box:Extent(p.Level))).Where(i=>i.Box!=null).Select(i=>(i.Level,i.Line,Box:i.Box!.Value)).OrderBy(i=>i.Level.Elevation).ToList();
  if(items.Count==0)return;
  // Column per head: first column that has no vertically overlapping head yet.
  double width=items.Max(i=>i.Box.MaxX-i.Box.MinX),gap=Implantation.Feet(.002)*view.Scale; // 2 mm on paper between columns
  var columns=new List<List<(double MinY,double MaxY)>>();var column=new Dictionary<Level,int>();
  foreach(var item in items)
  {
   int c=0;while(c<columns.Count&&columns[c].Any(r=>item.Box.MinY<r.MaxY&&item.Box.MaxY>r.MinY))c++;
   if(c==columns.Count)columns.Add(new());columns[c].Add((item.Box.MinY,item.Box.MaxY));column[item.Level]=c;
  }
  double baseShift=Math.Max(0,items.Max(i=>i.Box.MaxX)-(buildingLeft-clearance));
  bool moved=false;
  try
  {
   foreach(var item in items)
   {
    double shift=baseShift+column[item.Level]*(width+gap);if(shift<Implantation.Feet(.01))continue;
    var offset=-right*shift;item.Level.SetCurveInView(DatumExtentType.ViewSpecific,view,Line.CreateBound(item.Line.GetEndPoint(0)+offset,item.Line.GetEndPoint(1)+offset));moved=true;
   }
  }
  catch(Autodesk.Revit.Exceptions.ArgumentException){pending.Add("Símbolos de nível: não foi possível reposicioná-los; conferir na vista "+view.Name+".");return;}
  if(columns.Count>1)pending.Add("Níveis muito próximos ("+string.Join(", ",items.Where(i=>column[i.Level]>0).Select(i=>i.Level.Name))+") foram escalonados para a esquerda; valores inalterados.");
  if(moved)doc.Regenerate();
  var final=items.Select(i=>Extent(i.Level)).Where(e=>e!=null).Select(e=>e!.Value).ToList();if(final.Count==0)return;
  var box=view.CropBox;double needed=final.Min(e=>e.MinX)-Implantation.Feet(.2);
  if(view.CropBoxActive&&needed<box.Min.X){box.Min=new XYZ(needed,box.Min.Y,box.Min.Z);view.CropBox=box;doc.Regenerate();}
 }
}
