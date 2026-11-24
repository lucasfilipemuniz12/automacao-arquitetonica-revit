using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using WpfVisibility=System.Windows.Visibility;

namespace Vebram.Revit;

public sealed class SheetPlannerWindow:Window
{
 public SheetPlan? Result{get;private set;}
 public ModuleResult? Applied{get;private set;}
 readonly WrapPanel previews=new(){Orientation=Orientation.Horizontal};
 readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};
 public SheetPlannerWindow(IReadOnlyList<PlannerItem> items,Func<string,IEnumerable<long>,bool,SheetPlan> calculate,
  IReadOnlyList<string>? unprepared=null,Func<SheetPlan,ModuleResult>? applyPlan=null,
  Func<List<PlannerFormat>>? getFormats=null,Func<SheetPlan,Dictionary<long,ImageSource>>? getImages=null)
 {
  Title="VEBRAM — Organizar folhas";Width=Math.Min(1260,SystemParameters.WorkArea.Width-40);Height=Math.Min(895,SystemParameters.WorkArea.Height-40);WindowStartupLocation=WindowStartupLocation.CenterOwner;
  Background=new SolidColorBrush(Color.FromRgb(246,248,250));
  var root=new DockPanel{Margin=new Thickness(20)};Content=root;
  var heading=new StackPanel();DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
  heading.Children.Add(new TextBlock{Text="Organize o conjunto antes de aplicar",FontSize=24,FontWeight=FontWeights.SemiBold});
  heading.Children.Add(new TextBlock{Text="Clique numa vista para mudar a escala ou no número da folha para mudar o formato. Arraste para posicionar. Ao soltar sobre outra vista, as duas trocam de lugar.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,9)});
  var profile=new ComboBox{ItemsSource=new[]{"Preferencial — Implantação + Situação; Cortes A/B; quatro Fachadas","Referência R20 — Situação; Plantas; Implantação/Cobertura/Cortes; Fachadas"},SelectedIndex=0,MinHeight=30};heading.Children.Add(profile);
  var adaptive=new CheckBox{Content="Combinar assuntos compatíveis para aproveitar espaço",IsChecked=true,Margin=new Thickness(0,8,0,7)};heading.Children.Add(adaptive);
  var editing=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,8)};
  var selectedLabel=new TextBlock{Text="Selecione uma folha ou vista",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,10,0)};
  var scalePicker=new ComboBox{MinWidth=95,MinHeight=28,Visibility=WpfVisibility.Collapsed};
  var formatPicker=new ComboBox{MinWidth=190,MaxWidth=480,MinHeight=28,Visibility=WpfVisibility.Collapsed};
  var undo=new Button{Content="Desfazer",Margin=new Thickness(10,0,0,0),Padding=new Thickness(8,3,8,3),IsEnabled=false};
  editing.Children.Add(selectedLabel);editing.Children.Add(scalePicker);editing.Children.Add(formatPicker);editing.Children.Add(undo);heading.Children.Add(editing);
  var footer=new DockPanel{Margin=new Thickness(0,6,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
  var actions=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);footer.Children.Add(status);
  var preview=new Button{Content="Calcular prévia",Padding=new Thickness(12,8,12,8),Margin=new Thickness(4)};
  var apply=new Button{Content="Aplicar distribuição",Padding=new Thickness(12,8,12,8),Margin=new Thickness(4),IsEnabled=false};
  var cancel=new Button{Content="Cancelar",Padding=new Thickness(12,8,12,8),Margin=new Thickness(4),IsCancel=true};
  actions.Children.Add(preview);actions.Children.Add(apply);actions.Children.Add(cancel);
  var body=new Grid();body.ColumnDefinitions.Add(new(){Width=new GridLength(182)});body.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});root.Children.Add(body);
  var choices=new StackPanel{Margin=new Thickness(0,8,12,0)};body.Children.Add(new ScrollViewer{Content=choices,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  choices.Children.Add(new TextBlock{Text="ENTREGÁVEIS PREPARADOS",FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,12)});
  var checks=new List<(PlannerItem Item,CheckBox Check)>();
  SheetPlan? draft=null, original=null;
  string? selectedPage=null;
  long? selectedView=null;
  bool syncing=false;
  List<PlannerFormat>? formats=null;
  var images=new Dictionary<long,ImageSource>();
  var history=new Stack<SheetPlan>();
  var scroll=new ScrollViewer{Content=previews,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
  Grid.SetColumn(scroll,1);body.Children.Add(scroll);

  void Invalidate()
  {
   original=null;draft=null;selectedPage=null;selectedView=null;history.Clear();
   apply.IsEnabled=false;undo.IsEnabled=false;previews.Children.Clear();
   status.Text="Calcule a prévia para começar.";SyncSelection();
  }
  foreach(var item in items)
  {
   var check=new CheckBox{Content=new TextBlock{Text=item.Label,TextWrapping=TextWrapping.Wrap},IsChecked=true,Margin=new Thickness(0,4,0,4)};
   checks.Add((item,check));choices.Children.Add(check);check.Checked+=(_,_)=>Invalidate();check.Unchecked+=(_,_)=>Invalidate();
  }
  if(unprepared?.Count>0)
  {
   choices.Children.Add(new TextBlock{Text="AINDA NÃO PREPARADOS",FontWeight=FontWeights.SemiBold,Foreground=Brushes.DarkOrange,Margin=new Thickness(0,16,0,6)});
   foreach(var text in unprepared)choices.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DimGray});
  }
  profile.SelectionChanged+=(_,_)=>Invalidate();adaptive.Checked+=(_,_)=>Invalidate();adaptive.Unchecked+=(_,_)=>Invalidate();
  scroll.AddHandler(DragDrop.PreviewDragOverEvent,new DragEventHandler((_,e)=>
  {
   if(!e.Data.GetDataPresent(typeof(DragDrawing)))return;
   double y=e.GetPosition(scroll).Y;
   if(y<24)scroll.LineUp();
   else if(y>scroll.ActualHeight-24)scroll.LineDown();
  }),true);

  void SyncSelection()
  {
   syncing=true;
   try
   {
    var drawing=draft?.Pages.SelectMany(p=>p.Drawings).FirstOrDefault(d=>d.ViewId==selectedView);
    var page=draft?.Pages.FirstOrDefault(p=>p.Number==selectedPage);
    selectedLabel.Text=drawing?.Label??page?.Number??"Selecione uma folha ou vista";
    scalePicker.Visibility=drawing is {Scale:>0}&&applyPlan!=null?WpfVisibility.Visible:WpfVisibility.Collapsed;
    formatPicker.Visibility=page!=null&&getFormats!=null?WpfVisibility.Visible:WpfVisibility.Collapsed;
    int[] options=[20,25,50,75,100,125,150,200,250,500,1000];
    scalePicker.ItemsSource=drawing is {Scale:>0}?options.Append(drawing.Scale).Distinct().Order().Select(n=>"1:"+n).ToArray():null;
    scalePicker.SelectedItem=drawing is {Scale:>0}?"1:"+drawing.Scale:null;
    formatPicker.ItemsSource=formats?.Select(f=>new ComboBoxItem{Content=f.Label,Tag=f}).ToList();
    formatPicker.IsEnabled=formats?.Count>0;
    if(page!=null&&formats?.Count==0)selectedLabel.Text=page.Number+" · nenhum formato A0 a A4 carregado";
    formatPicker.SelectedItem=formatPicker.Items.OfType<ComboBoxItem>().FirstOrDefault(i=>(i.Tag as PlannerFormat)?.SymbolId==page?.FormatSymbolId);
    formatPicker.ToolTip="A lista mostra os carimbos A0 a A4 carregados no projeto.";
   }
   finally{syncing=false;}
  }
  void SelectDrawing(long id)
  {
   selectedView=id;selectedPage=null;SyncSelection();
   foreach(var panel in previews.Children.OfType<FrameworkElement>())
    if(panel.Tag is Dictionary<long,Border> boxes)
     foreach(var entry in boxes)entry.Value.Background=entry.Key==id?Brushes.LightBlue:Brushes.White;
  }
  void Render()
  {
   if(draft==null)return;
   var offset=scroll.VerticalOffset;
   previews.Children.Clear();
   int conflicts=0;
   foreach(var page in draft.Pages)
   {
    var invalid=SheetDraft.Conflicts(page);
    conflicts+=invalid.Count;
    previews.Children.Add(DrawPageCore(page,SelectDrawing,applyPlan==null?null:Drop,
     number=>{selectedPage=number;selectedView=null;SyncSelection();},selectedView,invalid,images));
   }
   scroll.ScrollToVerticalOffset(offset);
   status.Foreground=conflicts>0?Brushes.Firebrick:Brushes.DimGray;
   status.Text=conflicts>0?$"{conflicts} vista(s) em conflito na prévia. Ajuste as caixas em vermelho.":
    draft.ManualLayout?"Prévia editada. O Revit confere as dimensões exatas ao aplicar.":$"{draft.Pages.Count} folha(s) na prévia.";
   status.ToolTip=string.Join("\n",draft.Warnings);
   apply.IsEnabled=true;undo.IsEnabled=history.Count>0;SyncSelection();
  }
  void Update(List<PlannerPage> pages,Dictionary<long,int>? scales=null)
  {
   if(draft==null)return;
   history.Push(draft);
   draft=draft with{Pages=pages,ScaleOverrides=scales??draft.ScaleOverrides,ManualLayout=true,Moves=null};
   Render();
  }
  void Drop(DragDrawing payload,string destination,LegalPaperRect rect,long? otherId)
  {
   if(draft==null)return;
   var source=draft.Pages.Single(p=>p.Drawings.Any(d=>d.ViewId==payload.Drawing.ViewId));
   var target=draft.Pages.Single(p=>p.Number==destination);
   var drawing=payload.Drawing;
   var other=target.Drawings.Find(d=>d.ViewId==otherId);
   if(other!=null)rect=SheetDraft.At(drawing.Rect,other.Rect.Left,other.Rect.Top);
   bool CanUseSide(PlannerDrawing d)=>d.Scale==0;
   var moved=drawing with{Rect=rect,Zone=SheetDraft.ZoneAt(target,rect,CanUseSide(drawing))};
   var pages=draft.Pages.Select(p=>
   {
    if(p.Number!=source.Number&&p.Number!=target.Number)return p;
    var list=p.Drawings.Where(d=>d.ViewId!=drawing.ViewId&&d.ViewId!=otherId).ToList();
    if(p.Number==target.Number)list.Add(moved);
    if(other!=null&&p.Number==source.Number)
    {
     var replacement=SheetDraft.At(other.Rect,drawing.Rect.Left,drawing.Rect.Top);
     list.Add(other with{Rect=replacement,Zone=SheetDraft.ZoneAt(source,replacement,CanUseSide(other))});
    }
    return p with{Drawings=list};
   }).ToList();
   Update(pages);
  }
  scalePicker.SelectionChanged+=(_,_)=>
  {
   if(syncing||draft==null||original==null||selectedView is not long id||
      scalePicker.SelectedItem is not string label||!int.TryParse(label[2..],out int scale))return;
   var initial=original.Pages.SelectMany(p=>p.Drawings).Single(d=>d.ViewId==id);
   var scales=new Dictionary<long,int>(draft.ScaleOverrides??new());
   if(initial.Scale==scale)scales.Remove(id);else scales[id]=scale;
   var pages=draft.Pages.Select(p=>p with{Drawings=p.Drawings.Select(d=>
   {
    if(d.ViewId!=id)return d;
    var size=SheetDraft.Scale(initial.Rect,initial.Scale,scale);
    return d with{Scale=scale,Rect=SheetDraft.At(size,d.Rect.Left,d.Rect.Top)};
   }).ToList()}).ToList();
   Update(pages,scales);
  };
  formatPicker.SelectionChanged+=(_,_)=>
  {
   if(syncing||draft==null||formatPicker.SelectedItem is not ComboBoxItem {Tag:PlannerFormat format})return;
   Update(draft.Pages.Select(p=>p.Number==selectedPage?SheetDraft.ChangeFormat(p,format):p).ToList());
  };
  undo.Click+=(_,_)=>{if(history.TryPop(out var previous)){draft=previous;Render();}};
  preview.Click+=(_,_)=>
  {
   Invalidate();
   try
   {
    var selected=checks.Where(c=>c.Check.IsChecked==true).Select(c=>c.Item.ViewId).ToList();
    original=calculate(profile.SelectedIndex==1?"r20":"preferencial",selected,adaptive.IsChecked==true);
    formats??=getFormats?.Invoke();
    if(getImages!=null&&original.Pages.SelectMany(p=>p.Drawings).Any(d=>d.Scale>0&&!images.ContainsKey(d.ViewId)))
     foreach(var image in getImages(original))images[image.Key]=image.Value;
    draft=original;Render();
   }
   catch(Exception ex){status.Text=ex.Message;}
  };
  apply.Click+=(_,_)=>
  {
   if(draft==null)return;
   try{Applied=applyPlan?.Invoke(draft);Result=draft;DialogResult=true;}
   catch(Exception ex){status.Foreground=Brushes.Firebrick;status.Text=ex.Message;}
  };
  Invalidate();
 }

 public static FrameworkElement DrawPage(PlannerPage page)=>DrawPageCore(page,null,null,null,null,[],new());
 sealed record DragDrawing(PlannerDrawing Drawing,double OffsetX,double OffsetY);

 static FrameworkElement DrawPageCore(PlannerPage page,Action<long>? select,
  Action<DragDrawing,string,LegalPaperRect,long?>? drop,Action<string>? selectPage,long? selected,
  HashSet<long> invalid,Dictionary<long,ImageSource> images)
 {
  var paper=SheetDraft.Bounds(page);
  const double pixelsPerFoot=.46*304.8;
  double width=(paper.Right-paper.Left)*pixelsPerFoot,height=(paper.Top-paper.Bottom)*pixelsPerFoot;
  var panel=new StackPanel{Width=width+2,Margin=new Thickness(9)};
  var header=new Button{Content=page.Number+(page.IsNew?" · nova":"")+"   ▾",FontSize=16,FontWeight=FontWeights.SemiBold,
   HorizontalContentAlignment=HorizontalAlignment.Left,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0,2,0,2),
   ToolTip="Escolher formato desta folha"};
  header.Click+=(_,_)=>selectPage?.Invoke(page.Number);
  panel.Children.Add(header);
  panel.Children.Add(new TextBlock{Text=page.Name,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,3,0,7)});
  var canvas=new Canvas{Width=width,Height=height,Background=Brushes.White,ClipToBounds=true};
  panel.Children.Add(new Border{Child=canvas,BorderBrush=Brushes.SlateGray,BorderThickness=new Thickness(1)});
  var boxes=new Dictionary<long,Border>();panel.Tag=boxes;
  var ghost=new Border{IsHitTestVisible=false,Visibility=WpfVisibility.Collapsed,Opacity=.78,BorderThickness=new Thickness(2)};
  Panel.SetZIndex(ghost,100);
  void Position(FrameworkElement element,LegalPaperRect rect)
  {
   element.Width=Math.Max(1,(rect.Right-rect.Left)*pixelsPerFoot);element.Height=Math.Max(1,(rect.Top-rect.Bottom)*pixelsPerFoot);
   Canvas.SetLeft(element,(rect.Left-paper.Left)*pixelsPerFoot);Canvas.SetTop(element,(paper.Top-rect.Top)*pixelsPerFoot);
  }
  FrameworkElement Content(string label,long? id)
  {
   if(id.HasValue&&images.TryGetValue(id.Value,out var image))
   {
    var layout=new DockPanel{Margin=new Thickness(2)};
    var caption=new TextBlock{Text=label,FontSize=9,TextAlignment=TextAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis};
    DockPanel.SetDock(caption,Dock.Bottom);layout.Children.Add(caption);
    layout.Children.Add(new Image{Source=image,Stretch=Stretch.Uniform});return layout;
   }
   return new TextBlock{Text=label,FontSize=10,TextWrapping=TextWrapping.Wrap,TextAlignment=TextAlignment.Center,
    VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(2)};
  }
  string Caption(PlannerDrawing d)=>d.Scale>0?d.Label+" · 1:"+d.Scale:d.Label;
  void AddBox(LegalPaperRect rect,string label,Brush fill,bool obstacle=false)
  {
   var box=new Border{Background=fill,BorderBrush=Brushes.LightSlateGray,BorderThickness=new Thickness(.5),
    IsHitTestVisible=false,Child=Content(label,null),Opacity=obstacle ? .8 : 1};
   Position(box,rect);canvas.Children.Add(box);
  }
  foreach(var zone in page.Zones??[])
   AddBox(zone.Rect,zone.Key=="desenhos"?"":zone.Label,zone.Writable?Brushes.WhiteSmoke:Brushes.Gainsboro);
  foreach(var obstacle in page.Obstacles)AddBox(obstacle,"",Brushes.LightGray,true);
  foreach(var drawing in page.Drawings)
  {
   var label=Caption(drawing);
   var box=new Border{Background=drawing.ViewId==selected?Brushes.LightBlue:Brushes.White,
    BorderBrush=invalid.Contains(drawing.ViewId)?Brushes.IndianRed:Brushes.Teal,
    BorderThickness=new Thickness(invalid.Contains(drawing.ViewId)?2:1),Child=Content(label,drawing.ViewId),ClipToBounds=true,
    Cursor=drop!=null?Cursors.SizeAll:Cursors.Arrow,ToolTip=label};
   Position(box,drawing.Rect);canvas.Children.Add(box);boxes[drawing.ViewId]=box;
   Point origin=new();
   box.MouseLeftButtonDown+=(_,e)=>{select?.Invoke(drawing.ViewId);origin=e.GetPosition(box);box.CaptureMouse();e.Handled=true;};
   box.MouseLeftButtonUp+=(_,_)=>box.ReleaseMouseCapture();
   box.MouseMove+=(_,e)=>
   {
    if(drop==null||!box.IsMouseCaptured||e.LeftButton!=MouseButtonState.Pressed)return;
    var pt=e.GetPosition(box);
    if(Math.Abs(pt.X-origin.X)<SystemParameters.MinimumHorizontalDragDistance&&Math.Abs(pt.Y-origin.Y)<SystemParameters.MinimumVerticalDragDistance)return;
    box.ReleaseMouseCapture();
    DragDrop.DoDragDrop(box,new DragDrawing(drawing,origin.X/pixelsPerFoot,origin.Y/pixelsPerFoot),DragDropEffects.Move);
    ghost.Visibility=WpfVisibility.Collapsed;
   };
  }
  canvas.Children.Add(ghost);
  if(drop!=null)
  {
   canvas.AllowDrop=true;
   long? ghostView=null;
   (LegalPaperRect Rect,long? Other) Target(DragEventArgs e,DragDrawing data)
   {
    var pt=e.GetPosition(canvas);double x=paper.Left+pt.X/pixelsPerFoot,y=paper.Top-pt.Y/pixelsPerFoot;
    var other=page.Drawings.LastOrDefault(d=>d.ViewId!=data.Drawing.ViewId&&x>=d.Rect.Left&&x<=d.Rect.Right&&y>=d.Rect.Bottom&&y<=d.Rect.Top);
    return (other!=null?SheetDraft.At(data.Drawing.Rect,other.Rect.Left,other.Rect.Top):
     SheetDraft.At(data.Drawing.Rect,x-data.OffsetX,y+data.OffsetY),other?.ViewId);
   }
   canvas.DragOver+=(_,e)=>
   {
    if(e.Data.GetData(typeof(DragDrawing)) is not DragDrawing data)return;
    var (rect,other)=Target(e,data);
    var zoneKey=SheetDraft.ZoneAt(page,rect,data.Drawing.Scale==0);
    var zone=page.Zones?.FirstOrDefault(z=>z.Key==zoneKey)?.Rect??page.Area;
    bool fits=zone.Contains(rect)&&!page.Obstacles.Any(o=>o.Intersects(rect,0))&&
     !page.Drawings.Any(d=>d.ViewId!=data.Drawing.ViewId&&d.ViewId!=other&&d.Rect.Intersects(rect,0));
    if(ghostView!=data.Drawing.ViewId){ghost.Child=Content(Caption(data.Drawing),data.Drawing.ViewId);ghostView=data.Drawing.ViewId;}
    ghost.Background=fits?Brushes.AliceBlue:Brushes.MistyRose;
    ghost.BorderBrush=fits?Brushes.RoyalBlue:Brushes.IndianRed;
    Position(ghost,rect);ghost.Visibility=WpfVisibility.Visible;
    e.Effects=DragDropEffects.Move;e.Handled=true;
   };
   canvas.DragLeave+=(_,e)=>{var pt=e.GetPosition(canvas);if(pt.X<0||pt.Y<0||pt.X>=width||pt.Y>=height)ghost.Visibility=WpfVisibility.Collapsed;};
   canvas.Drop+=(_,e)=>
   {
    ghost.Visibility=WpfVisibility.Collapsed;
    if(e.Data.GetData(typeof(DragDrawing)) is not DragDrawing data)return;
    var (rect,other)=Target(e,data);
    e.Handled=true;drop(data,page.Number,rect,other);
   };
  }
  panel.Children.Add(new TextBlock{Text=$"{(paper.Right-paper.Left)*304.8:0} × {(paper.Top-paper.Bottom)*304.8:0} mm",
   FontSize=11,Foreground=Brushes.DimGray,Margin=new Thickness(0,4,0,0)});
  return panel;
 }
}
