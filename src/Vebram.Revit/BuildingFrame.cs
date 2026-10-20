using Autodesk.Revit.DB;
namespace Vebram.Revit;

// Local building context, excluding site solids and remote landscape elements.
internal sealed record BuildingFrame(List<Element> Members,List<XYZ> Corners,double Bottom,double Top,bool SurfaceMeasured,bool MeasuredAtPerimeter=false)
{
 public static BuildingFrame Read(Document doc,ViewPlan source,Floor floor,bool wholeSite=false)
 {
  var contour=Implantation.GetContour(doc,floor);double x0=contour.Min(p=>p.X),x1=contour.Max(p=>p.X),y0=contour.Min(p=>p.Y),y1=contour.Max(p=>p.Y),buffer=Implantation.Feet(2);
  bool Terrain(Element e)=>e.Category?.Id.Value==(long)BuiltInCategory.OST_Toposolid||e.GetType().Name=="TopographySurface";
  var members=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>!e.ViewSpecific&&(e is HostObject or FamilyInstance or DirectShape)&&!Terrain(e)&&e.get_BoundingBox(null) is {} b&&b.Min.X>=x0-buffer&&b.Max.X<=x1+buffer&&b.Min.Y>=y0-buffer&&b.Max.Y<=y1+buffer&&b.Max.Z>=source.GenLevel.Elevation-Implantation.Feet(.7)).ToList();
  if(!members.Any(e=>e.Id==floor.Id))members.Add(floor);
  double surface=source.GenLevel.Elevation;bool measured=false,perimeter=false;var center=new XYZ((x0+x1)/2,(y0+y1)/2,surface);
  var topFaces=new List<Face>();
  foreach(var terrain in new FilteredElementCollector(doc).WhereElementIsNotElementType().OfType<HostObject>().Where(Terrain))
  {
   IList<Reference> faces;try{faces=HostObjectUtils.GetTopFaces(terrain);}catch(Autodesk.Revit.Exceptions.ArgumentException){continue;}
   foreach(var faceRef in faces)if(terrain.GetGeometryObjectFromReference(faceRef) is Face face)topFaces.Add(face);
  }
  double? Hit(XYZ point)
  {
   foreach(var face in topFaces){var hit=face.Project(point);if(hit!=null&&face.IsInside(hit.UVPoint)&&Math.Abs(hit.XYZPoint.Z-source.GenLevel.Elevation)<Implantation.Feet(2))return hit.XYZPoint.Z;}
   return null;
  }
  if(Hit(center) is double z){surface=z;measured=true;}
  else
  {
   // When the terrain is cut under the house there is no surface at the centre.
   // Sample the ground 0.5 m outside each corner and edge midpoint and keep the lowest
   // point, which gives the largest (conservative) total height.
   var samples=new List<XYZ>();
   for(int i=0;i<contour.Count;i++){var a=contour[i];var b=contour[(i+1)%contour.Count];samples.Add(a);samples.Add((a+b)/2);}
   var hits=samples.Select(p=>{var d=new XYZ(p.X-center.X,p.Y-center.Y,0);double len=d.GetLength();return len<1e-6?(double?)null:Hit(new XYZ(p.X,p.Y,surface)+d/len*Implantation.Feet(.5));}).Where(h=>h!=null).Select(h=>h!.Value).ToList();
   if(hits.Count>0){surface=hits.Min();measured=true;perimeter=true;}
  }
  var bounds=members.Select(e=>e.get_BoundingBox(null)).Where(b=>b!=null).ToList();double bottom=surface-Implantation.Feet(.5),top=bounds.Max(b=>b!.Max.Z)+Implantation.Feet(.6);
  var corners=new List<XYZ>();foreach(var b in bounds)foreach(var x in new[]{b!.Min.X,b.Max.X})foreach(var y in new[]{b.Min.Y,b.Max.Y})foreach(var z in new[]{bottom,top})corners.Add(new(x,y,z));
  foreach(var p in contour){corners.Add(new(p.X,p.Y,bottom));corners.Add(new(p.X,p.Y,top));}
  if(wholeSite)foreach(var b in new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(Terrain).Select(e=>e.get_BoundingBox(null)).Where(b=>b!=null))foreach(var x in new[]{b!.Min.X,b.Max.X})foreach(var y in new[]{b.Min.Y,b.Max.Y}){corners.Add(new(x,y,bottom));corners.Add(new(x,y,top));}
  return new(members,corners,bottom,top,measured,perimeter);
 }
}
