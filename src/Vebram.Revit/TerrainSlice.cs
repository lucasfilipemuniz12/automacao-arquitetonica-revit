namespace Vebram.Revit;

internal readonly record struct TerrainPoint(double X,double Y,double Z)
{
 internal TerrainPoint Between(TerrainPoint b,double t)=>new(X+(b.X-X)*t,Y+(b.Y-Y)*t,Z+(b.Z-Z)*t);
 internal double DistanceTo(TerrainPoint b)=>Math.Sqrt((X-b.X)*(X-b.X)+(Y-b.Y)*(Y-b.Y)+(Z-b.Z)*(Z-b.Z));
 internal double PlaneDistance(TerrainPoint origin,TerrainPoint normal)=>(X-origin.X)*normal.X+(Y-origin.Y)*normal.Y+(Z-origin.Z)*normal.Z;
}
internal static class TerrainSlice
{
 internal static (TerrainPoint A,TerrainPoint B)? Triangle(IReadOnlyList<TerrainPoint> triangle,TerrainPoint origin,TerrainPoint normal)
 {
  if(triangle.All(p=>Math.Abs(p.PlaneDistance(origin,normal))<1e-7))return null;
  var hits=new List<TerrainPoint>();
  void Add(TerrainPoint p){if(!hits.Any(h=>h.DistanceTo(p)<1e-6))hits.Add(p);}
  for(int i=0;i<3;i++)
  {
   var a=triangle[i];var b=triangle[(i+1)%3];double da=a.PlaneDistance(origin,normal),db=b.PlaneDistance(origin,normal);
   if(Math.Abs(da)<1e-7)Add(a);
   if(da*db<0)Add(a.Between(b,da/(da-db)));
  }
  return hits.Count==2?(hits[0],hits[1]):null;
 }
}
