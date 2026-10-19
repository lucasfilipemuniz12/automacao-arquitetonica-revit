using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Automation;
using UIApplication=Autodesk.Revit.UI.UIApplication;
using Document=Autodesk.Revit.DB.Document;
namespace Vebram.Revit;

internal static class RegistrationUi
{
 internal static readonly Brush Ink=new SolidColorBrush(Color.FromRgb(30,55,69)),Teal=new SolidColorBrush(Color.FromRgb(0,119,119)),Pale=new SolidColorBrush(Color.FromRgb(233,245,242));
 internal static readonly string[] Uses=["Não definido","Residencial unifamiliar","Residencial multifamiliar","Comercial / serviços","Uso misto","Outro — descrever nas observações"];
 internal static readonly string[] Interventions=["Não definida","Construção nova","Ampliação","Reforma","Regularização","Outro — descrever nas observações"];
 internal static void Initialize(Window window,UIApplication app,string title,double width=1180)
 {window.Title="VEBRAM — "+title;window.Width=Math.Min(width,SystemParameters.WorkArea.Width-35);window.Height=Math.Min(850,SystemParameters.WorkArea.Height-35);window.MinWidth=950;window.MinHeight=620;window.Background=new SolidColorBrush(Color.FromRgb(246,248,250));window.WindowStartupLocation=WindowStartupLocation.CenterOwner;new WindowInteropHelper(window).Owner=app.MainWindowHandle;}
 internal static StackPanel Header(string step,string title,string subtitle)
 {var p=new StackPanel{Margin=new Thickness(24,18,24,12)};p.Children.Add(new TextBlock{Text=step+"  ·  PROTÓTIPO 0.5.26",Foreground=Teal,FontSize=12,FontWeight=FontWeights.Bold});p.Children.Add(new TextBlock{Text=title,FontSize=27,Foreground=Ink,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,5,0,5)});p.Children.Add(new TextBlock{Text=subtitle,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray});return p;}
 internal static TextBlock Label(string title)=>new(){Text=title,Foreground=Ink,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,9,0,5),TextWrapping=TextWrapping.Wrap};
 internal static TextBox Field(Panel panel,string label,string value="",bool multiline=false)
 {panel.Children.Add(Label(label));var box=new TextBox{Text=value,MinHeight=34,Padding=new Thickness(8,6,8,6),AcceptsReturn=multiline,TextWrapping=multiline?TextWrapping.Wrap:TextWrapping.NoWrap};if(multiline)box.MinHeight=65;AutomationProperties.SetName(box,label);panel.Children.Add(box);return box;}
 internal static ComboBox Choice(Panel panel,string label,IEnumerable<string> values,string selected)
 {panel.Children.Add(Label(label));var box=new ComboBox{ItemsSource=values,SelectedItem=selected,MinHeight=34};AutomationProperties.SetName(box,label);if(box.SelectedItem==null)box.SelectedIndex=0;panel.Children.Add(box);return box;}
 internal static void Row(Panel panel,params UIElement[] controls)
 {var g=new Grid();for(int i=0;i<controls.Length;i++){g.ColumnDefinitions.Add(new());Grid.SetColumn(controls[i],i);if(controls[i] is FrameworkElement f)f.Margin=new Thickness(0,0,i==controls.Length-1?0:14,0);g.Children.Add(controls[i]);}panel.Children.Add(g);}
 internal static Button Button(string text,bool primary=false)=>new(){Content=text,Padding=new Thickness(16,10,16,10),Margin=new Thickness(6,0,0,0),Background=primary?Teal:Brushes.White,Foreground=primary?Brushes.White:Ink,BorderBrush=Teal};
 internal static Border Guide(bool municipal)
 {
  var p=new StackPanel{Margin=new Thickness(18)};
  p.Children.Add(new TextBlock{Text=municipal?"Uma cidade, diferentes condições":"Cadastre uma vez. Reutilize no projeto.",FontSize=19,Foreground=Ink,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap});
  var canvas=new Canvas{Width=290,Height=230,Margin=new Thickness(0,20,0,15),HorizontalAlignment=HorizontalAlignment.Left};p.Children.Add(canvas);
  void Line(double x,double y,double x2,double y2)=>canvas.Children.Add(new Line{X1=x,Y1=y,X2=x2,Y2=y2,Stroke=Teal,StrokeThickness=2});
  void Rect(double x,double y,double w,double h,Brush fill){var r=new Rectangle{Width=w,Height=h,Fill=fill,Stroke=Teal,StrokeThickness=2};Canvas.SetLeft(r,x);Canvas.SetTop(r,y);canvas.Children.Add(r);}
  void Text(double x,double y,string value,double size=12){var t=new TextBlock{Text=value,Foreground=Ink,FontSize=size,TextWrapping=TextWrapping.Wrap,Width=270};Canvas.SetLeft(t,x);Canvas.SetTop(t,y);canvas.Children.Add(t);}
  if(!municipal)
  {Rect(25,35,230,135,Brushes.White);Rect(88,72,110,70,Pale);Line(140,72,140,142);Line(88,110,140,110);Rect(25,177,230,25,new SolidColorBrush(Color.FromRgb(218,225,233)));Text(87,87,"CASA",14);Text(35,42,"2  LOTE / ZONA");Text(34,182,"3  ENDEREÇO / ACESSO");Text(26,9,"1  MUNICÍPIO + UF");Text(26,212,"Ilustração de orientação; sem escala.",11);}
  else
  {Rect(28,35,230,45,Brushes.White);Text(42,48,"MUNICÍPIO + UF",15);Rect(28,103,105,62,Pale);Rect(153,103,105,62,Pale);Text(42,113,"ZONA A\nResidencial");Text(164,113,"ZONA B\nUso misto");Line(140,80,140,94);Line(81,94,204,94);Line(81,94,81,103);Line(204,94,204,103);Text(28,188,"Regras com fonte e condições\nde aplicação — não só o nome da cidade.");}
  p.Children.Add(Label(municipal?"O que fica salvo na biblioteca?":"O que o cadastro alimenta?"));
  p.Children.Add(new TextBlock{Text=municipal?"• Cidade, UF e nome do perfil\n• Zoneamento, uso e intervenção\n• Limites informados e sua fonte\n• Revisão do perfil para reutilização":"• Nome, cliente e endereço no carimbo\n• Lote na Implantação e na Situação\n• Perfil e limites no quadro urbanístico\n• Cópia do perfil escolhido para este projeto",TextWrapping=TextWrapping.Wrap,Foreground=Ink,LineHeight=24});
  p.Children.Add(new Border{Background=Brushes.White,Padding=new Thickness(12),Margin=new Thickness(0,18,0,0),Child=new TextBlock{Text=municipal?"Regras digitadas pelo usuário. Este protótipo não importa leis nem verifica conformidade automaticamente.":"O endereço não desenha nem posiciona o lote. Confira o contorno no modelo. Informações ausentes continuam como pendência.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DarkSlateGray}});
  return new Border{Background=Pale,CornerRadius=new CornerRadius(8),Child=p,Margin=new Thickness(18,0,0,0)};
 }
}

public sealed class ProjectRegistrationWindow:Window
{
 sealed record ProfileChoice(string Label,MunicipalProfile? Profile){public override string ToString()=>Label;}
 readonly Document doc;
 readonly Dictionary<string,TextBox> fields=[];
 readonly ComboBox profiles=new(){MinHeight=36};
 readonly ComboBox use,intervention;
 readonly TextBlock profileStatus=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray,Margin=new Thickness(0,7,0,0)};
 public bool PrepareAfterSave{get;private set;}
 bool loadingProfiles;
 public ProjectRegistrationWindow(UIApplication app)
 {
  doc=app.ActiveUIDocument.Document;var saved=ProjectRegistrationStore.ReadForEditing(doc,out var readProblem);
  RegistrationUi.Initialize(this,app,"Dados do projeto");var dock=new DockPanel();Content=dock;
  var header=RegistrationUi.Header("1 / 3","Dados do projeto","Identifique a casa, selecione um perfil municipal e confira o que será reutilizado.");DockPanel.SetDock(header,Dock.Top);dock.Children.Add(header);
  var footer=new DockPanel{Margin=new Thickness(24,15,24,20)};DockPanel.SetDock(footer,Dock.Bottom);dock.Children.Add(footer);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);
  var cancel=RegistrationUi.Button("Cancelar");cancel.Click+=(_,_)=>Close();actions.Children.Add(cancel);
  var save=RegistrationUi.Button("Salvar cadastro");save.Click+=(_,_)=>Save(false);actions.Children.Add(save);
  var go=RegistrationUi.Button("Salvar e preparar →",true);go.Click+=(_,_)=>Save(true);actions.Children.Add(go);
  var layout=new Grid{Margin=new Thickness(24,0,24,0)};layout.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});layout.ColumnDefinitions.Add(new(){Width=new GridLength(335)});dock.Children.Add(layout);
  var form=new StackPanel{Margin=new Thickness(0,0,8,0)};
  if(readProblem!=null)form.Children.Add(new TextBlock{Text=readProblem,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Firebrick,Margin=new Thickness(0,0,0,10)});layout.Children.Add(new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});var guide=RegistrationUi.Guide(false);Grid.SetColumn(guide,1);layout.Children.Add(guide);
  TextBox Field(Panel panel,string key,string title,string value,bool multi=false){var b=RegistrationUi.Field(panel,title,value,multi);fields[key]=b;return b;}
  var left=new StackPanel();var right=new StackPanel();Field(left,"name","Nome do projeto *",saved.Name);Field(right,"code","Código do projeto",saved.Code);RegistrationUi.Row(form,left,right);
  Field(form,"client","Cliente / proprietário",saved.Client);Field(form,"address","Endereço — rua, número e bairro",saved.Address);
  form.Children.Add(RegistrationUi.Label("Perfil municipal reutilizável"));form.Children.Add(profiles);form.Children.Add(profileStatus);
  var manage=RegistrationUi.Button("Abrir biblioteca de municípios →");manage.HorizontalAlignment=HorizontalAlignment.Left;manage.Margin=new Thickness(0,8,0,0);form.Children.Add(manage);
  left=new StackPanel();right=new StackPanel();Field(left,"city","Município *",saved.City);Field(right,"state","UF *",saved.State);RegistrationUi.Row(form,left,right);
  left=new StackPanel();right=new StackPanel();Field(left,"lot","Lote / quadra / inscrição cadastral",saved.Lot);Field(right,"zone","Zoneamento informado",saved.Zone);RegistrationUi.Row(form,left,right);
  left=new StackPanel();right=new StackPanel();use=RegistrationUi.Choice(left,"Uso predominante",RegistrationUi.Uses,saved.Use);intervention=RegistrationUi.Choice(right,"Tipo de intervenção",RegistrationUi.Interventions,saved.Intervention);RegistrationUi.Row(form,left,right);
  Field(form,"floors","Número de pavimentos — se conhecido",saved.Floors);Field(form,"notes","Observações / condições específicas do imóvel",saved.Notes,true);
  form.Children.Add(new TextBlock{Text="* Mínimo para salvar: nome, município e UF. Este cadastro não mede áreas, não verifica legislação e não modifica a geometria.",Foreground=Brushes.SlateGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
  LoadProfiles(saved.Profile);
  profiles.SelectionChanged+=(_,_)=>
  {
   if(loadingProfiles)return;
   var profile=(profiles.SelectedItem as ProfileChoice)?.Profile;
   if(profile!=null){fields["city"].Text=profile.City;fields["state"].Text=profile.State;fields["zone"].Text=profile.Zone;if(profile.Use!="Não definido")use.SelectedItem=profile.Use;if(profile.Intervention!="Não definida")intervention.SelectedItem=profile.Intervention;}
   Status();
  };
  manage.Click+=(_,_)=>{var chosen=(profiles.SelectedItem as ProfileChoice)?.Profile;var library=new MunicipalProfilesWindow(app){Owner=this};library.ShowDialog();LoadProfiles(chosen);};
 }
 void LoadProfiles(MunicipalProfile? snapshot)
 {
  loadingProfiles=true;
  try
  {
  var choices=new List<ProfileChoice>{new("Sem perfil — preencher os dados do projeto",null)};
  if(snapshot!=null)choices.Add(new("Salvo neste projeto: "+snapshot.Title+" · revisão "+snapshot.Revision,snapshot));
  choices.AddRange(MunicipalLibrary.Read().Where(p=>snapshot==null||p.Id!=snapshot.Id||p.Revision!=snapshot.Revision).Select(p=>new ProfileChoice(p.Title+" — "+p.City+"/"+p.State+" · revisão "+p.Revision,p)));
  profiles.ItemsSource=choices;profiles.SelectedItem=snapshot==null?choices[0]:choices[1];Status();
  }
  finally{loadingProfiles=false;}
 }
 void Status(){var p=(profiles.SelectedItem as ProfileChoice)?.Profile;profileStatus.Text=p==null?"Nenhuma regra municipal vinculada. Você pode cadastrar os dados e preparar vistas.":"Ao salvar, este projeto guarda uma cópia da revisão "+p.Revision+". Alterar a biblioteca não substitui essa cópia automaticamente.\nFonte: "+(string.IsNullOrWhiteSpace(p.Source)?"não informada — conferir antes de usar":p.Source);}
 void Save(bool prepare)
 {
  try
  {
   string V(string key)=>fields[key].Text.Trim();
   ProjectRegistrationStore.Save(doc,new(V("name"),V("code"),V("client"),V("address"),V("lot"),V("city"),V("state").ToUpperInvariant(),V("zone"),use.SelectedItem as string??"Não definido",intervention.SelectedItem as string??"Não definida",V("floors"),V("notes"),(profiles.SelectedItem as ProfileChoice)?.Profile));
   PrepareAfterSave=prepare;DialogResult=true;
  }
  catch(Exception ex){MessageBox.Show(this,ex.Message,"VEBRAM — confira o cadastro");}
 }
}

