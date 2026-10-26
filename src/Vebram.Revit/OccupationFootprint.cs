using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>
/// Ground-floor projection used for TO. Default: only the main slab chosen in the Implantação (unchanged behaviour).
/// The user may tick additional slabs/floors of the same level (e.g. the abrigo slab); each one uses the same
/// external-contour rule and overlaps are counted once. No criterion is decided here: the table lists every
/// slab with its area so the choice can be checked.
/// </summary>
internal static class OccupationFootprint
{
 internal sealed record Part(long Id,string Label,double Area);
 internal sealed record Result(double Total,List<Part> Parts,double Overlap);

 internal static Result Measure(Document doc,ImplantationOptions options)
 {
  var ids=new List<long>{options.FootprintId};
  foreach(var id in options.OccupationExtraIds??new())if(!ids.Contains(id))ids.Add(id);
  var parts=new List<Part>();var loops=new List<IReadOnlyList<XYZ>>();
  foreach(var id in ids)
  {
   if(doc.GetElement(new ElementId(id)) is not Floor floor)throw new InvalidOperationException("Laje "+id+" usada na taxa de ocupação não existe mais no modelo. Refaça a Implantação.");
   var contour=Implantation.GetContour(doc,floor);loops.Add(contour);
   parts.Add(new(id,(doc.GetElement(floor.GetTypeId())?.Name??floor.Name)+" (id "+id+")",Area(contour)));
  }
  if(parts.Count==1)return new(parts[0].Area,parts,0);
  Solid? union=null;
  foreach(var contour in loops){var solid=Extrude(contour);union=union==null?solid:BooleanOperationsUtils.ExecuteBooleanOperation(union,solid,BooleanOperationsType.Union);}
  double total=union!.Volume*.09290304; // unit-height extrusion in internal feet → m²
  return new(total,parts,Math.Max(0,parts.Sum(p=>p.Area)-total));
 }
 static double Area(IReadOnlyList<XYZ> contour)=>LegalMetrics.PolygonArea(contour.Select(p=>(Implantation.Meters(p.X),Implantation.Meters(p.Y))).ToList());
 static Solid Extrude(IReadOnlyList<XYZ> contour)
 {
  var points=contour.Select(p=>new XYZ(p.X,p.Y,0)).ToList();var curves=new List<Curve>();
  double signed=0;for(int i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];signed+=a.X*b.Y-b.X*a.Y;}
  if(signed<0)points.Reverse(); // counter-clockwise outer loop
  for(int i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];if(a.DistanceTo(b)>1e-6)curves.Add(Line.CreateBound(a,b));}
  return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>{CurveLoop.Create(curves)},XYZ.BasisZ,1);
 }
}
