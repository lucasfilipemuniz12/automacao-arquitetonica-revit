using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using ComboBox=System.Windows.Controls.ComboBox;
using TextBox=System.Windows.Controls.TextBox;
namespace Vebram.Revit;
internal sealed class RoofDrainageWindow : Window
{
 public List<RoofDrainageChoice>? Choices{get;private set;}
 public RoofDrainageWindow(Window owner,Document doc,IReadOnlyList<RoofWater> waters,ImplantationOptions? saved,List<RoofDrainageChoice>? edited)
 {
  Owner=owner;Title="Caimentos por água";WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var body=new StackPanel{Margin=new Thickness(24)};body.Children.Add(new TextBlock{Text="Caimento de cada água",FontSize=24});body.Children.Add(new TextBlock{Text="Água é cada superfície do telhado. Os valores abaixo anotam a proposta; a geometria não é alterada. Deixe vazio se ainda não foi definido.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,16)});
  var defaults=RoofDrainage.Choices(doc,waters,saved);var rows=new List<(RoofWater Water,TextBox Slope,ComboBox Flow)>();int index=0;
  foreach(var water in waters)
  {
   index++;var choice=edited?.FirstOrDefault(c=>c.Key==water.Key)??defaults.First(c=>c.Key==water.Key);bool flat=water.Slope<.05;
   body.Children.Add(new TextBlock{Text=$"A{index} · {water.RoofName}",FontWeight=FontWeights.Bold,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,5)});
   body.Children.Add(new TextBlock{Text=$"Centro no modelo: X {Implantation.Meters(water.Center.X):0.00} m · Y {Implantation.Meters(water.Center.Y):0.00} m",FontSize=11});
   if(!flat){body.Children.Add(new TextBlock{Text=$"{water.Slope:0.0}% · sentido lido da geometria",Margin=new Thickness(0,5,0,8)});continue;}
   body.Children.Add(new TextBlock{Text="Caimento de projeto (%)"});var slope=new TextBox{Text=choice.Slope?.ToString("0.###",Implantation.Pt)??"",MinHeight=32};body.Children.Add(slope);
   body.Children.Add(new TextBlock{Text="Sentido da seta no desenho",Margin=new Thickness(0,7,0,3)});var flow=new ComboBox{ItemsSource=new[]{"Modelo","Direita","Esquerda","Acima","Abaixo"},SelectedItem=choice.Flow,MinHeight=32};body.Children.Add(flow);rows.Add((water,slope,flow));
  }
  var apply=new Button{Content="Usar estes caimentos",Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,20,0,0)};body.Children.Add(apply);
  apply.Click+=(_,_)=>{try{var choices=new List<RoofDrainageChoice>();foreach(var row in rows){double? slope=null;if(!string.IsNullOrWhiteSpace(row.Slope.Text)){if(!double.TryParse(row.Slope.Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var n))throw new ArgumentException("Confira o percentual de "+row.Water.RoofName);slope=n;}var choice=new RoofDrainageChoice(row.Water.Key,slope,row.Flow.SelectedItem as string??"Modelo");_=RoofDrainageRules.Resolve(0,"Modelo",choice);choices.Add(choice);}Choices=choices;DialogResult=true;}catch(ArgumentException ex){MessageBox.Show(this,ex.Message,"Conferir caimento");}};
  LegalGuide.Attach(this,body,"cobertura");
 }
}