public sealed class MunicipalProfilesWindow:Window
{
 sealed record Item(MunicipalProfile Profile){public override string ToString()=>Profile.Title+"\n"+Profile.City+" / "+Profile.State+" · revisão "+Profile.Revision;}
 static readonly (string Key,string Label,bool Percent)[] Parameters=[("to","Ocupação máxima (%)",true),("ca","Aproveitamento máximo (CA)",false),("tp","Permeável mínimo (%)",true),("frente","Afastamento frontal (m)",false),("lateral","Afastamento lateral (m)",false),("fundos","Afastamento de fundos (m)",false),("altura","Altura máxima (m)",false)];
 readonly Dictionary<string,TextBox> fields=[];
 readonly ListBox list=new(){MinWidth=215,BorderThickness=new Thickness(0),Background=Brushes.White};
 readonly TextBlock status=new(){Foreground=Brushes.SlateGray,TextWrapping=TextWrapping.Wrap};
 readonly ComboBox use,intervention;
 MunicipalProfile? editing;
 public MunicipalProfilesWindow(UIApplication app)
 {
  RegistrationUi.Initialize(this,app,"Municípios e regras",1280);var dock=new DockPanel();Content=dock;
  var header=RegistrationUi.Header("2 / 3","Municípios e regras","Sua biblioteca pessoal de perfis. A mesma cidade pode ter vários zoneamentos e condições de aplicação.");DockPanel.SetDock(header,Dock.Top);dock.Children.Add(header);
  var footer=new DockPanel{Margin=new Thickness(24,14,24,18)};DockPanel.SetDock(footer,Dock.Bottom);dock.Children.Add(footer);
  var actions=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);
  var close=RegistrationUi.Button("Fechar");close.Click+=(_,_)=>Close();actions.Children.Add(close);var save=RegistrationUi.Button("Salvar perfil na biblioteca",true);save.Click+=(_,_)=>Save();actions.Children.Add(save);footer.Children.Add(status);
  var layout=new Grid{Margin=new Thickness(24,0,24,0)};layout.ColumnDefinitions.Add(new(){Width=new GridLength(225)});layout.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});layout.ColumnDefinitions.Add(new(){Width=new GridLength(310)});dock.Children.Add(layout);
  var sidebar=new DockPanel{Margin=new Thickness(0,0,16,0)};layout.Children.Add(sidebar);var create=RegistrationUi.Button("+ Novo perfil");create.Margin=new Thickness(0,0,0,12);DockPanel.SetDock(create,Dock.Top);sidebar.Children.Add(create);sidebar.Children.Add(list);
  ScrollViewer.SetHorizontalScrollBarVisibility(list,ScrollBarVisibility.Disabled);
  list.ItemContainerStyle=new Style(typeof(ListBoxItem));list.ItemContainerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch));
  var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding());text.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);text.SetValue(FrameworkElement.MarginProperty,new Thickness(5,7,5,7));list.ItemTemplate=new DataTemplate{VisualTree=text};
  var form=new StackPanel{Margin=new Thickness(0,0,5,0)};var scroll=new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(scroll,1);layout.Children.Add(scroll);var guide=RegistrationUi.Guide(true);Grid.SetColumn(guide,2);layout.Children.Add(guide);
  void Field(Panel panel,string key,string label,bool multi=false)=>fields[key]=RegistrationUi.Field(panel,label,"",multi);
  Field(form,"title","Nome do perfil * — ex.: Residencial / Zona X");
  var left=new StackPanel();var right=new StackPanel();Field(left,"city","Município *");Field(right,"state","UF *");RegistrationUi.Row(form,left,right);
  Field(form,"zone","Zoneamento / setor / condição territorial");
  left=new StackPanel();right=new StackPanel();use=RegistrationUi.Choice(left,"Uso aplicável",RegistrationUi.Uses,"Não definido");intervention=RegistrationUi.Choice(right,"Intervenção aplicável",RegistrationUi.Interventions,"Não definida");RegistrationUi.Row(form,left,right);
  form.Children.Add(RegistrationUi.Label("Parâmetros informados — deixe vazio se não souber"));
  for(int i=0;i<Parameters.Length;i+=2)
  {left=new StackPanel();right=new StackPanel();Field(left,Parameters[i].Key,Parameters[i].Label);if(i+1<Parameters.Length)Field(right,Parameters[i+1].Key,Parameters[i+1].Label);RegistrationUi.Row(form,left,right);}
  Field(form,"conditions","Condições e exceções de aplicação",true);Field(form,"source","Fonte — documento, artigo e/ou endereço oficial",true);Field(form,"date","Data da conferência / referência (texto)");
  form.Children.Add(new TextBlock{Text="Descrição e valores manuais. Nenhum parâmetro é deduzido da cidade, e nenhum perfil equivale a aprovação da prefeitura.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray,Margin=new Thickness(0,12,0,12)});
  list.SelectionChanged+=(_,_)=>{if(list.SelectedItem is Item item)Load(item.Profile);};create.Click+=(_,_)=>{list.SelectedItem=null;Load(null);};Reload(null);
 }
 void Reload(string? id)
 {list.ItemsSource=MunicipalLibrary.Read().Select(p=>new Item(p)).ToList();list.SelectedItem=list.Items.Cast<Item>().FirstOrDefault(i=>i.Profile.Id==id);if(list.SelectedItem==null)Load(null);}
 void Load(MunicipalProfile? p)
 {
  editing=p;foreach(var b in fields.Values)b.Text="";
  if(p!=null){fields["title"].Text=p.Title;fields["city"].Text=p.City;fields["state"].Text=p.State;fields["zone"].Text=p.Zone;fields["conditions"].Text=p.Conditions;fields["source"].Text=p.Source;fields["date"].Text=p.ReferenceDate;foreach(var value in p.Rules)if(fields.TryGetValue(value.Key,out var b))b.Text=value.Value;}
  use.SelectedItem=p?.Use??"Não definido";intervention.SelectedItem=p?.Intervention??"Não definida";
  status.Text=p==null?"Novo perfil · ainda não salvo\nReutilizável em outros RVTs após salvar.":"Revisão "+p.Revision+" · regras informadas pelo usuário\nProjetos existentes mantêm a cópia que escolheram.";
 }
 void Save()
 {
  try
  {
   string V(string key)=>fields[key].Text.Trim();var rules=new Dictionary<string,string>();
   foreach(var p in Parameters){string raw=V(p.Key);if(raw.Length==0)continue;if(!double.TryParse(raw.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double number)||!double.IsFinite(number)||number<0||(p.Percent&&number>100))throw new InvalidOperationException("Confira "+p.Label+". Use um número não negativo"+(p.Percent?" entre 0 e 100.":"."));rules[p.Key]=raw;}
   var draft=new MunicipalProfile(editing?.Id??Guid.NewGuid().ToString("N"),V("title"),V("city"),V("state").ToUpperInvariant(),V("zone"),use.SelectedItem as string??"Não definido",intervention.SelectedItem as string??"Não definida",rules,V("conditions"),V("source"),V("date"),editing?.Revision??0);
   var saved=MunicipalLibrary.Save(draft);Reload(saved.Id);status.Text="Perfil salvo · revisão "+saved.Revision+"\nDisponível no cadastro de qualquer projeto deste computador.";
  }
  catch(Exception ex){MessageBox.Show(this,ex.Message,"VEBRAM — confira o perfil");}
 }
}
