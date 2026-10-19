using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Vebram.Revit;

/// <summary>Native regression on an explicit test copy; no save or Revit export.</summary>
internal static class CasaTestRegression
{
 internal static string Run(UIApplication ui,string output)
 {
  var doc=ui.ActiveUIDocument.Document;
  if(!Path.GetFileNameWithoutExtension(doc.PathName).Contains("_DIAGNOSTICO",StringComparison.OrdinalIgnoreCase))
   throw new InvalidOperationException("Abra uma cópia com _DIAGNOSTICO no nome.");
  Directory.CreateDirectory(output);
  var checks=new List<string>();
  void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);checks.Add(label);}
  var physical=SectionValidation.Physical(doc);
  var marks=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e.Category?.Id.Value is (long)BuiltInCategory.OST_Doors or (long)BuiltInCategory.OST_Windows).ToDictionary(e=>e.Id,e=>e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString());
  var registration=ProjectRegistrationStore.Read(doc);
  var library=File.Exists(MunicipalLibrary.FilePath)?File.ReadAllText(MunicipalLibrary.FilePath):null;
  var originalView=ui.ActiveUIDocument.ActiveView;
  using var group=new TransactionGroup(doc,"VEBRAM — regressão em cópia (revertida)");group.Start();
  try
  {
   var lot=Implantation.Saved(doc)??throw new InvalidOperationException("Prepare a Implantação na cópia de teste.");
   var source=(ViewPlan)doc.GetElement(new ElementId(lot.SourceId));
   var first=OpeningSchedules.Apply(doc,source);
   var again=OpeningSchedules.Apply(doc,source);
   Check(first.ViewId==again.ViewId,"Quadros repetidos reutilizam a mesma vista");
   var schedules=new[]{OpeningSchedules.Doors,OpeningSchedules.Windows}.Select(name=>new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().Single(v=>v.Name==name)).ToList();
   foreach(var schedule in schedules)
   {
    Check(new FilteredElementCollector(doc,schedule.Id).WhereElementIsNotElementType().OfType<FamilyInstance>().Any(),"Tabela contém esquadrias: "+schedule.Name);
    PrepareLegalCommand.OpenModuleResult(ui.ActiveUIDocument,schedule.Id.Value);
    Check(ui.ActiveUIDocument.ActiveView.Id==schedule.Id,"Tabela abre sem zoom gráfico: "+schedule.Name);
   }
   PrepareLegalCommand.OpenModuleResult(ui.ActiveUIDocument,source.Id.Value);
   Check(ui.ActiveUIDocument.ActiveView.Id==source.Id,"Navegação de planta continua funcionando");
   var items=SheetPlanner.Items(doc);var ids=items.Select(i=>i.ViewId).ToList();
   Check(items.Count(i=>i.Key is "portas" or "janelas")==2,"Quadros aparecem no organizador");
   var scales=items.Where(i=>!i.Schedule).ToDictionary(i=>i.ViewId,i=>((View)doc.GetElement(new ElementId(i.ViewId))).Scale);
   var viewIds=LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order().ToList();
   var sheetIds=SheetPlanner.Sheets(doc).Select(s=>s.Id.Value).Order().ToList();
   var separate=SheetPlanner.Preview(doc,"preferencial",ids,false);
   var plan=SheetPlanner.Preview(doc,"preferencial",ids,true);
   Check(plan.Pages.Count<separate.Pages.Count&&plan.Warnings.Any(w=>w.Contains("incorporada")),"Otimização remove folha nova vazia com dependências nativas");
   Check(viewIds.SequenceEqual(LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order())&&sheetIds.SequenceEqual(SheetPlanner.Sheets(doc).Select(s=>s.Id.Value).Order()),"Prévia reverte vistas e folhas temporárias");
   SheetPlanner.Apply(doc,plan);
   var repeat=SheetPlanner.Preview(doc,"preferencial",ids,true);SheetPlanner.Apply(doc,repeat);
   Check(repeat.Pages.All(p=>!p.IsNew),"Reaplicação não cria folhas duplicadas");
   Check(scales.All(p=>((View)doc.GetElement(new ElementId(p.Key))).Scale==p.Value),"Escalas preservadas");
   using(var pinGroup=new TransactionGroup(doc,"Regressão — vista fixada"))
   {
    pinGroup.Start();var viewport=new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().First(v=>ids.Contains(v.ViewId.Value));
    using(var tx=new Transaction(doc,"Fixar apenas no teste")){tx.Start();viewport.Pinned=true;tx.Commit();}
    bool refused=false;try{SheetPlanner.Preview(doc,"preferencial",ids);}catch(InvalidOperationException ex) when(ex.Message.Contains("desfixe")){refused=true;}
    Check(refused,"Vista fixada continua protegida");pinGroup.RollBack();
   }
   using(var manualGroup=new TransactionGroup(doc,"Regressão — nota manual"))
   {
    manualGroup.Start();var sheet=SheetPlanner.Sheets(doc).First();var area=LegalSheetComposition.Area(doc,sheet);TextNote note;
    using(var tx=new Transaction(doc,"Nota apenas no teste")){tx.Start();note=TextNote.Create(doc,sheet.Id,new XYZ(area.Left,area.Top,0),"REGRESSÃO — CONTEÚDO MANUAL",LegalStyles.Text(doc,2).Id);tx.Commit();}
    string fingerprint=DrawingOwnership.Fingerprint(note);string sheetName=sheet.Name;
    var manualPlan=SheetPlanner.Preview(doc,"preferencial",ids);SheetPlanner.Apply(doc,manualPlan);
    Check(note.IsValidObject&&DrawingOwnership.Fingerprint(note)==fingerprint&&sheet.Name==sheetName,"Nota manual e nome de sua folha preservados");manualGroup.RollBack();
   }
   // Exercise the actual button and render its WPF preview, without exporting Revit views.
   var window=new SheetPlannerWindow(SheetPlanner.Items(doc),(profile,selected,optimize)=>SheetPlanner.Preview(doc,profile,selected,optimize));
   try
   {
    var root=(DockPanel)window.Content;root.Background=Brushes.White;
    IEnumerable<DependencyObject> Descendants(DependencyObject parent){for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
    void Layout(){root.Measure(new System.Windows.Size(1180,750));root.Arrange(new System.Windows.Rect(0,0,1180,750));root.UpdateLayout();}
    Layout();var buttons=Descendants(root).OfType<Button>().ToList();
    buttons.First(b=>b.Content as string=="Calcular prévia").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Check(buttons.First(b=>b.Content as string=="Aplicar esta distribuição").IsEnabled,"Botão Calcular prévia habilita Aplicar");
    Layout();var bitmap=new RenderTargetBitmap(1180,750,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(output,"PREVIA_INTERFACE_0526.png")))encoder.Save(file);
   }
   finally{window.Close();}
   var savedUrban=LegalContext.Saved(doc,"urbanistico");
   if(savedUrban!=null)Check(ProjectDataIntegration.Context(registration,"urbanistico",lot.SourceId,savedUrban)==savedUrban,"Cadastro preserva opções já salvas no quadro urbanístico");
   var suggestion=ProjectDataIntegration.Context(registration,"urbanistico",lot.SourceId,null);
   Check(!ProjectDataIntegration.Matches(registration)||registration.Profile==null||suggestion.RegistrationText.Contains(registration.Profile.Title),"Perfil compatível identificado nas sugestões");
   Check(physical.SequenceEqual(SectionValidation.Physical(doc)),"Modelagem física preservada");
   Check(marks.All(p=>doc.GetElement(p.Key)?.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString()==p.Value),"Marcas de portas e janelas preservadas");
   Check(JsonSerializer.Serialize(registration)==JsonSerializer.Serialize(ProjectRegistrationStore.Read(doc)),"Cadastro do projeto preservado");
   Check(library==(File.Exists(MunicipalLibrary.FilePath)?File.ReadAllText(MunicipalLibrary.FilePath):null),"Biblioteca municipal preservada");
   var report=JsonSerializer.Serialize(new{Checks=checks,SeparatePages=separate.Pages.Count,OptimizedPages=plan.Pages.Count,Plan=plan},new JsonSerializerOptions{WriteIndented=true});
   File.WriteAllText(Path.Combine(output,"REGRESSAO_CASA_0526.json"),report);return report;
  }
  finally
  {
   ui.ActiveUIDocument.ActiveView=originalView;
   group.RollBack();
  }
 }
}
