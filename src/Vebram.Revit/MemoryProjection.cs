using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>Horizontal finished-floor footprints; no model edits or municipal assumptions.</summary>
internal static class MemoryProjection
{
 internal static Solid Floor(Floor floor)
 {
  Solid? result=null;
  foreach(var reference in HostObjectUtils.GetTopFaces(floor))
  {
   if(floor.GetGeometryObjectFromReference(reference) is not PlanarFace face||face.FaceNormal.Z<.99999)
    throw new InvalidOperationException("A memória precisa de projeção horizontal conferida para o piso "+floor.Id.Value+". Piso inclinado/curvo não foi somado automaticamente.");
   var loops=face.GetEdgesAsCurveLoops();var shift=Transform.CreateTranslation(new XYZ(0,0,-face.Origin.Z));
   var flat=loops.Select(loop=>CurveLoop.Create(loop.Select(c=>c.CreateTransformed(shift)).ToList())).ToList();
   var solid=GeometryCreationUtilities.CreateExtrusionGeometry(flat,XYZ.BasisZ,1);
   result=result==null?solid:Union(result,solid);
  }
  return result??throw new InvalidOperationException("Não foi possível medir a face superior do piso "+floor.Id.Value);
 }
 internal static Solid Union(Solid a,Solid b)=>BooleanOperationsUtils.ExecuteBooleanOperation(a,b,BooleanOperationsType.Union);
 internal static double Area(Solid footprint)=>footprint.Volume*.09290304; // Unit-height extrusion in internal feet.
 internal static bool Within(BoundingBoxXYZ b,ImplantationOptions lot)
 {
  const double tolerance=.001; // metres: accommodates numerical noise, not boundary clipping.
  return Implantation.Meters(b.Min.X)>=lot.LotMinX-tolerance&&Implantation.Meters(b.Max.X)<=lot.LotMinX+lot.LotWidth+tolerance&&
   Implantation.Meters(b.Min.Y)>=lot.LotMinY-tolerance&&Implantation.Meters(b.Max.Y)<=lot.LotMinY+lot.LotDepth+tolerance;
 }
 internal static bool Intersects(BoundingBoxXYZ b,ImplantationOptions lot)=>
  Implantation.Meters(b.Max.X)>lot.LotMinX&&Implantation.Meters(b.Min.X)<lot.LotMinX+lot.LotWidth&&
  Implantation.Meters(b.Max.Y)>lot.LotMinY&&Implantation.Meters(b.Min.Y)<lot.LotMinY+lot.LotDepth;
}
