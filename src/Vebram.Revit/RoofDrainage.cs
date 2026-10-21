using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class RoofDrainage
{
 internal static List<RoofDrainageChoice> Choices(Document doc,IReadOnlyList<RoofWater> waters,ImplantationOptions? saved)
 {
  var list=new List<RoofDrainageChoice>();
  foreach(var water in waters)
  {
   var existing=saved?.Drainage?.FirstOrDefault(c=>c.Key==water.Key);
   if(existing!=null){list.Add(existing);continue;}
   // Preserve only the old, explicitly limited single-water main-roof proposal.
   var roof=doc.GetElement(water.Key.Split('/')[0]);var b=roof?.get_BoundingBox(null);
   bool legacy=saved?.Drainage==null&&saved?.DesignRoofSlope>0&&saved.RoofFlow!="Modelo"&&water.Slope<.05&&b!=null&&(b.Max.X-b.Min.X)*(b.Max.Y-b.Min.Y)>=Implantation.Feet(6)*Implantation.Feet(6)&&waters.Count(w=>w.Key.Split('/')[0]==water.Key.Split('/')[0])==1;
   list.Add(new(water.Key,legacy?saved!.DesignRoofSlope:null,legacy?saved!.RoofFlow:"Modelo"));
  }
  return list;
 }
 internal static XYZ? Direction(string flow)=>flow switch{"Direita"=>XYZ.BasisX,"Esquerda"=>-XYZ.BasisX,"Acima"=>XYZ.BasisY,"Abaixo"=>-XYZ.BasisY,_=>null};
 internal static void Annotate(Document doc,ViewPlan view,IReadOnlyList<RoofWater> waters,ImplantationOptions? options,List<Element> made)
 {
  var choices=Choices(doc,waters,options);double unit=Implantation.Feet(.001)*view.Scale,z=view.GenLevel.Elevation;var occupied=new List<BoundingBoxXYZ>();var textBounds=new List<BoundingBoxXYZ>();
  foreach(var existing in new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>())if(existing.get_BoundingBox(view) is {} bounds){occupied.Add(bounds);textBounds.Add(bounds);}
  foreach(var dimension in new FilteredElementCollector(doc,view.Id).OfClass(typeof(Dimension)))if(dimension.get_BoundingBox(view) is {} dimensionBounds)occupied.Add(dimensionBounds);
  // Small objects drawn on the roof (drains, equipment, columns, detail items) also reserve their space, so a label never lands on them.
  foreach(var item in new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType()
   .Where(e=>e.Category?.Id.Value is (long)BuiltInCategory.OST_GenericModel or (long)BuiltInCategory.OST_DetailComponents or (long)BuiltInCategory.OST_PlumbingFixtures or (long)BuiltInCategory.OST_Columns or (long)BuiltInCategory.OST_MechanicalEquipment))
   if(item.get_BoundingBox(view) is {} itemBounds&&itemBounds.Max.X-itemBounds.Min.X<=60*unit&&itemBounds.Max.Y-itemBounds.Min.Y<=60*unit)occupied.Add(itemBounds);
  // Small roof volumes (e.g. the tank enclosure) also reserve their footprint: a caption belongs outside the box.
  foreach(var roof in waters.Select(w=>doc.GetElement(w.Key.Split('/')[0])).Where(e=>e!=null).DistinctBy(e=>e!.Id))
   if(roof!.get_BoundingBox(null) is {} rb&&rb.Max.X-rb.Min.X<60*unit&&rb.Max.Y-rb.Min.Y<60*unit)occupied.Add(rb);
  var outlines=waters.SelectMany(w=>w.Loops??Array.Empty<RoofLoop>()).ToList();
  foreach(var loop in outlines.Where(b=>b.Max.X-b.Min.X<60*unit&&b.Max.Y-b.Min.Y<60*unit))occupied.Add(new BoundingBoxXYZ{Min=loop.Min,Max=loop.Max});
  var roofLines=new List<Line>();
  foreach(var edge in outlines.SelectMany(l=>l.Edges)){var a=edge.GetEndPoint(0);var b=edge.GetEndPoint(1);var from=new XYZ(a.X,a.Y,z);var to=new XYZ(b.X,b.Y,z);if(from.DistanceTo(to)>doc.Application.ShortCurveTolerance)roofLines.Add(Line.CreateBound(from,to));}
  var wallLines=new List<(Line Line,double Half)>();
  foreach(var curve in new FilteredElementCollector(doc,view.Id).OfClass(typeof(CurveElement)).Cast<CurveElement>())if(curve.GeometryCurve is Line edge&&edge.IsBound)roofLines.Add(edge);
  foreach(var wall in new FilteredElementCollector(doc,view.Id).OfClass(typeof(Wall)).Cast<Wall>())if((wall.Location as LocationCurve)?.Curve is Line wallLine&&wallLine.IsBound)wallLines.Add((wallLine,wall.Width/2));
  int index=0;foreach(var water in waters)
  {
   index++;var value=RoofDrainageRules.Resolve(water.Slope,"Modelo",choices.FirstOrDefault(c=>c.Key==water.Key));var center=new XYZ(water.Center.X,water.Center.Y,z);
   var caption="A"+index+" · "+(value.Slope.HasValue?value.Slope.Value.ToString("0.0",Implantation.Pt)+"%"+(value.FromModel?"":" (projeto)"):"caimento a definir");
   // Non-wrapping note: its box hugs the text, so the leader and the flow arrow line up with the caption
   // (a fixed 38 mm box left them pointing at empty space beside a short "A1 · 30,0%").
   var note=TextNote.Create(doc,view.Id,center,caption,new TextNoteOptions(LegalStyles.Text(doc,2).Id){HorizontalAlignment=HorizontalTextAlignment.Center});made.Add(note);doc.Regenerate();
   bool Overlap(BoundingBoxXYZ a,BoundingBoxXYZ b)=>a.Min.X<b.Max.X+2*unit&&a.Max.X>b.Min.X-2*unit&&a.Min.Y<b.Max.Y+2*unit&&a.Max.Y>b.Min.Y-2*unit;
   bool ClearLeader(BoundingBoxXYZ box)
   {
    if(center.X>=box.Min.X-unit&&center.X<=box.Max.X+unit&&center.Y>=box.Min.Y-unit&&center.Y<=box.Max.Y+unit)return true;
    var end=new XYZ(Math.Clamp(center.X,box.Min.X-unit,box.Max.X+unit),Math.Clamp(center.Y,box.Min.Y-unit,box.Max.Y+unit),z);
    if(center.DistanceTo(end)<=doc.Application.ShortCurveTolerance)return true;
    var leader=Autodesk.Revit.DB.Line.CreateBound(center,end);
    return !textBounds.Any(b=>PlanAnnotations.TouchesLine(new PlanAnnotations.Rect(b.Min.X-unit,b.Min.Y-unit,b.Max.X+unit,b.Max.Y+unit),leader,0));
   }
   var original=note.Coord;bool placed=false;
   // Rings of growing distance in eight directions. First pass also keeps the label off walls and lines; the second pass keeps the old rules.
   var rings=new List<XYZ>{XYZ.Zero};
   foreach(double radius in new[]{12,24,40,60,80,110,140})foreach(var step in new[]{(0,1),(0,-1),(1,0),(-1,0),(1,1),(-1,1),(1,-1),(-1,-1)})rings.Add(new XYZ(step.Item1*radius*unit,step.Item2*radius*unit,0));
   foreach(bool avoidWalls in new[]{true,false})
   {
    foreach(var offset in rings)
    {note.Coord=original+offset;doc.Regenerate();var bounds=note.get_BoundingBox(view);
     if(bounds!=null&&ClearLeader(bounds)&&!occupied.Any(b=>Overlap(bounds,b))&&!roofLines.Any(line=>PlanAnnotations.TouchesLine(new PlanAnnotations.Rect(bounds.Min.X-2*unit,bounds.Min.Y-2*unit,bounds.Max.X+2*unit,bounds.Max.Y+2*unit),line,0))&&(!avoidWalls||!wallLines.Any(w=>PlanAnnotations.TouchesLine(new PlanAnnotations.Rect(bounds.Min.X,bounds.Min.Y,bounds.Max.X,bounds.Max.Y),w.Line,w.Half)))){occupied.Add(bounds);textBounds.Add(bounds);placed=true;break;}}
    if(placed)break;
   }
   if(!placed){note.Coord=original+new XYZ(0,(index+2)*12*unit,0);doc.Regenerate();if(note.get_BoundingBox(view) is {} b){occupied.Add(b);textBounds.Add(b);}}
   void Line(XYZ a,XYZ b){if(a.DistanceTo(b)>doc.Application.ShortCurveTolerance){var segment=Autodesk.Revit.DB.Line.CreateBound(a,b);made.Add(doc.Create.NewDetailCurve(view,segment));roofLines.Add(segment);}}
   if(note.Coord.DistanceTo(original)>3*unit&&note.get_BoundingBox(view) is {} labelBounds&&!(center.X>=labelBounds.Min.X-unit&&center.X<=labelBounds.Max.X+unit&&center.Y>=labelBounds.Min.Y-unit&&center.Y<=labelBounds.Max.Y+unit))
    {// A leader shorter than 4 mm on paper reads as a stray dash: the caption is already next to the water centre.
     var leaderEnd=new XYZ(Math.Clamp(center.X,labelBounds.Min.X-unit,labelBounds.Max.X+unit),Math.Clamp(center.Y,labelBounds.Min.Y-unit,labelBounds.Max.Y+unit),z);
     if(center.DistanceTo(leaderEnd)>4*unit)Line(center,leaderEnd);}
   var direction=value.FromModel?water.Direction:Direction(value.Flow);
   if(direction!=null&&value.Slope.HasValue)
   {
    var bounds=note.get_BoundingBox(view)!;var origin=new XYZ((bounds.Min.X+bounds.Max.X)/2,bounds.Min.Y-5*unit,z);var tip=origin+direction*5*unit;var tail=origin-direction*5*unit;var cross=new XYZ(-direction.Y,direction.X,0);
    Line(tail,tip);Line(tip,tip-direction*2*unit+cross*unit);Line(tip,tip-direction*2*unit-cross*unit);
   }
  }
 }
}
