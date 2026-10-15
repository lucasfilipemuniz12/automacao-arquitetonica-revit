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
  var choices=Choices(doc,waters,options);double unit=Implantation.Feet(.001)*view.Scale,z=view.GenLevel.Elevation;var occupied=new List<BoundingBoxXYZ>();
  foreach(var existing in new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>())if(existing.get_BoundingBox(view) is {} bounds)occupied.Add(bounds);
  int index=0;foreach(var water in waters)
  {
   index++;var value=RoofDrainageRules.Resolve(water.Slope,"Modelo",choices.FirstOrDefault(c=>c.Key==water.Key));var center=new XYZ(water.Center.X,water.Center.Y,z);
   var caption="A"+index+" · "+(value.Slope.HasValue?value.Slope.Value.ToString("0.0",Implantation.Pt)+"%"+(value.FromModel?"":" (projeto)"):"caimento a definir");
   var note=TextNote.Create(doc,view.Id,center,Implantation.Feet(.038),caption,new TextNoteOptions(LegalStyles.Text(doc,2).Id){HorizontalAlignment=HorizontalTextAlignment.Center});made.Add(note);doc.Regenerate();
   bool Overlap(BoundingBoxXYZ a,BoundingBoxXYZ b)=>a.Min.X<b.Max.X+2*unit&&a.Max.X>b.Min.X-2*unit&&a.Min.Y<b.Max.Y+2*unit&&a.Max.Y>b.Min.Y-2*unit;
   var original=note.Coord;bool placed=false;
   foreach(var offset in new[]{XYZ.Zero,new XYZ(0,12*unit,0),new XYZ(0,-12*unit,0),new XYZ(-25*unit,0,0),new XYZ(25*unit,0,0),new XYZ(0,24*unit,0),new XYZ(0,-24*unit,0)})
   {note.Coord=original+offset;doc.Regenerate();var bounds=note.get_BoundingBox(view);if(bounds!=null&&!occupied.Any(b=>Overlap(bounds,b))){occupied.Add(bounds);placed=true;break;}}
   if(!placed){note.Coord=original+new XYZ(0,(index+2)*12*unit,0);doc.Regenerate();if(note.get_BoundingBox(view) is {} b)occupied.Add(b);}
   void Line(XYZ a,XYZ b){if(a.DistanceTo(b)>doc.Application.ShortCurveTolerance)made.Add(doc.Create.NewDetailCurve(view,Autodesk.Revit.DB.Line.CreateBound(a,b)));}
   if(note.Coord.DistanceTo(original)>3*unit)Line(center,note.Coord-new XYZ(0,3*unit,0));
   var direction=value.FromModel?water.Direction:Direction(value.Flow);
   if(direction!=null&&value.Slope.HasValue)
   {
    var bounds=note.get_BoundingBox(view)!;var origin=new XYZ((bounds.Min.X+bounds.Max.X)/2,bounds.Min.Y-5*unit,z);var tip=origin+direction*5*unit;var tail=origin-direction*5*unit;var cross=new XYZ(-direction.Y,direction.X,0);
    Line(tail,tip);Line(tip,tip-direction*2*unit+cross*unit);Line(tip,tip-direction*2*unit-cross*unit);
   }
  }
 }
}
