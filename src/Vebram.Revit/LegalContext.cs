using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;

public sealed record ContextSelection(string Key,long SourceId,string Identification="",string LeftNeighbor="",string RightNeighbor="",string RearNeighbor="",double? ComputableArea=null,double? PermeableArea=null,double? MaximumOccupation=null,double? MaximumCoefficient=null,double? MinimumPermeability=null,bool Confirmed=false,bool ReplaceEdited=false,string RegistrationText="");
record ContextStamp(string Source,List<ElementStamp> Elements,ContextSelection Input);

/// <summary>Situation diagram and urbanistic worksheet using explicitly confirmed project data.</summary>
public static class LegalContext
{
 static readonly Guid SchemaId=new("63465842-6c64-46d4-b3aa-0a92c7e329e4");
 public static ContextSelection? Saved(Document doc,string key)
 {var view=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key==key));return view==null?null:Read(view)?.Input;}
 public static ModuleResult Apply(Document doc,ContextSelection input)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  if(input.Key is not ("situacao" or "urbanistico")||!input.Confirmed)throw new InvalidOperationException("Confira os dados do lote e confirme as informações.");
  var options=Implantation.Saved(doc)??throw new InvalidOperationException("Prepare a Implantação e confirme seu lote antes deste entregável.");
  if(options.SourceId!=input.SourceId)throw new InvalidOperationException("Use a mesma planta fonte da Implantação.");
  _=Implantation.Plan(doc,options); // Revalidate dimensions, source and floor against the current project.
  var values=new[]{input.ComputableArea,input.PermeableArea,input.MaximumOccupation,input.MaximumCoefficient,input.MinimumPermeability};
  if(values.Any(v=>v.HasValue&&(!double.IsFinite(v.Value)||v<0)))throw new InvalidOperationException("Áreas e índices devem ser números positivos ou zero.");
  if(new[]{input.Identification,input.LeftNeighbor,input.RightNeighbor,input.RearNeighbor}.Any(s=>s.Length>120))throw new InvalidOperationException("Use até 120 caracteres por identificação ou confrontante.");
  double area=options.LotWidth*options.LotDepth;
  if(input.PermeableArea>area||input.MaximumOccupation>100||input.MinimumPermeability>100)throw new InvalidOperationException("Confira área permeável e percentuais entre 0 e 100%.");
  var item=LegalCatalog.Items.First(i=>i.Key==input.Key);var view=LegalCatalog.Reference(LegalCatalog.Views(doc),item)??throw new InvalidOperationException("Entregável de referência ausente no V02.");
  var source=(ViewPlan)doc.GetElement(new ElementId(input.SourceId));var previous=Read(view);
  if(previous!=null&&previous.Source!=source.UniqueId)throw new InvalidOperationException("O entregável utiliza outra fonte.");
  if(previous!=null&&!input.ReplaceEdited&&previous.Elements.Any(s=>doc.GetElement(s.UniqueId) is not {} e||DrawingOwnership.Fingerprint(e)!=s.Fingerprint))throw new InvalidOperationException("Marcações editadas: confira e marque a opção de refazê-las.");
  using var tx=new Transaction(doc,"VEBRAM — "+item.Label);tx.Start();
  try
  {
   // The V02 reference may already occupy the title-block sidebar. Keep that content
   // and its placement intact; the complete generated table needs its own paper area.
   if(input.Key=="urbanistico"&&previous==null&&view.Name==item.ReferenceName)
   {
    var reference=view;view=ViewDrafting.Create(doc,reference.GetTypeId());view.Name=item.ReferenceName+" — VEBRAM";
    foreach(var parameter in new[]{LegalCatalog.StageId,LegalCatalog.FunctionId})
     if(view.get_Parameter(parameter) is {IsReadOnly:false} target)target.Set(LegalCatalog.Text(reference,parameter));
   }
   if(previous!=null){var ids=previous.Elements.Select(s=>doc.GetElement(s.UniqueId)).Where(e=>e!=null).Select(e=>e!.Id).ToList();if(ids.Count>0){var allowed=ids.ToHashSet();if(doc.Delete(ids).Any(id=>!allowed.Contains(id)))throw new InvalidOperationException("Existem elementos dependentes das marcações. Operação revertida.");}}
   var made=new List<Element>();var type=TextType(doc);var report=new List<string>();
   if(input.Key=="situacao")
   {
    view.ViewTemplateId=ElementId.InvalidElementId;view.Scale=500;
    var annotationCrop=view.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);if(annotationCrop is {IsReadOnly:false})annotationCrop.Set(0);
    // A localization diagram intentionally excludes the physical building; it is not a surveyed map.
    foreach(Category category in doc.Settings.Categories)if(category.CategoryType==CategoryType.Model&&category.Id.Value!=(long)BuiltInCategory.OST_Lines&&view.CanCategoryBeHidden(category.Id))view.SetCategoryHidden(category.Id,true);
    var lines=doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);if(view.CanCategoryBeHidden(lines.Id))view.SetCategoryHidden(lines.Id,false);foreach(Category sub in lines.SubCategories)if(view.CanCategoryBeHidden(sub.Id))view.SetCategoryHidden(sub.Id,false);
    // Elevation and section markers belong to model navigation, not the localization diagram.
    foreach(var builtIn in new[]{BuiltInCategory.OST_Elev,BuiltInCategory.OST_Sections,BuiltInCategory.OST_RoomSeparationLines}){var category=doc.Settings.Categories.get_Item(builtIn);if(category!=null&&view.CanCategoryBeHidden(category.Id))view.SetCategoryHidden(category.Id,true);}
    double z=((ViewPlan)view).GenLevel.Elevation,x=Implantation.Feet(options.LotMinX),y=Implantation.Feet(options.LotMinY),w=Implantation.Feet(options.LotWidth),h=Implantation.Feet(options.LotDepth);
    XYZ P(double a,double b)=>new(a,b,z);
    DetailCurve L(XYZ a,XYZ b){var c=doc.Create.NewDetailCurve(view,Line.CreateBound(a,b));view.SetElementOverrides(c.Id,new OverrideGraphicSettings().SetProjectionLineColor(new Color(0,0,0)).SetProjectionLineWeight(3));made.Add(c);return c;}
    void Text(double a,double b,string value)=>made.Add(TextNote.Create(doc,view.Id,P(a,b),value,type.Id));
    var edges=new[]{L(P(x,y),P(x+w,y)),L(P(x+w,y),P(x+w,y+h)),L(P(x+w,y+h),P(x,y+h)),L(P(x,y+h),P(x,y))};
    double gap=Implantation.Feet(3);
    void Dimension(int a,int b,XYZ p,XYZ q){var refs=new ReferenceArray();refs.Append(edges[a].GeometryCurve.Reference??new Reference(edges[a]));refs.Append(edges[b].GeometryCurve.Reference??new Reference(edges[b]));doc.Regenerate();made.Add(doc.Create.NewDimension(view,Line.CreateBound(p,q),refs,LegalPreferenceStore.Dimension(doc)));}
    Dimension(3,1,P(x,y-gap),P(x+w,y-gap));Dimension(0,2,P(x-gap,y),P(x-gap,y+h));
    // Text wrapping widths are in paper units. Neighbor names are outside the highlighted lot.
    void Wrapped(double a,double b,double paperWidth,string value)=>made.Add(TextNote.Create(doc,view.Id,P(a,b),Implantation.Feet(paperWidth),value,new TextNoteOptions(type.Id)));
    Wrapped(x+w*.2,y+h*.65,.055,"LOTE DO PROJETO\n"+(string.IsNullOrWhiteSpace(input.Identification)?"Identificação não informada":input.Identification)+"\n"+area.ToString("0.00",Implantation.Pt)+" m²");
    Wrapped(x-Implantation.Feet(35),y+h*.7,.05,"CONFRONTANTE ESQUERDO\n"+Missing(input.LeftNeighbor));
    Wrapped(x+w+Implantation.Feet(5),y+h*.7,.05,"CONFRONTANTE DIREITO\n"+Missing(input.RightNeighbor));
    Wrapped(x,y+h+Implantation.Feet(10),.065,"CONFRONTANTE AO FUNDO\n"+Missing(input.RearNeighbor));
    var side=options.Front;double offset=Implantation.Feet(options.SidewalkWidth+options.StreetWidth);
    if(options.AddStreet)
    {
     if(side=="Direita"||side=="Esquerda"){double xx=side=="Direita"?x+w+offset:x-offset;L(P(xx,y-gap),P(xx,y+h+gap));Text(xx+(side=="Direita"?gap:-Implantation.Feet(25)),y-gap-Implantation.Feet(6),"VIA: "+options.StreetName);}
     else{double yy=side=="Acima"?y+h+offset:y-offset;L(P(x-gap,yy),P(x+w+gap,yy));Text(x,yy+(side=="Acima"?Implantation.Feet(8):-Implantation.Feet(8)),"VIA: "+options.StreetName);}
    }
    double radians=options.NorthDegrees*Math.PI/180;var north=new XYZ(-Math.Sin(radians),Math.Cos(radians),0);var tail=P(x+w+Implantation.Feet(35),y+h);var tip=tail+north*Implantation.Feet(8);var right=new XYZ(north.Y,-north.X,0);L(tail,tip);L(tip,tip-north*Implantation.Feet(2)+right*Implantation.Feet(1));L(tip,tip-north*Implantation.Feet(2)-right*Implantation.Feet(1));Text(tip.X,tip.Y+Implantation.Feet(3),"N");
    Text(x-Implantation.Feet(35),y-Implantation.Feet(18),(options.Study?"ESTUDO FICTÍCIO — ":"")+"SITUAÇÃO ESQUEMÁTICA · conferir levantamento e cadastro municipal");
    if(!string.IsNullOrWhiteSpace(input.RegistrationText))Wrapped(x-Implantation.Feet(35),y-Implantation.Feet(30),.11,input.RegistrationText);
    doc.Regenerate();var box=view.CropBox;var inverse=box.Transform.Inverse;var boxes=made.Select(e=>e.get_BoundingBox(view)).Where(b=>b!=null).ToList();var points=boxes.SelectMany(b=>new[]{inverse.OfPoint(b!.Min),inverse.OfPoint(b.Max)}).ToList();box.Min=new(points.Min(p=>p.X)-gap,points.Min(p=>p.Y)-gap,box.Min.Z);box.Max=new(points.Max(p=>p.X)+gap,points.Max(p=>p.Y)+gap,box.Max.Z);view.CropBox=box;view.CropBoxActive=true;view.CropBoxVisible=false;
    report.Add("Diagrama com lote confirmado, confrontantes informados, via e norte. Não equivale a mapa cadastral ou levantamento georreferenciado.");
   }
   else
   {
    view.ViewTemplateId=ElementId.InvalidElementId;view.Scale=1;
    var contour=Implantation.GetContour(doc,(Floor)doc.GetElement(new ElementId(options.FootprintId)));double projection=LegalMetrics.PolygonArea(contour.Select(p=>(Implantation.…12030 tokens truncated…4};AutomationProperties.SetName(box,label);if(box.SelectedItem==null)box.SelectedIndex=0;panel.Children.Add(box);return box;}
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
  doc=app.ActiveUIDocument.Document;var saved=ProjectRegistrationStore.Read(doc);
  RegistrationUi.Initialize(this,app,"Dados do projeto");var dock=new DockPanel();Content=dock;
  var header=RegistrationUi.Header("1 / 3","Dados do projeto","Identifique a casa, selecione um perfil municipal e confira o que será reutilizado.");DockPanel.SetDock(header,Dock.Top);dock.Children.Add(header);
  var footer=new DockPanel{Margin=new Thickness(24,15,24,20)};DockPanel.SetDock(footer,Dock.Bottom);dock.Children.Add(footer);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);
  var cancel=RegistrationUi.Button("Cancelar");cancel.Click+=(_,_)=>Close();actions.Children.Add(cancel);
  var save=RegistrationUi.Button("Salvar cadastro");save.Click+=(_,_)=>Save(false);actions.Children.Add(save);
  var go=RegistrationUi.Button("Salvar e preparar →",true);go.Click+=(_,_)=>Save(true);actions.Children.Add(go);
  var layout=new Grid{Margin=new Thickness(24,0,24,0)};layout.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});layout.ColumnDefinitions.Add(new(){Width=new GridLength(335)});dock.Children.Add(layout);
  var form=new StackPanel{Margin=new Thickness(0,0,8,0)};layout.Children.Add(new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});var guide=RegistrationUi.Guide(false);Grid.SetColumn(guide,1);layout.Children.Add(guide);
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
