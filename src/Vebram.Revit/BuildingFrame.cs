using Autodesk.Revit.DB;
namespace Vebram.Revit;

// Local building context, excluding site solids and remote landscape elements.
internal sealed record BuildingFrame(List<Element> Members,List<XYZ> Corners,double Bottom,double Top,bool SurfaceMeasured)
{
 public static BuildingFrame Read(Document doc,ViewPlan source,Floor floor,bool wholeSite=false)
 {
  var contour=Implantation.GetContour(doc,floor);double x0=contour.Min(p=>p.X),x1=contour.Max(p=>p.X),y0=contour.Min(p=>p.Y),y1=contour.Max(p=>p.Y),buffer=Implantation.Feet(2);
  bool Terrain(Element e)=>e.Category?.Id.Value==(long)BuiltInCategory.OST_Toposolid||e.GetType().Name=="TopographySurface";
  var members=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>!e.ViewSpecific&&(e is HostObject or FamilyInstance or DirectShape)&&!Terrain(e)&&e.get_BoundingBox(null) is {} b&&b.Min.X>=x0-buffer&&b.Max.X<=x1+buffer&&b.Min.Y>=y0-buffer&&b.Max.Y<=y1+buffer&&b.Max.Z>=source.GenLevel.Elevation-Implantation.Feet(.7)).ToList();
  if(!members.Any(e=>e.Id==floor.Id))members.Add(floor);
  double surface=source.GenLevel.Elevation;bool measured=false;var center=new XYZ((x0+x1)/2,(y0+y1)/2,surface);
  foreach(var terrain in new FilteredElementCollector(doc).WhereElementIsNotElementType().OfType<HostObject>().Where(Terrain))
  {
   IList<Reference> faces;try{faces=HostObjectUtils.GetTopFaces(terrain);}catch(Autodesk.Revit.Exceptions.ArgumentException){continue;}
   foreach(var faceRef in faces)if(terrain.GetGeometryObjectFromReference(faceRef) is Face face)
   {var hit=face.Project(center);if(hit!=null&&face.IsInside(hit.UVPoint)&&Math.Abs(hit.XYZPoint.Z-source.GenLevel.Elevation)<Implantation.Feet(2)){surface=hit.XYZPoint.Z;measured=true;break;}}
   if(measured)break;
  }
  var bounds=members.Select(e=>e.get_BoundingBox(null)).Where(b=>b!=null).ToList();double bottom=surface-Implantation.Feet(.5),top=bounds.Max(b=>b!.Max.Z)+Implantation.Feet(.6);
  var corners=new List<XYZ>();foreach(var b in bounds)foreach(var x in new[]{b!.Min.X,b.Max.X})foreach(var y in new[]{b.Min.Y,b.Max.Y})foreach(var z in new[]{bottom,top})corners.Add(new(x,y,z));
  foreach(var p in contour){corners.Add(new(p.X,p.Y,bottom));corners.Add(new(p.X,p.Y,top));}
  if(wholeSite)foreach(var b in new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(Terrain).Select(e=>e.get_BoundingBox(null)).Where(b=>b!=null))foreach(var x in new[]{b!.Min.X,b.Max.X})foreach(var y in new[]{b.Min.Y,b.Max.Y}){corners.Add(new(x,y,bottom));corners.Add(new(x,y,top));}
  return new(members,corners,bottom,top,measured);
 }
}
