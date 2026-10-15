using Autodesk.Revit.DB;
namespace Vebram.Revit;
public sealed record RoofWater(string Key,string RoofName,XYZ Center,double Slope,XYZ? Direction);
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
    result.Add(new(key,roof.Name,center,slope,slope>.05?new XYZ(n.X,n.Y,0).Normalize():null));
   }
  }
  return result.OrderBy(w=>w.Key,StringComparer.Ordinal).ToList();
 }
 static IEnumerable<PlanarFace> Faces(GeometryElement geometry)
 {
  foreach(var g in geometry){if(g is Solid s){foreach(var f in s.Faces.OfType<PlanarFace>())yield return f;}else if(g is GeometryInstance instance){foreach(var f in Faces(instance.GetInstanceGeometry()))yield return f;}}
 }
}

