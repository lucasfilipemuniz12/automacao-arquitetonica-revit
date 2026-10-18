using Autodesk.Revit.DB;
namespace Vebram.Revit;
public sealed record RoofLoop(XYZ Min,XYZ Max,IReadOnlyList<Line> Edges);
public sealed record RoofWater(string Key,string RoofName,XYZ Center,double Slope,XYZ? Direction,IReadOnlyList<RoofLoop>? Loops=null);
public static class RoofWaters
{
 public static IReadOnlyList<RoofWater> Read(Document doc,IEnumerable<Element> roofs)
 {
  var result=new List<RoofWater>();
  foreach(var roof in roofs)
  {
   var geometry=roof.get_Geometry(new Options{DetailLevel=ViewDetailLevel.Fine,ComputeReferences=true});if(geometry==null)continue;
   var faces=Faces(geometry).Where(f=>f.FaceNormal.Z>.1&&f.Area*.09290304>=.5).ToList();
   for(int i=0;i<faces.Count;i++)
   {
    var face=faces[i];var bounds=face.GetBoundingBox();var uv=(bounds.Min+bounds.Max)/2;
    var center=face.IsInside(uv)?face.Evaluate(uv):face.Triangulate().Vertices.Aggregate(XYZ.Zero,(p,q)=>p+q)/face.Triangulate().Vertices.Count;
    var n=face.FaceNormal;double slope=100*Math.Sqrt(n.X*n.X+n.Y*n.Y)/Math.Abs(n.Z);
    var key=roof.UniqueId+"/"+(face.Reference?.ConvertToStableRepresentation(doc)??i.ToString());
    var loops=new List<RoofLoop>();
    foreach(EdgeArray boundary in face.EdgeLoops)
    {
     var segments=new List<Line>();var points=new List<XYZ>();
     foreach(Edge edge in boundary)
     {var path=edge.AsCurve().Tessellate();points.AddRange(path);for(int j=1;j<path.Count;j++)if(path[j-1].DistanceTo(path[j])>doc.Application.ShortCurveTolerance)segments.Add(Line.CreateBound(path[j-1],path[j]));}
     if(points.Count>0)loops.Add(new(new XYZ(points.Min(p=>p.X),points.Min(p=>p.Y),points.Min(p=>p.Z)),new XYZ(points.Max(p=>p.X),points.Max(p=>p.Y),points.Max(p=>p.Z)),segments));
    }
    result.Add(new(key,roof.Name,center,slope,slope>.05?new XYZ(n.X,n.Y,0).Normalize():null,loops));
   }
  }
  return result.OrderBy(w=>w.Key,StringComparer.Ordinal).ToList();
 }
 static IEnumerable<PlanarFace> Faces(GeometryElement geometry)
 {
  foreach(var g in geometry){if(g is Solid s){foreach(var f in s.Faces.OfType<PlanarFace>())yield return f;}else if(g is GeometryInstance instance){foreach(var f in Faces(instance.GetInstanceGeometry()))yield return f;}}
 }
}

