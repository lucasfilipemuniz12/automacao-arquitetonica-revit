using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
namespace Vebram.Revit;

public sealed class SheetPlannerWindow:Window
{
 public SheetPlan? Result{get;private set;}
 readonly WrapPanel previews=new(){Orientation=Orientation.Horizontal};
 readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};
 public SheetPlannerWindow(IReadOnlyList<PlannerItem> items,Func<string,IEnumerable<long>,bool,SheetPlan> calculate,IReadOnlyList<string>? unprepared=null,Func<string,IEnumerable<long>,bool,Dictionary<long,int>,SheetPlan>? calculateScaled=null,Func<string,IEnumerable<long>,bool,Dictionary<long,int>,List<PlannerMove>,SheetPlan>? calculateEdited=null)
 {
  Title="VEBRAM — Distribuição automática de folhas";Width=Math.Min(1220,SystemParameters.WorkArea.Width-40);Height=Math.Min(850,SystemParameters.WorkArea.Height-40);WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var root=new DockPanel{Margin=new Thickness(22)};Content=root;
  var heading=new StackPanel();DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
  heading.Children.Add(new TextBlock{Text="Organize o conjunto antes de aplicar",FontSize=25,FontWeight=FontWeights.SemiBold});
  heading.Children.Add(new TextBlock{Text="Calcule a prévia e clique numa vista para mudar sua escala. Arraste para uma área livre ou sobre outra vista para trocar as duas de lugar. As mudanças só entram no projeto ao aplicar.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,10)});
  var profile=new ComboBox{ItemsSource=new[]{"Preferencial — Implantação + Situação; Cortes A/B; quatro Fachadas","Referência R20 — Situação; Plantas; Implantação/Cobertura/Cortes; Fachadas"},SelectedIndex=0,MinHeight=34};heading.Children.Add(profile);
  var adaptive=new CheckBox{Content="Combinar assuntos compatíveis para aproveitar espaço (mantém grupos de cortes e fachadas)",IsChecked=true,Margin=new Thickness(0,8,0,5)};heading.Children.Add(adaptive);
  var editing=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,4,0,0)};
  var selectedLabel=new TextBlock{Text="Selecione uma vista na prévia",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,10,0)};
  var scalePicker=new ComboBox{MinWidth=95,MinHeight=27,IsEnabled=false};
  var undo=new Button{Content="Desfazer edição",Margin=new Thickness(10,0,0,0),Padding=new Thickness(8,3,8,3),IsEnabled=false};
  editing.Children.Add(selectedLabel);editing.Children.Add(scalePicker);editing.Children.Add(undo);heading.Children.Add(editing);
  var footer=new DockPanel{Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);footer.Children.Add(status);
  var preview=new Button{Content="Calcular prévia",Padding=new Thickness(14,8,14,8),Margin=new Thickness(6)};var apply=new Button{Content="Aplicar esta distribuição",Padding=new Thickness(14,8,14,8),Margin=new Thickness(6),IsEnabled=false};var cancel=new Button{Content="Cancelar",Padding=new Thickness(14,8,14,8),Margin=new Thickness(6),IsCancel=true};actions.Children.Add(preview);actions.Children.Add(apply);actions.Children.Add(cancel);
  var body=new Grid();body.ColumnDefinitions.Add(new(){Width=new GridLength(245)});body.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});root.Children.Add(body);
  var choices=new StackPanel{Margin=new Thickness(0,12,12,0)};body.Children.Add(new ScrollViewer{Content=choices,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  choices.Children.Add(new TextBlock{Text="ENTREGÁVEIS PREPARADOS",FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,12)});
  var checks=new List<(PlannerItem Item,CheckBox Check)>();SheetPlan? calculated=null;
  long? selectedView=null;
  bool syncingScale=false;
  var history=new Stack<SheetPlan>();
  var scroll=new ScrollViewer{Content=previews,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  void Invalidate()
  {
   calculated=null;selectedView=null;history.Clear();apply.IsEnabled=false;undo.IsEnabled=false;
   scalePicker.IsEnabled=false;scalePicker.ItemsSource=null;selectedLabel.Text="Selecione uma vista na prévia";
   previews.Children.Clear();status.Text="Calcule a prévia para conferir folhas e agrupamentos.";
  }
  foreach(var item in items){var check=new CheckBox{Content=new TextBlock{Text=item.Label,TextWrapping=TextWrapping.Wrap},IsChecked=true,Margin=new Thickness(0,5,0,5)};checks.Add((item,check));choices.Children.Add(check);check.Checked+=(_,_)=>Invalidate();check.Unchecked+=(_,_)=>Invalidate();}
  // Views skipped because they were never prepared are listed, so nothing disappears silently.
  if(unprepared?.Count>0)
  {
   choices.Children.Add(new TextBlock{Text="AINDA NÃO PREPARADOS (ficam fora)",FontWeight=FontWeights.SemiBold,Foreground=Brushes.DarkOrange,Margin=new Thickness(0,16,0,6)});
   foreach(var text in unprepared)choices.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DimGray,Margin=new Thickness(0,3,0,3)});
  }
  profile.SelectionChanged+=(_,_)=>Invalidate();adaptive.Checked+=(_,_)=>Invalidate();adaptive.Unchecked+=(_,_)=>Invalidate();
  Grid.SetColumn(scroll,1);body.Children.Add(scroll);
  if(calculateEdited!=null)scroll.AddHandler(DragDrop.PreviewDragOverEvent,new DragEventHandler((_,e)=>
  {
   if(!e.Data.GetDataPresent(DrawingDrag,false))return;
   double y=e.GetPosition(scroll).Y;
   if(y<24)scroll.LineUp();
   else if(y>scroll.ActualHeight-24)scroll.LineDown();
  }),true);
  void SyncScale()
  {
   var drawing=calculated?.Pages.SelectMany(p=>p.Drawings).FirstOrDefault(d=>d.ViewId==selectedView);
   syncingScale=true;
   try
   {
    selectedLabel.Text=drawing?.Label??"Selecione uma vista na prévia";
    scalePicker.IsEnabled=drawing is {Scale:>0}&&(calculateEdited!=null||
     calculateScaled!=null&&items.Any(i=>i.ViewId==drawing.ViewId&&i.Key=="implantacao"));
    var choicesForScale=calculateEdited==null?new[]{50,100,200}:new[]{20,25,50,75,100,125,150,200,250,500,1000};
    scalePicker.ItemsSource=drawing is {Scale:>0}?choicesForScale.Append(drawing.Scale).Distinct().Order().Select(n=>"1:"+n).ToArray():null;
    scalePicker.SelectedItem=drawing is {Scale:>0}?"1:"+drawing.Scale:null;
   }
   finally{syncingScale=false;}
  }
  void Render()
  {
   if(calculated==null)return;
   var offset=scroll.VerticalOffset;
   previews.Children.Clear();
   foreach(var page in calculated.Pages)
    previews.Children.Add(DrawPageCore(page,SelectDrawing,calculateEdited==null?null:MoveDrawing,selectedView));
   scroll.ScrollToVerticalOffset(offset);
   var summary=$"{calculated.Pages.Count} folha(s) utilizada(s) · {calculated.Pages.Count(p=>p.IsNew)} nova(s)";
   status.Text=summary+(calculated.Warnings.Count==0?"":"\n"+string.Join("\n",calculated.Warnings))+
    (unprepared?.Count>0?$"\nEntregáveis ainda não preparados: {unprepared.Count}.":"");
   apply.IsEnabled=true;undo.IsEnabled=history.Count>0;
   SyncScale();
  }
  void SelectDrawing(long id)
  {
   if(selectedView==id)return;
   selectedView=id;
   SyncScale();
   foreach(var panel in previews.Children.OfType<FrameworkElement>())
    if(panel.Tag is Dictionary<long,Button> buttons)
     foreach(var entry in buttons)entry.Value.Background=entry.Key==id?Brushes.LightBlue:Brushes.Azure;
  }
  void Recalculate(Dictionary<long,int> scales,List<PlannerMove> moves)
  {
   var previous=calculated;
   if(previous==null)return;
   try
   {
    var next=calculateEdited!=null?calculateEdited(previous.Profile,previous.ViewIds,previous.Optimize,scales,moves):
     calculateScaled!(previous.Profile,previous.ViewIds,previous.Optimize,scales);
    history.Push(previous);calculated=next;Render();
   }
   catch(Exception ex){status.Text=ex.Message;SyncScale();}
  }
  void MoveDrawing(PlannerMove move)
  {
   if(calculated==null||move.SwapWith==move.ViewId)return;
   var moves=calculated.Moves?.ToList()??[];
   moves.Add(move);
   Recalculate(calculated.ScaleOverrides??new(),moves);
  }
  scalePicker.SelectionChanged+=(_,_)=>
  {
   if(syncingScale||calculated==null||selectedView is not long id||
    scalePicker.SelectedItem is not string label||!int.TryParse(label[2..],out int scale))return;
   if(calculated.Pages.SelectMany(p=>p.Drawings).Any(d=>d.ViewId==id&&d.Scale==scale))return;
   var scales=new Dictionary<long,int>(calculated.ScaleOverrides??new()){[id]=scale};
   Recalculate(scales,calculated.Moves??[]);
  };
  undo.Click+=(_,_)=>{if(history.TryPop(out var previous)){calculated=previous;Render();}};
  preview.Click+=(_,_)=>
  {
   Invalidate();
   try
   {
    var selected=checks.Where(c=>c.Check.IsChecked==true).Select(c=>c.Item.ViewId).ToList();
    calculated=calculate(profile.SelectedIndex==1?"r20":"preferencial",selected,adaptive.IsChecked==true);
    Render();
   }
   catch(Exception ex){status.Text=ex.Message;}
  };
  apply.Click+=(_,_)=>{if(calculated==null)return;Result=calculated;DialogResult=true;};Invalidate();
 }
 public static FrameworkElement DrawPage(PlannerPage page)=>DrawPageCore(page,null,null,null);

 const string DrawingDrag="Vebram.SheetPlanner.Drawing";
 static FrameworkElement DrawPageCore(PlannerPage page,Action<long>? select,Action<PlannerMove>? move,long? selected)
 {
  var panel=new StackPanel{Width=430,Margin=new Thickness(8)};panel.Children.Add(new TextBlock{Text=page.Number+(page.IsNew?" · NOVA":" · existente"),FontSize=18,FontWeight=FontWeights.SemiBold});panel.Children.Add(new TextBlock{Text=page.Name,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,3,0,8)});
  var paper=page.SheetBounds??page.Area;double scale=400/(paper.Right-paper.Left),height=(paper.Top-paper.Bottom)*scale;var canvas=new Canvas{Width=400,Height=height,Background=Brushes.White,ClipToBounds=true};panel.Children.Add(new Border{Child=canvas,BorderBrush=Brushes.SlateGray,BorderThickness=new Thickness(1)});
  var buttons=new Dictionary<long,Button>();panel.Tag=buttons;
  string target=page.LayoutKey??page.Number;
  bool DragId(DragEventArgs e,out long id)
  {
   id=0;
   return e.Data.GetDataPresent(DrawingDrag,false)&&e.Data.GetData(DrawingDrag,false) is long value&&(id=value)>0;
  }
  if(move!=null)
  {
   canvas.AllowDrop=true;
   canvas.DragOver+=(sender,e)=>
   {
    var pt=e.GetPosition(canvas);
    double x=paper.Left+pt.X/scale,y=paper.Top-pt.Y/scale;
    e.Effects=DragId(e,out _)&&x>=page.Area.Left&&x<=page.Area.Right&&y>=page.Area.Bottom&&y<=page.Area.Top?DragDropEffects.Move:DragDropEffects.None;
    e.Handled=true;
   };
   canvas.Drop+=(_,e)=>
   {
    if(!DragId(e,out var id))return;
    var pt=e.GetPosition(canvas);e.Handled=true;
    move(new(id,target,paper.Left+pt.X/scale,paper.Top-pt.Y/scale));
   };
  }
  void Box(LegalPaperRect r,Brush fill,string label,PlannerDrawing? drawing=null)
  {
   var border=new Border{Width=Math.Max(1,(r.Right-r.Left)*scale),Height=Math.Max(1,(r.Top-r.Bottom)*scale),Background=fill,BorderBrush=Brushes.Teal,BorderThickness=new Thickness(.5),ToolTip=label,Child=new TextBlock{Text=label,TextWrapping=TextWrapping.Wrap,FontSize=10,Margin=new Thickness(3),VerticalAlignment=VerticalAlignment.Center,TextAlignment=TextAlignment.Center}};   FrameworkElement visual=border;
   if(drawing!=null&&select!=null)
   {
    var labelText=border.Child;
    border.Child=null;
    var button=new Button{Width=border.Width,Height=border.Height,Background=drawing.ViewId==selected?Brushes.LightBlue:fill,
     BorderBrush=Brushes.Teal,BorderThickness=new Thickness(.5),Padding=new Thickness(0),Cursor=Cursors.Hand,
     ToolTip=label+(move==null?"":"\nClique para selecionar. Arraste sobre outra vista para trocar."),
     Content=labelText,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Center};
    button.Click+=(_,_)=>select(drawing.ViewId);
    buttons.Add(drawing.ViewId,button);
    if(move!=null)
    {
     Point origin=new();
     bool dragging=false;
     button.PreviewMouseLeftButtonDown+=(_,e)=>{origin=e.GetPosition(canvas);dragging=false;select(drawing.ViewId);};
     button.PreviewMouseMove+=(_,e)=>
     {
      if(dragging||e.LeftButton!=MouseButtonState.Pressed||!button.IsMouseCaptured)return;
      var pt=e.GetPosition(canvas);
      if(Math.Abs(pt.X-origin.X)<SystemParameters.MinimumHorizontalDragDistance&&
       Math.Abs(pt.Y-origin.Y)<SystemParameters.MinimumVerticalDragDistance)return;
      dragging=true;button.ReleaseMouseCapture();
      DragDrop.DoDragDrop(button,new DataObject(DrawingDrag,drawing.ViewId),DragDropEffects.Move);
     };
     button.AllowDrop=true;
     button.DragOver+=(_,e)=>{if(DragId(e,out var id)){e.Effects=id==drawing.ViewId?DragDropEffects.None:DragDropEffects.Move;e.Handled=true;}};
     button.Drop+=(_,e)=>{if(DragId(e,out var id)){e.Handled=true;if(id!=drawing.ViewId)move(new(id,target,0,0,drawing.ViewId));}};
    }
    visual=button;
   }
   Canvas.SetLeft(visual,(r.Left-paper.Left)*scale);Canvas.SetTop(visual,(paper.Top-r.Top)*scale);canvas.Children.Add(visual);
  }
  foreach(var zone in page.Zones??[])Box(zone.Rect,zone.Writable?Brushes.WhiteSmoke:Brushes.Gainsboro,zone.Key=="desenhos"||page.Drawings.Any(d=>d.Zone==zone.Key)?"":zone.Key=="notas"&&page.Notes?.Count>0?"NOTAS\nAUTOMÁTICAS":zone.Label);
  foreach(var r in page.Obstacles)Box(r,Brushes.Gainsboro,"");foreach(var d in page.Drawings)Box(d.Rect,Brushes.Azure,d.Label+(d.Scale>0?"\n1:"+d.Scale:""),d);
  panel.Children.Add(new TextBlock{Text=$"{(paper.Right-paper.Left)*304.8:0} × {(paper.Top-paper.Bottom)*304.8:0} mm · {page.Drawings.Count(d=>d.Zone!="desenhos")} quadro(s) lateral(is)",FontSize=11,Foreground=Brushes.DimGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,4)});return panel;
 }
}
