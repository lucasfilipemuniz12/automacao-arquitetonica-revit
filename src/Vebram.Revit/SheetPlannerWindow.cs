using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace Vebram.Revit;

public sealed class SheetPlannerWindow:Window
{
 public SheetPlan? Result{get;private set;}
 readonly WrapPanel previews=new(){Orientation=Orientation.Horizontal};
 readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};
 public SheetPlannerWindow(IReadOnlyList<PlannerItem> items,Func<string,IEnumerable<long>,bool,SheetPlan> calculate,IReadOnlyList<string>? unprepared=null)
 {
  Title="VEBRAM — Distribuição automática de folhas";Width=Math.Min(1220,SystemParameters.WorkArea.Width-40);Height=Math.Min(850,SystemParameters.WorkArea.Height-40);WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var root=new DockPanel{Margin=new Thickness(22)};Content=root;
  var heading=new StackPanel();DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
  heading.Children.Add(new TextBlock{Text="Organize o conjunto antes de aplicar",FontSize=25,FontWeight=FontWeights.SemiBold});
  heading.Children.Add(new TextBlock{Text="Prévia da folha inteira: vistas, quadros laterais e notas. Escalas preservadas. Cinza = conteúdo existente ou identificação reservada. Novas folhas somente quando necessário.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,10)});
  var profile=new ComboBox{ItemsSource=new[]{"Preferencial — Implantação + Situação; Cortes A/B; quatro Fachadas","Referência R20 — Situação; Plantas; Implantação/Cobertura/Cortes; Fachadas"},SelectedIndex=0,MinHeight=34};heading.Children.Add(profile);
  var adaptive=new CheckBox{Content="Combinar assuntos compatíveis para aproveitar espaço (mantém grupos de cortes e fachadas)",IsChecked=true,Margin=new Thickness(0,8,0,5)};heading.Children.Add(adaptive);
  var footer=new DockPanel{Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);footer.Children.Add(status);
  var preview=new Button{Content="Calcular prévia",Padding=new Thickness(14,8,14,8),Margin=new Thickness(6)};var apply=new Button{Content="Aplicar esta distribuição",Padding=new Thickness(14,8,14,8),Margin=new Thickness(6),IsEnabled=false};var cancel=new Button{Content="Cancelar",Padding=new Thickness(14,8,14,8),Margin=new Thickness(6),IsCancel=true};actions.Children.Add(preview);actions.Children.Add(apply);actions.Children.Add(cancel);
  var body=new Grid();body.ColumnDefinitions.Add(new(){Width=new GridLength(245)});body.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});root.Children.Add(body);
  var choices=new StackPanel{Margin=new Thickness(0,12,12,0)};body.Children.Add(new ScrollViewer{Content=choices,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  choices.Children.Add(new TextBlock{Text="ENTREGÁVEIS PREPARADOS",FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,12)});
  var checks=new List<(PlannerItem Item,CheckBox Check)>();SheetPlan? calculated=null;
  void Invalidate(){calculated=null;apply.IsEnabled=false;previews.Children.Clear();status.Text="Calcule a prévia para conferir folhas e agrupamentos.";}
  foreach(var item in items){var check=new CheckBox{Content=new TextBlock{Text=item.Label,TextWrapping=TextWrapping.Wrap},IsChecked=true,Margin=new Thickness(0,5,0,5)};checks.Add((item,check));choices.Children.Add(check);check.Checked+=(_,_)=>Invalidate();check.Unchecked+=(_,_)=>Invalidate();}
  // Views skipped because they were never prepared are listed, so nothing disappears silently.
  if(unprepared?.Count>0)
  {
   choices.Children.Add(new TextBlock{Text="AINDA NÃO PREPARADOS (ficam fora)",FontWeight=FontWeights.SemiBold,Foreground=Brushes.DarkOrange,Margin=new Thickness(0,16,0,6)});
   foreach(var text in unprepared)choices.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DimGray,Margin=new Thickness(0,3,0,3)});
  }
  profile.SelectionChanged+=(_,_)=>Invalidate();adaptive.Checked+=(_,_)=>Invalidate();adaptive.Unchecked+=(_,_)=>Invalidate();var scroll=new ScrollViewer{Content=previews,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(scroll,1);body.Children.Add(scroll);
  preview.Click+=(_,_)=>
  {
   Invalidate();try{calculated=calculate(profile.SelectedIndex==1?"r20":"preferencial",checks.Where(c=>c.Check.IsChecked==true).Select(c=>c.Item.ViewId),adaptive.IsChecked==true);foreach(var page in calculated.Pages)previews.Children.Add(DrawPage(page));status.Text=$"{calculated.Pages.Count} folhas utilizadas · {calculated.Pages.Count(p=>p.IsNew)} novas"+(calculated.ReservedSheets?.Count>0?$" · {calculated.ReservedSheets.Count} existente(s) de reserva":"")+"\n"+(calculated.Warnings.Count==0?"Grupos preferenciais mantidos.":string.Join("\n",calculated.Warnings))+(unprepared?.Count>0?$"\nFora da distribuição (não preparados): {unprepared.Count}. Veja a lista à esquerda.":"");apply.IsEnabled=true;}
   catch(Exception ex){status.Text=ex.Message;}
  };
  apply.Click+=(_,_)=>{if(calculated==null)return;Result=calculated;DialogResult=true;};Invalidate();
 }
 public static FrameworkElement DrawPage(PlannerPage page)
 {
  var panel=new StackPanel{Width=430,Margin=new Thickness(8)};panel.Children.Add(new TextBlock{Text=page.Number+(page.IsNew?" · NOVA":" · existente"),FontSize=18,FontWeight=FontWeights.SemiBold});panel.Children.Add(new TextBlock{Text=page.Name,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,3,0,8)});
  var paper=page.SheetBounds??page.Area;double scale=400/(paper.Right-paper.Left),height=(paper.Top-paper.Bottom)*scale;var canvas=new Canvas{Width=400,Height=height,Background=Brushes.White,ClipToBounds=true};panel.Children.Add(new Border{Child=canvas,BorderBrush=Brushes.SlateGray,BorderThickness=new Thickness(1)});
  void Box(LegalPaperRect r,Brush fill,string label)
  {
   var border=new Border{Width=Math.Max(1,(r.Right-r.Left)*scale),Height=Math.Max(1,(r.Top-r.Bottom)*scale),Background=fill,BorderBrush=Brushes.Teal,BorderThickness=new Thickness(.5),ToolTip=label,Child=new TextBlock{Text=label,TextWrapping=TextWrapping.Wrap,FontSize=10,Margin=new Thickness(3),VerticalAlignment=VerticalAlignment.Center,TextAlignment=TextAlignment.Center}};Canvas.SetLeft(border,(r.Left-paper.Left)*scale);Canvas.SetTop(border,(paper.Top-r.Top)*scale);canvas.Children.Add(border);
  }
  foreach(var zone in page.Zones??[])Box(zone.Rect,zone.Writable?Brushes.WhiteSmoke:Brushes.Gainsboro,zone.Key=="desenhos"||page.Drawings.Any(d=>d.Zone==zone.Key)?"":zone.Key=="notas"&&page.Notes?.Count>0?"NOTAS\nAUTOMÁTICAS":zone.Label);
  foreach(var r in page.Obstacles)Box(r,Brushes.Gainsboro,"");foreach(var d in page.Drawings)Box(d.Rect,Brushes.Azure,d.Label+(d.Scale>0?"\n1:"+d.Scale:""));
  panel.Children.Add(new TextBlock{Text=$"{(paper.Right-paper.Left)*304.8:0} × {(paper.Top-paper.Bottom)*304.8:0} mm · {page.Drawings.Count(d=>d.Zone!="desenhos")} quadro(s) lateral(is)",FontSize=11,Foreground=Brushes.DimGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,4)});return panel;
 }
}
