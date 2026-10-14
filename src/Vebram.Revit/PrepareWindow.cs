using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ComboBox=System.Windows.Controls.ComboBox;
using Grid=System.Windows.Controls.Grid;
using Rectangle=System.Windows.Shapes.Rectangle;
using TextBox=System.Windows.Controls.TextBox;
using Panel=System.Windows.Controls.Panel;
using WpfLine=System.Windows.Shapes.Line;
namespace Vebram.Revit;
public sealed record SourceChoice(long Id,string Name){public override string ToString()=>Name;}
public sealed class PrepareWindow : Window
{
    readonly Document doc;
    readonly Grid root=new(){Margin=new Thickness(26)};
    readonly ComboBox sources=new(){MinHeight=36,DisplayMemberPath=nameof(SourceChoice.Name)};
    readonly ComboBox floors=new(){MinHeight=34,DisplayMemberPath=nameof(SourceChoice.Name)};
    readonly Dictionary<string,TextBox> fields=[];
    ComboBox scale=null!,front=null!,roofFlow=null!;
    CheckBox confirm=null!,study=null!,street=null!,replace=null!;
    public ImplantationOptions? Selection{get;private set;}
    public ModuleSelection? Module{get;private set;}
    public SectionSelection? Section{get;private set;}
    public IReadOnlyList<Request> Requests{get;}=[];
    ViewPlan? Source=>sources.SelectedItem is SourceChoice c?doc.GetElement(new ElementId(c.Id)) as ViewPlan:null;
    public PrepareWindow(UIApplication app)
    {
        doc=app.ActiveUIDocument.Document;Title="VEBRAM — Projeto legal";
        Width=1000;Height=820;MinWidth=880;MinHeight=650;
        Background=new SolidColorBrush(System.Windows.Media.Color.FromRgb(246,248,250));
        WindowStartupLocation=WindowStartupLocation.CenterOwner;new WindowInteropHelper(this).Owner=app.MainWindowHandle;
        Content=root;root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
        var header=new StackPanel();header.Children.Add(new TextBlock{Text="PROJETO LEGAL",Foreground=Brushes.SlateGray,FontSize=12});
        header.Children.Add(new TextBlock{Text="O que você quer preparar?",FontSize=27,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,5,0,5)});
        header.Children.Add(new TextBlock{Text=doc.Title,Foreground=Brushes.DimGray,Margin=new Thickness(0,0,0,17)});root.Children.Add(header);
        LoadSources(false);var saved=Implantation.Saved(doc);
        sources.SelectedItem=sources.Items.Cast<SourceChoice>().FirstOrDefault(c=>c.Id==saved?.SourceId)??
            sources.Items.Cast<SourceChoice>().FirstOrDefault(c=>c.Name.StartsWith("PRE - Layout / Nível 1"))??sources.Items.Cast<SourceChoice>().FirstOrDefault();
        Hub();
    }
    void LoadSources(bool all)
    {
        var previous=(sources.SelectedItem as SourceChoice)?.Id;var plans=Implantation.ModelPlans(doc,all);
        if(plans.Count==0)plans=Implantation.ModelPlans(doc,true);
        sources.ItemsSource=plans.Select(v=>new SourceChoice(v.Id.Value,v.Name+" · "+v.GenLevel.Name)).ToList();
        sources.SelectedItem=sources.Items.Cast<SourceChoice>().FirstOrDefault(c=>c.Id==previous)??sources.Items.Cast<SourceChoice>().FirstOrDefault();
    }
    void Page(UIElement element)
    {if(root.Children.Count>1)root.Children.RemoveAt(1);Grid.SetRow(element,1);root.Children.Add(element);}
    static TextBlock Label(string value)=>new(){Text=value,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,9,0,6)};
    void Hub()
    {
        if(sources.Parent is Panel parent)parent.Children.Remove(sources);
        var body=new StackPanel();Page(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        body.Children.Add(Label("1. Escolha sua planta de modelagem"));body.Children.Add(sources);
        body.Children.Add(new TextBlock{Text="A planta escolhida será preservada. O entregável recebe o perfil gráfico do template.",Foreground=Brushes.DimGray,Margin=new Thickness(0,8,0,0)});
        var more=new CheckBox{Content="Mostrar também outras plantas do projeto",Margin=new Thickness(0,10,0,10)};
        more.Checked+=(_,_)=>LoadSources(true);more.Unchecked+=(_,_)=>LoadSources(false);body.Children.Add(more);
        body.Children.Add(Label("2. Clique no entregável"));
        var cards=new UniformGrid{Columns=3,Margin=new Thickness(0,6,0,0)};body.Children.Add(cards);
        cards.Children.Add(Card("Implantação","Lote, cobertura, acesso e cotas","site",true,Configure));
        cards.Children.Add(Card("Situação","Localização e contexto do terreno","map"));
        cards.Children.Add(Card("Planta legal","Cotas gerais e áreas dos ambientes","plan",true,()=>ChooseModule("planta")));
        cards.Children.Add(Card("Corte A","Posição, sentido e profundidade","cut",true,()=>ChooseSection("corte-a")));cards.Children.Add(Card("Corte B","Posição, sentido e profundidade","cut",true,()=>ChooseSection("corte-b")));
        cards.Children.Add(Card("Fachadas","Elevações a partir do modelo","facade"));
        cards.Children.Add(Card("Cobertura","Inclinação e seta por água modelada","roof",true,()=>ChooseModule("cobertura")));
        cards.Children.Add(Card("Áreas dos ambientes","Quadro Q04 do template","area",true,()=>ChooseModule("areas")));
        cards.Children.Add(Card("Quadro urbanístico","Dados e critérios do projeto","area"));
        body.Children.Add(new TextBlock{Text="Estilo das cotas, escala e informação adicional: botão Preferências legais na faixa do Revit. Planta legal inclui cotas internas de acabamento e rótulos ajustados; cortes e fachadas serão o próximo lote.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray,Margin=new Thickness(0,12,0,0)});
    }
    Button Card(string title,string description,string kind,bool active=false,Action? action=null)
    {
        var content=new StackPanel();content.Children.Add(DrawIcon(kind,active));content.Children.Add(new TextBlock{Text=title,FontSize=17,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,6,0,4)});
        content.Children.Add(new TextBlock{Text=description,FontSize=12,TextWrapping=TextWrapping.Wrap});
        content.Children.Add(new TextBlock{Text=active?"CONFIGURAR E CRIAR →":"PRÓXIMA ETAPA",FontSize=10,Foreground=active?Brushes.Teal:Brushes.SlateGray,Margin=new Thickness(0,8,0,0)});
        var button=new Button{Content=content,Padding=new Thickness(16,10,16,10),Margin=new Thickness(0,0,12,12),MinHeight=124,
            HorizontalContentAlignment=HorizontalAlignment.Left,Background=Brushes.White,BorderBrush=active?Brushes.Teal:Brushes.LightGray,BorderThickness=new Thickness(active?2:1),IsEnabled=active};
        if(action!=null)button.Click+=(_,_)=>action();return button;
    }
    static Canvas DrawIcon(string kind,bool active)
    {
        var canvas=new Canvas{Width=90,Height=32,HorizontalAlignment=HorizontalAlignment.Left};var ink=active?Brushes.Teal:Brushes.SlateGray;
        void Line(double a,double b,double c,double d)=>canvas.Children.Add(new WpfLine{X1=a,Y1=b,X2=c,Y2=d,Stroke=ink,StrokeThickness=1.5});
        void Rect(double x,double y,double w,double h){var r=new Rectangle{Width=w,Height=h,Stroke=ink,StrokeThickness=1.5};Canvas.SetLeft(r,x);Canvas.SetTop(r,y);canvas.Children.Add(r);}
        if(kind is "site" or "map"){Rect(3,2,66,28);Rect(20,8,32,16);Line(79,29,79,3);Line(79,3,75,9);Line(79,3,83,9);if(kind=="map"){Line(0,16,88,16);Line(36,0,36,32);}}
        else if(kind is "cut" or "facade"){Line(3,30,82,30);Rect(14,10,56,20);Rect(24,20,11,10);Rect(49,17,14,8);Line(10,10,74,10);if(kind=="cut")Line(41,0,41,32);}
        else if(kind=="roof"){Rect(8,4,68,25);Line(8,4,42,17);Line(76,4,42,17);Line(42,17,42,29);}
        else if(kind=="area"){Rect(5,2,75,28);Line(5,12,80,12);Line(5,22,80,22);Line(50,2,50,30);}
        else{Rect(5,2,74,28);Line(45,2,45,30);Line(5,16,45,16);Line(61,2,61,30);}return canvas;
    }
    void Configure()
    {
        if(Source==null){MessageBox.Show(this,"Escolha uma planta de modelagem.");return;}
        var candidates=Implantation.Footprints(doc,Source);if(candidates.Count==0){MessageBox.Show(this,"Escolha o nível em que o piso da casa foi modelado.");return;}
        var layout=new DockPanel();Page(layout);var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,15,0,0)};
        DockPanel.SetDock(footer,Dock.Bottom);layout.Children.Add(footer);
        var back=new Button{Content="← Voltar aos entregáveis",Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,0,12,0)};back.Click+=(_,_)=>Hub();footer.Children.Add(back);
        var create=new Button{Content="Preparar Implantação",Padding=new Thickness(22,9,22,9),Background=Brushes.Teal,Foreground=Brushes.White,IsDefault=true};create.Click+=(_,_)=>Accept();footer.Children.Add(create);
        var content=new StackPanel();layout.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        content.Children.Add(new TextBlock{Text="Implantação",FontSize=23,FontWeight=FontWeights.SemiBold});
        content.Children.Add(new TextBlock{Text="Origem: "+Source.Name+"\nPerfil gráfico: V02. Confira os dados específicos deste lote.",Margin=new Thickness(0,8,0,12)});
        content.Children.Add(Label("Contorno do térreo — escolha o piso principal"));floors.ItemsSource=candidates.Select(f=>new SourceChoice(f.Id.Value,f.Name+" · "+
            ((f.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble()??0)*.09290304).ToString("0.00",Implantation.Pt)+" m²")).ToList();
        var saved=Implantation.Saved(doc);floors.SelectedItem=floors.Items.Cast<SourceChoice>().FirstOrDefault(c=>c.Id==saved?.FootprintId)??floors.Items.Cast<SourceChoice>().First();
        if(floors.Parent is Panel fp)fp.Children.Remove(floors);content.Children.Add(floors);
        var settings=new Grid{Margin=new Thickness(0,10,0,10)};settings.ColumnDefinitions.Add(new());settings.ColumnDefinitions.Add(new());
        var left=new StackPanel{Margin=new Thickness(0,0,24,0)};var right=new StackPanel();Grid.SetColumn(right,1);settings.Children.Add(left);settings.Children.Add(right);content.Children.Add(settings);
        var lot=Implantation.SuggestedLot(doc);fields.Clear();
        left.Children.Add(Label("Lote retangular · dimensões em metros"));
        Field(left,"Largura do lote","width",saved?.LotWidth??lot?.Width??0);Field(left,"Profundidade do lote","depth",saved?.LotDepth??lot?.Depth??0);
        Field(left,"Canto inferior esquerdo · X (m)","x",saved?.LotMinX??lot?.X??0);Field(left,"Canto inferior esquerdo · Y (m)","y",saved?.LotMinY??lot?.Y??0);
        left.Children.Add(new TextBlock{Text="O terreno sugere esses valores. Confirme se o retângulo corresponde ao lote. X/Y usam as coordenadas do modelo.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray,Margin=new Thickness(0,7,0,7)});
        left.Children.Add(Label("Escala do entregável"));scale=new ComboBox{ItemsSource=new[]{"1:100","1:50","1:200"},SelectedItem="1:"+(saved?.Scale??100),MinHeight=34};left.Children.Add(scale);
        right.Children.Add(Label("Lado da frente, no desenho"));front=new ComboBox{ItemsSource=new[]{"Direita","Esquerda","Acima","Abaixo"},SelectedItem=saved?.Front??"Direita",MinHeight=34};right.Children.Add(front);
        Field(right,"Norte · giro a partir do eixo vertical (graus)","north",saved?.NorthDegrees??0);
        street=new CheckBox{Content="Representar passeio e via com os dados abaixo",IsChecked=saved?.AddStreet??doc.Title.Contains("Casa_Terras",StringComparison.OrdinalIgnoreCase),Margin=new Thickness(0,12,0,6)};right.Children.Add(street);
        Field(right,"Largura do passeio (m)","sidewalk",saved?.SidewalkWidth??1.8);Field(right,"Largura da via (m)","road",saved?.StreetWidth??7);
        right.Children.Add(Label("Nome da via"));var name=new TextBox{Text=saved?.StreetName??"Rua de estudo (fictícia)",MinHeight=32,Padding=new Thickness(6)};fields["streetName"]=name;right.Children.Add(name);
        left.Children.Add(Label("Caimento de projeto da laje plana (%) · opcional"));
        var slope=new TextBox{Text=saved?.DesignRoofSlope?.ToString("0.###",Implantation.Pt)??"",MinHeight=32,Padding=new Thickness(6)};
        fields["slope"]=slope;left.Children.Add(slope);
        left.Children.Add(new TextBlock{Text="Telhados inclinados: ler cada água e seu sentido automaticamente. Este campo só anota uma proposta para a cobertura principal plana de uma superfície; não altera a geometria.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray,Margin=new Thickness(0,6,0,6)});
        left.Children.Add(Label("Sentido da seta de caimento"));roofFlow=new ComboBox{ItemsSource=new[]{"Modelo","Direita","Esquerda","Acima","Abaixo"},SelectedItem=saved?.RoofFlow??"Modelo",MinHeight=34};left.Children.Add(roofFlow);
        study=new CheckBox{Content="Estudo fictício (identificar no desenho)",IsChecked=saved?.Study??doc.Title.Contains("Casa_Terras",StringComparison.OrdinalIgnoreCase),Margin=new Thickness(0,6,0,6)};content.Children.Add(study);
        confirm=new CheckBox{Content="Conferi o contorno, a posição do lote e o norte informado",Margin=new Thickness(0,8,0,6)};content.Children.Add(confirm);
        replace=new CheckBox{Content="Refazer também marcações do gerador que editei manualmente",Margin=new Thickness(0,6,0,6)};content.Children.Add(replace);
        content.Children.Add(new TextBlock{Text="Será completada LEGAL - Implantação: cobertura vista de cima, contorno do térreo, cotas do lote/afastamentos, norte, acesso e chamadas. A casa é preservada; a folha será escolhida depois.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DimGray,Margin=new Thickness(0,8,0,0)});
    }
    void Field(Panel parent,string label,string key,double value){parent.Children.Add(Label(label));var field=new TextBox{Text=value.ToString("0.###",Implantation.Pt),MinHeight=32,Padding=new Thickness(6)};fields[key]=field;parent.Children.Add(field);}
    void ChooseModule(string key)
    {
        if(Source==null){MessageBox.Show(this,"Escolha sua planta de modelagem.");return;}
        var title=key=="planta"?"Planta legal":key=="cobertura"?"Cobertura":"Áreas dos ambientes";
        var dialog=new Window{Owner=this,Title=title,Width=620,Height=350,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var body=new StackPanel{Margin=new Thickness(24)};dialog.Content=body;
        body.Children.Add(new TextBlock{Text=title,FontSize=22,Margin=new Thickness(0,0,0,10)});
        body.Children.Add(new TextBlock{Text="Fonte: "+Source.Name,TextWrapping=TextWrapping.Wrap});
        var candidates=Implantation.Footprints(doc,Source);var bases=new ComboBox{ItemsSource=candidates.Select(f=>new SourceChoice(f.Id.Value,f.Name)).ToList(),MinHeight=32,Margin=new Thickness(0,10,0,10)};
        if(candidates.Count>0)bases.SelectedIndex=0;if(key!="areas"){body.Children.Add(Label("Piso base do nível escolhido"));body.Children.Add(bases);}
        var edited=new CheckBox{Content="Refazer marcas do gerador que editei manualmente",Margin=new Thickness(0,12,0,12)};if(key!="areas")body.Children.Add(edited);
        body.Children.Add(new TextBlock{Text=key=="planta"?"Cotas gerais e internas entre acabamentos; nomes e áreas priorizados dentro dos ambientes. Espessuras e larguras de esquadrias opcionais nas preferências. Cotas sem espaço ou referência válida são relatadas.":key=="cobertura"?"Ler inclinação e sentido de cada água plana. Telhados curvos exigem outro perfil.":"Abrir o quadro Q04 existente, que acompanha as áreas do modelo.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        var apply=new Button{Content="Preparar "+title,Padding=new Thickness(15,8,15,8),HorizontalAlignment=HorizontalAlignment.Right};body.Children.Add(apply);apply.Click+=(_,_)=>{if(key!="areas"&&bases.SelectedItem==null){MessageBox.Show(dialog,"Este nível não possui piso base disponível.");return;}dialog.DialogResult=true;};
        if(dialog.ShowDialog()==true){Module=new(key,Source.Id.Value,(bases.SelectedItem as SourceChoice)?.Id??0,edited.IsChecked==true);DialogResult=true;}
    }
    void ChooseSection(string key)
    {
        if(Source==null){MessageBox.Show(this,"Escolha sua planta de modelagem.");return;}
        var title=key=="corte-a"?"Corte A":"Corte B";
        var dialog=new Window{Owner=this,Title=title,Width=650,Height=590,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var body=new StackPanel{Margin=new Thickness(24)};dialog.Content=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        body.Children.Add(new TextBlock{Text=title,FontSize=23});body.Children.Add(new TextBlock{Text="Fonte: "+Source.Name,TextWrapping=TextWrapping.Wrap});
        var candidates=Implantation.Footprints(doc,Source);var bases=new ComboBox{ItemsSource=candidates.Select(f=>new SourceChoice(f.Id.Value,f.Name)).ToList(),MinHeight=32};if(candidates.Count>0)bases.SelectedIndex=0;
        body.Children.Add(Label("Piso base"));body.Children.Add(bases);
        var axis=new ComboBox{ItemsSource=new[]{"Ao longo de X — posição medida em Y","Ao longo de Y — posição medida em X"},SelectedIndex=key=="corte-a"?0:1,MinHeight=32};body.Children.Add(Label("Direção do corte (eixos do projeto)"));body.Children.Add(axis);
        var position=new Slider{Minimum=5,Maximum=95,Value=50,TickFrequency=5,IsSnapToTickEnabled=true,Margin=new Thickness(0,5,0,0)};var location=new TextBlock{Text="50% — centro da casa"};position.ValueChanged+=(_,_)=>location.Text=position.Value.ToString("0")+"% da extensão do piso";
        body.Children.Add(Label("Posição dentro da casa"));body.Children.Add(position);body.Children.Add(location);
        var reverse=new CheckBox{Content="Inverter sentido de observação",Margin=new Thickness(0,12,0,5)};body.Children.Add(reverse);
        var depth=new TextBox{Text="15",MinHeight=32};body.Children.Add(Label("Profundidade visível, em metros"));body.Children.Add(depth);
        var edited=new CheckBox{Content="Refazer corte/marcações do gerador que editei",Margin=new Thickness(0,12,0,8)};body.Children.Add(edited);
        body.Children.Add(new TextBlock{Text="Reutiliza o corte do template, identifica níveis e cota altura entre faces horizontais quando disponíveis. Escala/estilo: Preferências legais. Não substitui detalhamento construtivo. Cortes com anotações manuais externas ao gerador exigem revisão antes de reposicionar.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        var apply=new Button{Content="Preparar "+title,Padding=new Thickness(16,8,16,8),HorizontalAlignment=HorizontalAlignment.Right};body.Children.Add(apply);
        apply.Click+=(_,_)=>{if(bases.SelectedItem is not SourceChoice floor||!double.TryParse(depth.Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var meters)||!double.IsFinite(meters)||meters<=.1||meters>200){MessageBox.Show(dialog,"Escolha o piso e informe uma profundidade entre 0,1 e 200 m.");return;}Section=new(key,Source.Id.Value,floor.Id,axis.SelectedIndex==0,position.Value,reverse.IsChecked==true,meters,edited.IsChecked==true);dialog.DialogResult=true;};
        if(dialog.ShowDialog()==true)DialogResult=true;
    }
    void Accept()
    {
        try
        {
            if(confirm.IsChecked!=true)throw new InvalidOperationException("Confira o lote e o norte e marque a confirmação.");
            double Number(string key){if(!double.TryParse(fields[key].Text.Trim().Replace(',', '.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n))throw new InvalidOperationException("Confira o campo: "+key);return n;}
            if(Source==null||floors.SelectedItem is not SourceChoice footprint)throw new InvalidOperationException("Escolha a planta e seu piso base.");
            Selection=new(Source.Id.Value,footprint.Id,Number("x"),Number("y"),Number("width"),Number("depth"),int.Parse(((string)scale.SelectedItem).Split(':')[1]),
                (string)front.SelectedItem,street.IsChecked==true,Number("sidewalk"),Number("road"),fields["streetName"].Text,Number("north"),study.IsChecked==true,replace.IsChecked==true,
                string.IsNullOrWhiteSpace(fields["slope"].Text)?null:Number("slope"),(string)roofFlow.SelectedItem);
            _=Implantation.Plan(doc,Selection);DialogResult=true;
        }
        catch(Exception e){MessageBox.Show(this,e.Message,"Conferir Implantação",MessageBoxButton.OK,MessageBoxImage.Information);}
    }
}

