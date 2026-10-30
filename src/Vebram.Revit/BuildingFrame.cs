using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
namespace Vebram.Revit;

internal sealed record BuildingFrame(List<Element> Members,List<XYZ> Corners,double Bottom,double Top,bool SurfaceMeasured,bool MeasuredAtPerimeter=false)
{
 public static BuildingFrame Read(Document doc,ViewPlan source,Floor floor,bool wholeSite=false,Level? baseLevel=null)
 {
  var rooms=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Room>().Where(r=>r.Area>0).ToList();
  var walls=new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
  bool BuildingFloor(Floor f)
  {
   if(f.Id==floor.Id)return true;
   var b=f.get_BoundingBox(null);if(b==null)return false;
   string name=f.Name.ToUpperInvariant();if(name.Contains("CALÇADA")||name.Contains("CALCADA")||name.Contains("GRAM")||name.Contains("PASSEIO"))return false;
   bool Overlap(BoundingBoxXYZ a)=>Math.Min(a.Max.X,b.Max.X)-Math.Max(a.Min.X,b.Min.X)>.01&&Math.Min(a.Max.Y,b.Max.Y)-Math.Max(a.Min.Y,b.Min.Y)>.01;
   if(rooms.Any(r=>r.get_BoundingBox(null) is {} rb&&Overlap(rb)&&Math.Abs(((Level)doc.GetElement(r.LevelId)).Elevation-b.Max.Z)<Implantation.Feet(.65)))return true;
   // Slabs supported by walls include uncovered terraces even without a room.
   return walls.Any(w=>w.get_BoundingBox(null) is {} wb&&Overlap(wb)&&Math.Abs(wb.Max.Z-b.Min.Z)<Implantation.Feet(.4));
  }
  var candidates=new FilteredElementCollector(doc).WhereElementIsNotElementType()
   .Where(e=>!e.ViewSpecific&&(e is Wall or RoofBase or Ceiling or Stairs or Railing||e is Floor f&&BuildingFloor(f)))
   .Where(e=>e.get_BoundingBox(null)!=null).ToList();
  if(!candidates.Any(e=>e.Id==floor.Id))candidates.Add(floor);
  var boxes=candidates.Select(e=>e.get_BoundingBox(null)!).ToList();
  var connected=BuildingEnvelope.Connected(boxes.Select(b=>new BuildingBounds(b.Min.X,b.Min.Y,b.Min.Z,b.Max.X,b.Max.Y,b.Max.Z)).ToList(),candidates.FindIndex(e=>e.Id==floor.Id),Implantation.Feet(.35));
  var members=connected.Select(i=>candidates[i]).ToList();var bounds=members.Select(e=>e.get_BoundingBox(null)!).ToList();
  double x0=bounds.Min(b=>b.Min.X),x1=bounds.Max(b=>b.Max.X),y0=bounds.Min(b=>b.Min.Y),y1=bounds.Max(b=>b.Max.Y);
  // Families follow the building, but cannot expand the envelope to landscape objects.
  members.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
   .Where(e=>!e.ViewSpecific&&e.get_BoundingBox(null) is {} b&&b.Min.X>=x0-.01&&b.Max.X<=x1+.01&&b.Min.Y>=y0-.01&&b.Max.Y<=y1+.01));
  var terrain=TerrainProfile.Read(doc);var surface=terrain.Points.Where(p=>p.X>=x0-Implantation.Feet(1)&&p.X<=x1+Implantation.Feet(1)&&p.Y>=y0-Implantation.Feet(1)&&p.Y<=y1+Implantation.Feet(1)).ToList();
  double bottom=Math.Min(bounds.Min(b=>b.Min.Z),surface.Select(p=>p.Z).DefaultIfEmpty(source.GenLevel.Elevation).Min())-Implantation.Feet(.5);
  double top=Math.Max(bounds.Max(b=>b.Max.Z),surface.Select(p=>p.Z).DefaultIfEmpty(source.GenLevel.Elevation).Max())+Implantation.Feet(.6);
  var corners=new List<XYZ>();foreach(var x in new[]{x0,x1})foreach(var y in new[]{y0,y1})foreach(var z in new[]{bottom,top})corners.Add(new(x,y,z));
  if(wholeSite)foreach(var p in terrain.Points){corners.Add(new(p.X,p.Y,Math.Min(bottom,p.Z-Implantation.Feet(.5))));corners.Add(new(p.X,p.Y,top));}
  return new(members,corners,bottom,top,surface.Count>0,true);
 }
}
