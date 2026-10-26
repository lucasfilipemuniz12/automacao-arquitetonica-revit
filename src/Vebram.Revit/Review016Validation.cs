using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class Review016Validation
{
 public static void Run(Document doc,string output)
 {
  Review015Validation.Run(doc,output);
  var views=LegalCatalog.Views(doc);var sections=views.OfType<ViewSection>().Where(v=>LegalCatalog.IsLegal(v)&&!v.Name.StartsWith("RESERVA")).ToList();
  var details=new List<object>();
  foreach(var view in sections)
  {
   var notes=new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(DrawingOwnership.IsAnnotation).Select(n=>n.Text).ToList();
   var dims=new FilteredElementCollector(doc,view.Id).OfClass(typeof(Dimension)).Cast<Dimension>().Where(DrawingOwnership.IsAnnotation).ToList();
   if(!notes.Any(n=>n.Contains("ALTURA TOTAL"))||dims.Count==0||dims.Any(d=>!d.Value.HasValue||d.Value<=0))throw new Exception("Altura total ausente/inválida: "+view.Name);
   if(view.ViewType==ViewType.Section&&!notes.Any(n=>n.Contains("PD ")))throw new Exception("Ambientes cortados não identificados: "+view.Name);
   details.Add(new{view.Name,Dimensions=dims.Count,Notes=notes});
  }
  var roofViews=views.Where(v=>v.Name=="LEGAL - Implantação"||v.Name.StartsWith("LEGAL - Cobertura")).ToList();
  foreach(var view in roofViews)
  {
   var notes=new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(DrawingOwnership.IsAnnotation).Select(n=>n.Text).ToList();
   if(notes.Any(n=>System.Text.RegularExpressions.Regex.IsMatch(n,@"(?<![\d,])0,0%"))||!notes.Any(n=>n.Contains("caimento a definir")||n.Contains("(projeto)")))throw new Exception("Representação de caimento incorreta: "+view.Name);
  }
  var urban=LegalCatalog.Reference(views,LegalCatalog.Items.First(i=>i.Key=="urbanistico"))!;
  var texts=new FilteredElementCollector(doc,urban.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(DrawingOwnership.IsAnnotation).Select(n=>n.Text.Trim()).ToList();
  if(new[]{"Índice","Permitido","Projeto","Situação"}.Any(t=>!texts.Contains(t)))throw new Exception("Quadro sem quatro colunas.");
  // Explicit per-water proposals: do not copy to another surface, and share between views.
  using(var group=new TransactionGroup(doc,"Teste isolado de caimentos por água"))
  {
   group.Start();var saved=Implantation.Saved(doc)!;var plan=Implantation.Plan(doc,saved);var waters=RoofWaters.Read(doc,plan.Roofs);var flats=waters.Where(w=>w.Slope<.05).ToList();
   if(flats.Count<2)throw new Exception("Teste de caimentos exige pelo menos duas águas planas; compartilhamento não foi validado.");
   if(flats.Count>=2)
   {
    var proposal=saved with{Drainage=new(){new(flats[0].Key,7,"Esquerda"),new(flats[1].Key,3,"Direita")}};
    Implantation.Apply(doc,Implantation.Plan(doc,proposal));LegalModules.Apply(doc,new("cobertura",saved.SourceId,saved.FootprintId));
    foreach(var view in roofViews){var notes=new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(DrawingOwnership.IsAnnotation).Select(n=>n.Text).ToList();if(!notes.Any(n=>n.Contains("7,0% (projeto)")))throw new Exception("Proposta não compartilhada: "+view.Name);}
   }
   group.RollBack();
  }
  foreach(var kind in new[]{"implantacao","situacao","cobertura","planta","corte","fachadas","urbanistico","areas","folhas"})
  {
   var guide=new LegalGuide(kind){Width=430,Height=700,Margin=new Thickness(0)};guide.Measure(new System.Windows.Size(430,700));guide.Arrange(new Rect(0,0,430,700));guide.UpdateLayout();
   var bitmap=new RenderTargetBitmap(430,700,96,96,PixelFormats.Pbgra32);bitmap.Render(guide);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(output,"GUIA_"+kind+".png"));encoder.Save(file);
  }
  File.WriteAllText(Path.Combine(output,"REVISAO_016.json"),JsonSerializer.Serialize(new{Version="0.5.16",Native=true,SectionsAndFacades=details,NoZeroSlope=true,SharedWaterProposals=true,FourColumns=true,IllustratedGuides=9},new JsonSerializerOptions{WriteIndented=true}));
 }
}

