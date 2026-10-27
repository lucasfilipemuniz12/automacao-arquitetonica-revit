using Autodesk.Revit.DB;
namespace Vebram.Revit;

// Slice actual top-face triangles, including shape edits and excavation boundaries.
internal sealed class TerrainProfile
{
 readonly List<XYZ[]> triangles=[];
 internal IEnumerable<XYZ> Points=>triangles.SelectMany(t=>t);
 internal static TerrainProfile Read(Document doc)
 {
  var result=new TerrainProfile();
  foreach(var host in new FilteredElementCollector(doc).OfClass(typeof(Toposolid)).Cast<Toposolid>())
   foreach(var reference in HostObjectUtils.GetTopFaces(host))if(host.GetGeometryObjectFromReference(reference) is Face face)
   {
    var mesh=face.Triangulate();for(int i=0;i<mesh.NumTriangles;i++){var t=mesh.get_Triangle(i);result.triangles.Add([t.get_Vertex(0),t.get_Vertex(1),t.get_Vertex(2)]);}
   }
  return result;
 }
 internal List<(XYZ A,XYZ B)> Slice(ViewSection view,BuildingFrame frame)
 {
  var normal=view.ViewDirection;var origin=view.Origin;var inv=view.CropBox.Transform.Inverse;
  if(view.ViewType==ViewType.Elevation)
  {
   double edge=frame.Corners.Max(p=>p.DotProduct(normal));
   origin+=normal*(edge-origin.DotProduct(normal)+Implantation.Feet(.05));
  }
  double left=frame.Corners.Min(p=>inv.OfPoint(p).X)-Implantation.Feet(1),right=frame.Corners.Max(p=>inv.OfPoint(p).X)+Implantation.Feet(1);
  var segments=new List<(XYZ A,XYZ B)>();
  TerrainPoint P(XYZ p)=>new(p.X,p.Y,p.Z);
  XYZ Native(TerrainPoint p)=>new(p.X,p.Y,p.Z);
  foreach(var triangle in triangles)
  {
   var hit=TerrainSlice.Triangle(triangle.Select(P).ToList(),P(origin),P(normal));if(hit==null)continue;
   var local=new[]{Native(hit.Value.A),Native(hit.Value.B)}.Select(inv.OfPoint).OrderBy(p=>p.X).ToList();var a0=local[0];var b0=local[1];double dx=b0.X-a0.X;
   if(dx<1e-6||b0.X<left||a0.X>right)continue;
   double lo=Math.Max(left,a0.X),hi=Math.Min(right,b0.X);if(hi-lo<.003)continue;
   XYZ At(double x)=>view.CropBox.Transform.OfPoint(new(x,a0.Y+(b0.Y-a0.Y)*(x-a0.X)/dx,0));
   var start=At(lo);var end=At(hi);
   // A plane along a mesh edge intersects both neighboring triangles: draw the edge once.
   if(!segments.Any(s=>s.A.DistanceTo(start)<1e-6&&s.B.DistanceTo(end)<1e-6))segments.Add((start,end));
  }
  return segments;
 }
}
