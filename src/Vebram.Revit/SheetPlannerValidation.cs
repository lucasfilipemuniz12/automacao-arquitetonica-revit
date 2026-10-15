using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class SheetPlannerValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);var ids=SheetPlanner.Items(doc).Select(i=>i.ViewId).ToList();
  var elementIds=new FilteredElementCollector(doc).WhereElementIsNotElementType().Select(e=>e.UniqueId).Order().ToList();
  var before=SheetPlanner.Sheets(doc).Count;var plan=SheetPlanner.Preview(doc,"preferencial",ids);
  if(!elementIds.SequenceEqual(new FilteredElementCollector(doc).WhereElementIsNotElementType().Select(e=>e.UniqueId).Order()))throw new Exception("Prévia alterou elementos.");
  var r20=SheetPlanner.Preview(doc,"r20",ids);if(!r20.Pages.Any())throw new Exception("Sem prévia R20.");
  SheetPlanner.Apply(doc,plan);var count=SheetPlanner.Sheets(doc).Count;
  var repeat=SheetPlanner.Preview(doc,"preferencial",ids);SheetPlanner.Apply(doc,repeat);
  int autoNotes=new FilteredElementCollector(doc).OfClass(typeof(TextNote)).Count(SheetNotes.IsUnedited);
  SheetPlanner.Apply(doc,SheetPlanner.Preview(doc,"preferencial",ids));
  if(new FilteredElementCollector(doc).OfClass(typeof(TextNote)).Count(SheetNotes.IsUnedited)!=autoNotes)throw new Exception("Repetição duplicou notas automáticas.");
  if(SheetPlanner.Sheets(doc).Count!=count||repeat.Pages.Any(p=>p.IsNew))throw new Exception("Repetição criou folhas desnecessárias.");
  foreach(var page in repeat.Pages)
  {
   if(page.Drawings.Any(d=>!(page.Zones?.FirstOrDefault(z=>z.Key==d.Zone)?.Rect??page.Area).Contains(d.Rect,1e-6)))throw new Exception("Fora da folha.");
   for(int i=0;i<page.Drawings.Count;i++)for(int j=i+1;j<page.Drawings.Count;j++)if(page.Drawings[i].Rect.Intersects(page.Drawings[j].Rect,0))throw new Exception("Sobreposição.");
  }
  if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo físico alterado.");
  if(repeat.Pages.SelectMany(p=>p.Drawings).Count(d=>d.Zone!="desenhos")<2)throw new Exception("Quadros não foram colocados na lateral.");
  File.WriteAllText(Path.Combine(output,"PREVIA_CALCULADA.json"),JsonSerializer.Serialize(repeat,new JsonSerializerOptions{WriteIndented=true}));
  if(repeat.Pages.Any(p=>p.SheetBounds==null||p.Zones?.Count<4||p.Notes?.Count<2))throw new Exception("Prévia sem zonas/notas completas: "+string.Join("; ",repeat.Warnings));
  // Pinned placements must abort atomically, including any earlier removals.
  using(var group=new TransactionGroup(doc,"Teste isolado de vista fixada"))
  {
   group.Start();var vp=new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().First(v=>ids.Contains(v.ViewId.Value));
   using(var tx=new Transaction(doc,"Fixar teste")){tx.Start();vp.Pinned=true;tx.Commit();}
   bool blocked=false;try{SheetPlanner.Preview(doc,"preferencial",ids);}catch(InvalidOperationException){blocked=true;}
   if(!blocked)throw new Exception("Vista fixada não protegida.");group.RollBack();
  }
  using(var group=new TransactionGroup(doc,"Teste isolado de conteúdo manual"))
  {
   group.Start();var sheet=SheetPlanner.Sheets(doc).First();var area=LegalSheetComposition.Area(doc,sheet);TextNote note;
   using(var tx=new Transaction(doc,"Nota manual de teste")){tx.Start();note=TextNote.Create(doc,sheet.Id,new XYZ(area.Left,area.Top,0),"CONTEÚDO MANUAL — PRESERVAR",LegalStyles.Text(doc,2).Id);tx.Commit();}
   var fingerprint=DrawingOwnership.Fingerprint(note);var manualPlan=SheetPlanner.Preview(doc,"preferencial",ids);SheetPlanner.Apply(doc,manualPlan);
   if(DrawingOwnership.Fingerprint(note)!=fingerprint)throw new Exception("Nota manual alterada.");group.RollBack();
  }
  using(var group=new TransactionGroup(doc,"Teste isolado de escala e prévia antiga"))
  {
   group.Start();var view=(View)doc.GetElement(new ElementId(ids.First(i=>doc.GetElement(new ElementId(i)) is ViewPlan)));
   using(var tx=new Transaction(doc,"Alterar escala de teste")){tx.Start();view.Scale=1;tx.Commit();}
   bool blocked=false;try{SheetPlanner.Preview(doc,"preferencial",ids);}catch(InvalidOperationException){blocked=true;}
   if(!blocked)throw new Exception("Vista maior que folha não recusada.");group.RollBack();
  }
  using(var group=new TransactionGroup(doc,"Teste isolado de edição das notas automáticas"))
  {
   group.Start();var note=new FilteredElementCollector(doc).OfClass(typeof(TextNote)).Cast<TextNote>().First(SheetNotes.IsUnedited);
   using(var tx=new Transaction(doc,"Editar nota de teste")){tx.Start();note.Text+="\nNOTA EDITADA PELO USUÁRIO";tx.Commit();}
   string fingerprint=DrawingOwnership.Fingerprint(note);var edited=SheetPlanner.Preview(doc,"preferencial",ids);SheetPlanner.Apply(doc,edited);
   if(DrawingOwnership.Fingerprint(note)!=fingerprint)throw new Exception("Edição manual das notas foi sobrescrita.");group.RollBack();
  }
  // Exercise the actual WPF preview button while the Revit API context is valid.
  var window=new SheetPlannerWindow(SheetPlanner.Items(doc),(profile,selected)=>SheetPlanner.Preview(doc,profile,selected));
  var root=(System.Windows.Controls.DockPanel)window.Content;root.Background=System.Windows.Media.Brushes.White;
  IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject parent){for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);i++){var child=System.Windows.Media.VisualTreeHelper.GetChild(parent,i);yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
  root.Measure(new System.Windows.Size(1180,750));root.Arrange(new System.Windows.Rect(0,0,1180,750));root.UpdateLayout();
  var buttons=Descendants(root).OfType<System.Windows.Controls.Button>().ToList();buttons.First(b=>b.Content as string=="Calcular prévia").RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
  if(!buttons.First(b=>b.Content as string=="Aplicar esta distribuição").IsEnabled)throw new Exception("Prévia WPF não habilitou aplicação.");
  root.Measure(new System.Windows.Size(1180,750));root.Arrange(new System.Windows.Rect(0,0,1180,750));root.UpdateLayout();var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(1180,750,96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(output,"PREVIA_INTERFACE.png")))encoder.Save(file);window.Close();
  doc.Save();File.WriteAllText(Path.Combine(output,"PLANEJAMENTO_018.json"),JsonSerializer.Serialize(new{Version="0.5.18",SidebarTables=true,AutomaticNotes=true,WholeSheetPreview=true,PreviewRollback=true,Repeat=true,PinnedProtected=true,PhysicalPreserved=true,ManualContentPreserved=true,OversizeRejected=true,PreviewButtonValidated=true,Before=before,After=count,Preferred=plan,R20=r20},new JsonSerializerOptions{WriteIndented=true}));
  foreach(var page in repeat.Pages)
  {
   var sheet=SheetPlanner.Sheets(doc).First(s=>s.SheetNumber==page.Number);using var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,"FOLHA"),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2800};options.SetViewsAndSheets(new List<ElementId>{sheet.Id});doc.ExportImage(options);
  }
 }
}
