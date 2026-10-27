namespace Vebram.Revit;

internal readonly record struct BuildingBounds(double X0,double Y0,double Z0,double X1,double Y1,double Z1)
{
 internal bool Touches(BuildingBounds b,double gap)=>X0<=b.X1+gap&&X1>=b.X0-gap&&Y0<=b.Y1+gap&&Y1>=b.Y0-gap&&Z0<=b.Z1+gap&&Z1>=b.Z0-gap;
}
internal static class BuildingEnvelope
{
 internal static HashSet<int> Connected(IReadOnlyList<BuildingBounds> bounds,int seed,double gap)
 {
  var selected=new HashSet<int>{seed};var queue=new Queue<int>();queue.Enqueue(seed);
  while(queue.TryDequeue(out int current))for(int i=0;i<bounds.Count;i++)
   if(!selected.Contains(i)&&bounds[current].Touches(bounds[i],gap)){selected.Add(i);queue.Enqueue(i);}
  return selected;
 }
}
