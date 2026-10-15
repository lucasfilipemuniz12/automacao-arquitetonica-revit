using System.Windows;
using System.Windows.Controls;
namespace Vebram.Revit;

/// <summary>One row per deliverable, one column per LEG sheet: pick where each one goes. No Revit types here.</summary>
public sealed class SheetPlannerWindow:Window
{
 readonly List<(PlannerItem Item,List<(long SheetId,RadioButton Button)> Choices)> rows=[];
 public SheetPlan? Result{get;private set;}
 public SheetPlannerWindow(IReadOnlyList<PlannerItem> items,IReadOnlyList<PlannerSheet> sheets,IReadOnlyDictionary<string,long> defaults)
 {
  Title="VEBRAM — Organizar folhas legais";Width=Math.Min(1000,SystemParameters.WorkArea.Width-40);Height=Math.Min(720,SystemParameters.WorkArea.Height-40);
  WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var root=new DockPanel{Margin=new Thickness(22)};Content=root;
  var heading=new TextBlock{Text="Em qual folha vai cada entregável?",FontSize=22,Margin=new Thickness(0,0,0,6)};DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
  var hint=new TextBlock{Text="Marque uma folha por linha, ou \"Nenhuma\" para não mexer. O que já está em outra folha é movido. A escala nunca muda. Se algo não couber, nada é alterado.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,14)};DockPanel.SetDock(hint,Dock.Top);root.Children.Add(hint);
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,14,0,0)};DockPanel.SetDock(buttons,Dock.Bottom);root.Children.Add(buttons);
  var apply=new Button{Content="Distribuir nas folhas",Padding=new Thickness(16,8,16,8),Margin=new Thickness(0,0,10,0)};var cancel=new Button{Content="Cancelar",Padding=new Thickness(16,8,16,8),IsCancel=true};buttons.Children.Add(apply);buttons.Children.Add(cancel);
  var grid=new System.Windows.Controls.Grid();root.Children.Add(new ScrollViewer{Content=grid,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star),MinWidth=240});
  for(int c=0;c<sheets.Count+1;c++)grid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto,MinWidth=110});
  grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  void Cell(UIElement element,int row,int column){System.Windows.Controls.Grid.SetRow(element,row);System.Windows.Controls.Grid.SetColumn(element,column);grid.Children.Add(element);}
  Cell(new TextBlock{Text="Entregável",FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,8)},0,0);
  Cell(new TextBlock{Text="Nenhuma",FontWeight=FontWeights.SemiBold,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(8,0,8,8)},0,1);
  for(int s=0;s<sheets.Count;s++)Cell(new TextBlock{Text=sheets[s].Number+"\n"+sheets[s].Name,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap,MaxWidth=130,TextAlignment=TextAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(8,0,8,8)},0,s+2);
  for(int r=0;r<items.Count;r++)
  {
   var item=items[r];grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
   string note=item.CurrentSheetId is {} current&&sheets.FirstOrDefault(x=>x.Id==current) is {} placed?"  (hoje na "+placed.Number+")":"  (ainda sem folha)";
   Cell(new TextBlock{Text=item.Label+note,Margin=new Thickness(0,6,12,6),VerticalAlignment=VerticalAlignment.Center,TextWrapping=TextWrapping.Wrap},r+1,0);
   var none=new RadioButton{GroupName=item.Key,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};Cell(none,r+1,1);
   var choices=new List<(long,RadioButton)>();
   for(int s=0;s<sheets.Count;s++){var radio=new RadioButton{GroupName=item.Key,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};choices.Add((sheets[s].Id,radio));Cell(radio,r+1,s+2);}
   long? start=item.CurrentSheetId??(defaults.TryGetValue(item.Key,out long suggested)?(long?)suggested:null);
   var chosen=start.HasValue?choices.FirstOrDefault(c=>c.Item1==start.Value).Item2:null;(chosen??none).IsChecked=true;
   rows.Add((item,choices));
  }
  apply.Click+=(_,_)=>
  {
   var plan=new Dictionary<long,long>();
   foreach(var row in rows)foreach(var choice in row.Choices)if(choice.Button.IsChecked==true)plan[row.Item.ViewId]=choice.SheetId;
   if(plan.Count==0){MessageBox.Show(this,"Marque pelo menos um entregável em uma folha.","VEBRAM",MessageBoxButton.OK,MessageBoxImage.Information);return;}
   Result=new SheetPlan(plan);DialogResult=true;
  };
 }
}
