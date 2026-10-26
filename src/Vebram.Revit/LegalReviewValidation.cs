using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class LegalReviewValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);
  IntegrationValidation.Run(doc,output);
  var lot=Implantation.Saved(doc)!;var source=(ViewPlan)doc.GetElement(new ElementId(lot.SourceId));
  var results=new[]{LegalModules.Apply(doc,new("planta",lot.SourceId,lot.FootprintId,true)),LegalModules.Apply(doc,new("cobertura",lot.SourceId,lot.FootprintId,true))};
  var roofView=(ViewPlan)doc.GetElement(new ElementId(results[1].ViewId));
  var sectionCategory=new ElementId(BuiltInCategory.OST_Sections);
  if(!roofView.GetCategoryHidden(sectionCategory))throw new Exception("Marcas de corte visíveis na cobertura.");
  var waters=RoofWaters.Read(doc,new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType().ToList());
  var outlines=waters.SelectMany(w=>w.Loops??Array.Empty<RoofLoop>()).ToList();
  var smallRoofs=outlines.Where(b=>b.Max.X-b.Min.X<Implantation.Feet(.06)*lot.Scale&&b.Max.Y-b.Min.Y<Implantation.Feet(.06)*lot.Scale).ToList();
  var implantation=LegalCatalog.Views(doc).First(v=>v.Name=="LEGAL - Implantação");
  var labels=new FilteredElementCollector(doc,implantation.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>System.Text.RegularExpressions.Regex.IsMatch(n.Text,"^A[0-9]+ · ")).ToList();
  bool overlap=false;foreach(var note in labels)if(note.get_BoundingBox(implantation) is {} a&&smallRoofs.Any(b=>a.Min.X<b.Max.X&&a.Max.X>b.Min.X&&a.Min.Y<b.Max.Y&&a.Max.Y>b.Min.Y))overlap=true;
  if(smallRoofs.Count==0)throw new Exception("Nenhum contorno pequeno foi exercitado no teste da caixa.");
  foreach(var note in labels)if(note.get_BoundingBox(implantation) is {} box&&outlines.SelectMany(l=>l.Edges).Any(line=>PlanAnnotations.TouchesLine(new PlanAnnotations.Rect(box.Min.X,box.Min.Y,box.Max.X,box.Max.Y),line,0)))throw new Exception("Rótulo cruza borda da cobertura.");
  var guideLines=new FilteredElementCollector(doc,implantation.Id).OfClass(typeof(CurveElement)).Cast<CurveElement>().Select(c=>c.GeometryCurve).OfType<Line>().Where(l=>l.IsBound).ToList();
  foreach(var note in labels)if(note.get_BoundingBox(implantation) is {} noteBox){var collisions=guideLines.Where(line=>PlanAnnotations.TouchesLine(new PlanAnnotations.Rect(noteBox.Min.X,noteBox.Min.Y,noteBox.Max.X,noteBox.Max.Y),line,0)).ToList();if(collisions.Count>0)throw new Exception("Rótulo cruza linha: "+note.Text+" caixa="+noteBox.Min+" / "+noteBox.Max+" linhas="+string.Join(";",collisions.Select(l=>l.GetEndPoint(0)+" / "+l.GetEndPoint(1))));}
  var dimensionBounds=new FilteredElementCollector(doc,implantation.Id).OfClass(typeof(Dimension)).Select(d=>d.get_BoundingBox(implantation)).Where(b=>b!=null).ToList();
  foreach(var note in labels)if(note.get_BoundingBox(implantation) is {} box&&dimensionBounds.Any(b=>box.Min.X<b!.Max.X&&box.Max.X>b.Min.X&&box.Min.Y<b.Max.Y&&box.Max.Y>b.Min.Y))throw new Exception("Rótulo sobrepõe cota da implantação.");
  if(overlap)throw new Exception("Rótulo de caimento ainda dentro de pequena cobertura.");
  MemoryAreasValidation.Run(doc,output);
  if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo físico alterado na revisão.");
  File.WriteAllText(Path.Combine(output,"REVISAO_LEGAL_025.json"),JsonSerializer.Serialize(new{Version="0.5.25",PhysicalPreserved=true,PlantRegenerated=true,RoofRegenerated=true,RoofSectionMarksHidden=true,SmallRoofLabelsClear=true,DrawingLinesClear=true,DimensionBoundsClear=true,DrainageLabels=labels.Count,SmallRoofs=smallRoofs.Count,Reports=results.Select(r=>r.Report)},new JsonSerializerOptions{WriteIndented=true}));
 }
}
