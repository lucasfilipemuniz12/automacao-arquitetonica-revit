using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ComboBox=System.Windows.Controls.ComboBox;
using TextBox=System.Windows.Controls.TextBox;
namespace Vebram.Revit;
public sealed record LegalPreferences(string? DimensionType=null,int Scale=100,string AdditionalNote="",bool InternalDimensions=true,bool WallThickness=false,bool OpeningWidths=false);
public static class LegalPreferenceStore
{
 static readonly Guid Id=new("d5ad57a0-fdc0-4a5d-888a-5c0e5810ef56");
 public static LegalPreferences Read(Document doc)
 {
  var schema=Schema.Lookup(Id);if(schema==null)return new();
  var storage=new FilteredElementCollector(doc).OfClass(typeof(DataStorage)).Cast<DataStorage>().FirstOrDefault(e=>e.GetEntity(schema).IsValid());
  return storage==null?new():JsonSerializer.Deserialize<LegalPreferences>(storage.GetEntity(schema).Get<string>(schema.GetField("Json")))??new();
 }
 public static void Save(Document doc,LegalPreferences prefs)
 {
  var schema=Schema.Lookup(Id);if(schema==null){var b=new SchemaBuilder(Id);b.SetSchemaName("VebramLegalPreferences");b.AddSimpleField("Json",typeof(string));schema=b.Finish();}
  var storage=new FilteredElementCollector(doc).OfClass(typeof(DataStorage)).Cast<DataStorage>().FirstOrDefault(e=>e.GetEntity(schema).IsValid())??DataStorage.Create(doc);
  var entity=new Entity(schema);entity.Set(schema.GetField("Json"),JsonSerializer.Serialize(prefs));storage.SetEntity(entity);
 }
 public static DimensionType Dimension(Document doc)
 {
  var prefs=Read(doc);if(prefs.DimensionType!=null&&doc.GetElement(prefs.DimensionType) is DimensionType selected&&selected.StyleType==DimensionStyleType.Linear)return selected;
  const string name="ARQ - Legal m 0,01 - VEBRAM";
  var type=new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>().FirstOrDefault(t=>t.Name==name);
  if(type==null){var source=new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(t=>t.StyleType==DimensionStyleType.Linear).OrderBy(t=>t.Name.StartsWith("ARQ")?0:1).First();type=(DimensionType)source.Duplicate(name);type.SetUnitsFormatOptions(new FormatOptions(UnitTypeId.Meters){UseDefault=false,Accuracy=.01,SuppressTrailingZeros=false});}
  return type;
 }
 public static string UnitLabel(Document doc,DimensionType type)
 {var f=type.GetUnitsFormatOptions();var id=(f.UseDefault?doc.GetUnits().GetFormatOptions(SpecTypeId.Length):f).GetUnitTypeId();return id==UnitTypeId.Meters?"m":id==UnitTypeId.Centimeters?"cm":id==UnitTypeId.Millimeters?"mm":LabelUtils.GetLabelForUnit(id);}
}
[Transaction(TransactionMode.Manual)]
public sealed class LegalPreferencesCommand:IExternalCommand
{
 public Result Execute(ExternalCommandData data,ref string message,ElementSet elements)
 {
  try
  {
   var doc=data.Application.ActiveUIDocument.Document;var prefs=LegalPreferenceStore.Read(doc);
   var window=new Window{Title="VEBRAM — Preferências do Projeto legal",Width=610,Height=535,WindowStartupLocation=WindowStartupLocation.CenterOwner};new WindowInteropHelper(window).Owner=data.Application.MainWindowHandle;
   var body=new StackPanel{Margin=new Thickness(24)};window.Content=body;
   body.Children.Add(new TextBlock{Text="Preferências deste projeto",FontSize=22,Margin=new Thickness(0,0,0,15)});
   body.Children.Add(new TextBlock{Text="Estilo das cotas geradas"});
   var choices=new List<SourceChoice>{new(0,"Padrão VEBRAM — metros, 0,01 m")};choices.AddRange(new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(t=>t.StyleType==DimensionStyleType.Linear).OrderBy(t=>t.Name).Select(t=>new SourceChoice(t.Id.Value,t.Name+" · "+LegalPreferenceStore.UnitLabel(doc,t))));
   var styles=new ComboBox{ItemsSource=choices,MinHeight=32,Margin=new Thickness(0,5,0,10)};styles.SelectedItem=choices.FirstOrDefault(c=>doc.GetElement(new ElementId(c.Id))?.UniqueId==prefs.DimensionType)??choices[0];body.Children.Add(styles);
   body.Children.Add(new TextBlock{Text="Escala dos novos entregáveis"});var scales=new ComboBox{ItemsSource=new[]{50,100,200},SelectedItem=prefs.Scale,MinHeight=32,Margin=new Thickness(0,5,0,10)};body.Children.Add(scales);
   var internalDims=new CheckBox{Content="Cotar dimensões internas dos ambientes (faces de acabamento)",IsChecked=prefs.InternalDimensions,Margin=new Thickness(0,3,0,5)};body.Children.Add(internalDims);
   var thickness=new CheckBox{Content="Cotar espessuras de paredes (opcional)",IsChecked=prefs.WallThickness,Margin=new Thickness(0,3,0,5)};body.Children.Add(thickness);
   var openings=new CheckBox{Content="Cotar larguras de portas/janelas com referências válidas (opcional)",IsChecked=prefs.OpeningWidths,Margin=new Thickness(0,3,0,10)};body.Children.Add(openings);
   body.Children.Add(new TextBlock{Text="Informação adicional no desenho (opcional)"});var note=new TextBox{Text=prefs.AdditionalNote,MinHeight=60,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,10)};body.Children.Add(note);
   body.Children.Add(new TextBlock{Text="As escolhas valem para a próxima geração. Marcas manuais são preservadas; a unidade acompanha o estilo escolhido.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)});
   var save=new Button{Content="Salvar preferências",Padding=new Thickness(12,7,12,7),HorizontalAlignment=HorizontalAlignment.Right};body.Children.Add(save);save.Click+=(_,_)=>window.DialogResult=true;
   if(window.ShowDialog()!=true)return Result.Cancelled;
   using var tx=new Transaction(doc,"VEBRAM — preferências legais");tx.Start();var selected=(SourceChoice)styles.SelectedItem;
   LegalPreferenceStore.Save(doc,new(selected.Id==0?null:doc.GetElement(new ElementId(selected.Id)).UniqueId,(int)scales.SelectedItem,note.Text.Trim(),internalDims.IsChecked==true,thickness.IsChecked==true,openings.IsChecked==true));tx.Commit();return Result.Succeeded;
  }
  catch(Exception e){message=e.Message;return Result.Failed;}
 }
}

