using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
namespace Vebram.Revit;
internal static class ElevationAnnotations
{
 // Explicit reference height; never labels a flat line as surveyed natural terrain.
 internal static void Total(Document doc,ViewSection view,BuildingFrame frame,List<Element> made,List<string> pending)
 {
  var transform=view.CropBox.Transform;var inv=transform.Inverse;double right=frame.Corners.Max(p=>inv.OfPoint(p).X),left=frame.Corners.Min(p=>inv.OfPoint(p).X);
  double surface=frame.Bottom+Implantation.Feet(.5),top=frame.Top-Implantation.Feet(.6),x=right+Implantation.Feet(.65);
  XYZ At(double xx,double z)=>transform.OfPoint(new XYZ(xx,z-transform.Origin.Z,0));
  DetailCurve Tick(double z){var c=doc.Create.NewDetailCurve(view,Line.CreateBound(At(x-Implantation.Feet(.2),z),At(x+Implantation.Feet(.2),z)));made.Add(c);return c;}
  if(top>surface+Implantation.Feet(.1))
  {
   var a=Tick(surface);var b=Tick(top);doc.Regenerate();var refs=new ReferenceArray();refs.Append(a.GeometryCurve.Reference??new Reference(a));refs.Append(b.GeometryCurve.Reference??new Reference(b));
   made.Add(doc.Create.NewDimension(view,Line.CreateBound(At(x,surface),At(x,top)),refs,LegalPreferenceStore.Dimension(doc)));
   made.Add(TextNote.Create(doc,view.Id,At(right-Implantation.Feet(.2),top+Implantation.Feet(.4)),"ALTURA TOTAL",LegalStyles.Text(doc,2).Id));
  }
  var text=frame.SurfaceMeasured?"BASE: TERRENO DO MODELO (COTA PONTUAL)":"BASE: NÍVEL FONTE · TERRENO A CONFERIR";
  made.Add(TextNote.Create(doc,view.Id,At(left,surface-Implantation.Feet(.18)),text,LegalStyles.Text(doc,2).Id));
  if(!frame.SurfaceMeasured)pending.Add("Altura total referenciada ao nível fonte: não foi encontrada superfície de terreno válida no centro da edificação.");
 }
 internal static void Rooms(Document doc,ViewSection view,ViewPlan source,BuildingFrame frame,List<Element> made,List<string> pending)
 {
  var transform=view.CropBox.Transform;var inv=transform.Inverse;var normal=view.ViewDirection;var origin=view.Origin;
  var phase=source.get_Parameter(BuiltInParameter.VIEW_PHASE)?.AsElementId();
  var rooms=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Room>().Where(r=>r.Area>0&&r.LevelId==source.GenLevel.Id&&(phase==null||r.get_Parameter(BuiltInParameter.ROOM_PHASE)?.AsElementId()==phase)).ToList();
  foreach(var room in rooms)
  {
   using var boundaryOptions=new SpatialElementBoundaryOptions();var boundaries=room.GetBoundarySegments(boundaryOptions);if(boundaries==null)continue;var hits=new List<double>();
   foreach(var loop in boundaries)foreach(var segment in loop)
   {
    var pts=segment.GetCurve().Tessellate();for(int i=1;i<pts.Count;i++)
    {var a=pts[i-1];var b=pts[i];double da=(a-origin).DotProduct(normal),db=(b-origin).DotProduct(normal);if(da*db>0||Math.Abs(da-db)<1e-9)continue;var p=a+(b-a)*(da/(da-db));hits.Add(inv.OfPoint(p).X);}
   }
   hits=hits.Order().Aggregate(new List<double>(),(list,v)=>{if(list.Count==0||Math.Abs(list[^1]-v)>.001)list.Add(v);return list;});
   if(hits.Count<2)continue;var intervals=hits.Zip(hits.Skip(1),(a,b)=>(Left:a,Right:b)).Where(i=>i.Right-i.Left>Implantation.Feet(.3)).ToList();
   XYZ At(double x,double z)=>transform.OfPoint(new XYZ(x,z-transform.Origin.Z,0));
   var valid=intervals.Where(i=>room.IsPointInRoom(At((i.Left+i.Right)/2,source.GenLevel.Elevation+Implantation.Feet(1)))).OrderByDescending(i=>i.Right-i.Left).ToList();if(valid.Count==0)continue;
   var span=valid[0];double mid=(span.Left+span.Right)/2;var point=At(mid,source.GenLevel.Elevation+Implantation.Feet(1));
   var bottom=frame.Members.OfType<Floor>().SelectMany(f=>Faces(f,true)).Where(f=>f.Face.Origin.Z<point.Z&&Contains(f.Face,point)).OrderByDescending(f=>f.Face.Origin.Z).FirstOrDefault();
   var top=frame.Members.OfType<HostObject>().Where(h=>h is Floor or Ceiling or RoofBase).SelectMany(h=>Faces(h,false)).Where(f=>f.Face.Origin.Z>point.Z&&Contains(f.Face,point)).OrderBy(f=>f.Face.Origin.Z).FirstOrDefault();
   var name=RoomNameText.Display(room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString());
   double baseZ=bottom?.Face.Origin.Z??source.GenLevel.Elevation;
   var caption=name;
   if(bottom!=null&&top!=null)
   {
    double height=Implantation.Meters(top.Face.Origin.Z-bottom.Face.Origin.Z);caption+="\nPD "+height.ToString("0.00",Implantation.Pt)+" m";
    // Place a native face-referenced dimension only where there is room for it.
    if(span.Right-span.Left>=Implantation.Feet(1.4))
    {var refs=new ReferenceArray();refs.Append(bottom.Ref);refs.Append(top.Ref);doc.Regenerate();double xx=span.Left+Implantation.Feet(.25);made.Add(doc.Create.NewDimension(view,Line.CreateBound(At(xx,baseZ),At(xx,top.Face.Origin.Z)),refs,LegalPreferenceStore.Dimension(doc)));}
   }
   else{caption+="\nPD a conferir";pending.Add(name+": não há par de faces horizontais de piso/teto na posição do corte para medir pé-direito.");}
   double paperWidth=Math.Min(.045,Implantation.Meters(span.Right-span.Left)/view.Scale*.85);
   made.Add(TextNote.Create(doc,view.Id,At(mid,baseZ+Implantation.Feet(1.15)),Implantation.Feet(paperWidth),caption,new TextNoteOptions(LegalStyles.Text(doc,2).Id){HorizontalAlignment=HorizontalTextAlignment.Center}));
  }
 }
 sealed record FaceReference(PlanarFace Face,Reference Ref);
 static IEnumerable<FaceReference> Faces(HostObject host,bool top)
 {
  IList<Reference> refs;try{refs=top?HostObjectUtils.GetTopFaces(host):HostObjectUtils.GetBottomFaces(host);}catch(Autodesk.Revit.Exceptions.ArgumentException){yield break;}
  foreach(var reference in refs)if(host.GetGeometryObjectFromReference(reference) is PlanarFace face&&Math.Abs(face.FaceNormal.Z)>.999)yield return new(face,reference);
 }
 static bool Contains(PlanarFace face,XYZ point){var hit=face.Project(point);return hit!=null&&face.IsInside(hit.UVPoint);}
}
