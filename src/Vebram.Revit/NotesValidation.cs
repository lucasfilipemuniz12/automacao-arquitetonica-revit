using Autodesk.Revit.DB;
using System.IO;
namespace Vebram.Revit;
/// <summary>On a copy only: writes obviously fictitious test notes, redraws, checks repetition and exports the sheet.</summary>
internal static class NotesValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);
  using(var tx=new Transaction(doc,"Teste de notas")){tx.Start();SheetNotes.SaveUser(doc,new("TESTE nota geral",new(){["LEG-01"]="TESTE nota da folha 1\nTESTE segunda linha"}));tx.Commit();}
  var first=SheetNotes.Refresh(doc);var second=SheetNotes.Refresh(doc);
  var sheet=SheetPlanner.Sheets(doc).First(s=>s.SheetNumber=="LEG-01");
  List<TextNote> Notes()=>new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>n.Text.Contains("TESTE nota geral")).ToList();
  if(Notes().Count!=1)throw new Exception("Notas ausentes ou duplicadas na LEG-01: "+Notes().Count+"\n"+first+"\n"+second);
  if(!Notes()[0].Text.Contains("TESTE nota da folha 1"))throw new Exception("Nota da folha não entrou.");
  var other=SheetPlanner.Sheets(doc).FirstOrDefault(s=>s.SheetNumber!="LEG-01");
  if(other!=null&&new FilteredElementCollector(doc,other.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Any(n=>n.Text.Contains("TESTE nota da folha 1")))throw new Exception("Nota de uma folha apareceu em outra.");
  if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo alterado.");
  File.WriteAllText(Path.Combine(output,"RELATORIO.txt"),first+"\n---\n"+second+"\nTexto na LEG-01:\n"+Notes()[0].Text);
  using var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,"FOLHA"),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2800};
  options.SetViewsAndSheets(new List<ElementId>{sheet.Id});doc.ExportImage(options);
  doc.Save();
 }
}
