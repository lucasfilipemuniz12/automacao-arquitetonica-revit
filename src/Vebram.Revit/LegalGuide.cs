using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Automation;
namespace Vebram.Revit;

/// <summary>Offline vector tutorial. Illustrations are examples, never project measurements.</summary>
public sealed class LegalGuide : Border
{
 readonly Canvas drawing=new(){Width=400,Height=300};
 readonly TextBlock explanation=new(){TextWrapping=TextWrapping.Wrap,FontSize=14,LineHeight=21};
 readonly Dictionary<int,Border> pins=[];
 readonly string kind;
 readonly Dictionary<int,(string Title,string Help)> steps=[];
 static readonly Brush Ink=new SolidColorBrush(Color.FromRgb(34,57,70));
 static readonly Brush Accent=new SolidColorBrush(Color.FromRgb(0,119,121));
 public LegalGuide(string kind)
 {
  this.kind=kind;Background=Brushes.White;BorderBrush=Brushes.LightGray;BorderThickness=new Thickness(1);CornerRadius=new CornerRadius(12);Padding=new Thickness(18);Margin=new Thickness(18,0,0,0);
  var body=new StackPanel();Child=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  body.Children.Add(new TextBlock{Text="VEJA ONDE CADA DADO ENTRA",FontSize=12,Foreground=Accent,FontWeight=FontWeights.Bold});
  body.Children.Add(new TextBlock{Text="Guia ilustrado",FontSize=24,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,5,0,5)});
  body.Children.Add(new TextBlock{Text="Clique em um campo para destacar sua função no desenho.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DimGray});
  body.Children.Add(new Viewbox{Child=drawing,Stretch=Stretch.Uniform,Margin=new Thickness(0,12,0,12)});
  body.Children.Add(new TextBlock{Text="EXEMPLO ESQUEMÁTICO • SEM ESCALA",FontSize=10,Foreground=Brushes.SlateGray,Margin=new Thickness(0,0,0,12)});
  body.Children.Add(explanation);Draw();Show(1);
 }
 void L(double x,double y,double x2,double y2,Brush? brush=null,double weight=1.5)=>drawing.Children.Add(new Line{X1=x,Y1=y,X2=x2,Y2=y2,Stroke=brush??Ink,StrokeThickness=weight});
 void R(double x,double y,double w,double h,Brush? fill=null){var r=new Rectangle{Width=w,Height=h,Fill=fill??Brushes.Transparent,Stroke=Ink,StrokeThickness=1.5};Canvas.SetLeft(r,x);Canvas.SetTop(r,y);drawing.Children.Add(r);}
 void T(double x,double y,string text,double size=12){var t=new TextBlock{Text=text,FontSize=size,Foreground=Ink};Canvas.SetLeft(t,x);Canvas.SetTop(t,y);drawing.Children.Add(t);}
 void Arrow(double x,double y,double xx,double yy){L(x,y,xx,yy,Accent,2);double a=Math.Atan2(yy-y,xx-x);L(xx,yy,xx-9*Math.Cos(a-.45),yy-9*Math.Sin(a-.45),Accent,2);L(xx,yy,xx-9*Math.Cos(a+.45),yy-9*Math.Sin(a+.45),Accent,2);}
 void Pin(int n,double x,double y,string title,string help)
 {
  steps[n]=(title,help);var pin=new Border{Width=25,Height=25,CornerRadius=new CornerRadius(13),Background=Accent,Child=new TextBlock{Text=n.ToString(),Foreground=Brushes.White,FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};
  Canvas.SetLeft(pin,x);Canvas.SetTop(pin,y);drawing.Children.Add(pin);pins[n]=pin;pin.MouseLeftButtonDown+=(_,_)=>Show(n);pin.ToolTip=title;
 }
 void Draw()
 {
  if(kind=="areas")
  {
   R(20,45,360,205);T(35,60,"Q04 · ÁREAS DOS AMBIENTES",16);L(20,98,380,98);T(35,108,"Ambiente                      Área");L(20,135,380,135);T(35,147,"Sala                                24,50 m²");T(35,177,"Quarto                            12,00 m²");T(35,207,"Cozinha                          10,20 m²");
   Pin(1,130,10,"Ambientes do modelo","O Q04 acompanha os ambientes nativos do Revit. Modele os limites e coloque um ambiente em cada espaço fechado.");Pin(2,340,135,"Áreas medidas","A área vem do ambiente no modelo. Corrija os limites se o valor estiver incorreto; o quadro será atualizado pelo Revit.");Pin(3,35,265,"Conferência","Ambientes sem área ou sem fechamento aparecem no diagnóstico. Confira nomes e níveis antes de colocar o quadro na folha.");return;
  }
  if(kind=="urbanistico")
  {
   R(15,35,370,235);T(28,46,kind=="areas"?"ÁREAS DOS AMBIENTES":"QUADRO URBANÍSTICO",15);
   T(28,82,"Lote: 300 m²     Projeção: 120 m²");T(28,106,"Computável: 150 m²   Permeável: 90 m²");
   L(15,140,385,140);T(28,150,"Índice     Permitido    Projeto    Situação");L(15,175,385,175);
   T(28,188,"TO          60%              40%        Atende");T(28,215,"CA          1,00              0,50       Atende");T(28,242,"TP          20%              30%        Atende");
   Pin(1,170,8,"Identificação do lote","Informe quadra, lote ou cadastro. O nome da rua vem da Implantação.");
   Pin(2,340,88,"Áreas em m²","Área computável soma as parcelas que a legislação manda contar. Área permeável é a parcela do lote que atende ao critério local. Não use a área total do terreno nos dois campos.");
   Pin(3,105,128,"Limites permitidos","TO e TP são percentuais: digite 60 para 60%. CA é um coeficiente: digite 1,5, sem %. Consulte os critérios aplicáveis ao seu lote.");
   Pin(4,290,265,"Leitura do resultado","TO = projeção ÷ lote. CA = área computável ÷ lote. TP = área permeável ÷ lote. Sem área ou limite informado, o resultado fica pendente.");return;
  }
  if(kind=="cobertura")
  {
   // Roof seen from above: two waters, a ridge, slope arrows with example percentages and north.
   var water=new SolidColorBrush(Color.FromRgb(228,242,242));
   R(55,45,290,190,water);L(200,45,200,235,Ink,2.5);R(55,45,290,190);
   Arrow(128,120,128,160);T(95,168,"ÁGUA 1 · 5%*",12);Arrow(272,170,272,130);T(238,100,"ÁGUA 2 · 3%*",12);
   T(120,245,"cumeeira / divisão das águas",11);Arrow(370,70,370,22);T(379,8,"N");
   Pin(1,28,22,"Piso base","Escolha o piso do pavimento sob a cobertura. Ele define quais telhados e lajes pertencem a esta planta.");
   Pin(2,150,80,"Águas e caimentos","Cada água é uma superfície da cobertura. Inclinações modeladas são lidas automaticamente. Em lajes planas, o percentual e o sentido vêm da configuração de caimentos feita na Implantação; sem dados, aparece \"caimento a definir\". Os 5% e 3% do desenho são só exemplos.");
   Pin(3,300,200,"Atualização","Se mudar o telhado, a laje ou os caimentos na Implantação, prepare a Cobertura de novo. Marcas do gerador que você editou só são refeitas se marcar a opção.");return;
  }
  if(kind=="situacao")
  {
   // Same orientation as the generated diagram: X to the right, Y up. Left/right/rear are read on the drawing, not from the street.
   var neighbor=new SolidColorBrush(Color.FromRgb(237,240,242));
   R(105,95,190,105,new SolidColorBrush(Color.FromRgb(228,242,242)));T(172,140,"LOTE");T(128,112,"Quadra 12 · Lote 5",11);
   R(15,95,85,105,neighbor);T(25,140,"ESQUERDA",11);R(300,95,85,105,neighbor);T(312,140,"DIREITA",11);R(105,30,190,60,neighbor);T(168,53,"FUNDO",11);
   R(15,215,370,40,new SolidColorBrush(Color.FromRgb(225,232,229)));T(152,226,"FRENTE · VIA",12);
   Arrow(355,78,355,22);T(364,8,"N");
   Pin(1,120,70,"Identificação do lote","Informe quadra, lote ou cadastro (por exemplo: Quadra 12 · Lote 5). O texto aparece dentro do lote no diagrama de situação.");
   Pin(2,43,70,"Confrontante esquerdo","Terreno vizinho do lado ESQUERDO do desenho. Escreva o nome do proprietário ou a identificação do lote vizinho. Vazio aparece como NÃO INFORMADO.");
   Pin(3,330,70,"Confrontante direito","Terreno vizinho do lado DIREITO do desenho. Vazio aparece como NÃO INFORMADO.");
   Pin(4,215,3,"Confrontante ao fundo","Terreno vizinho na parte de CIMA do desenho (os fundos do lote). Vazio aparece como NÃO INFORMADO.");
   Pin(5,5,262,"Conferência","Esquerda, direita e fundo seguem os eixos do desenho da Implantação, não a visão de quem está na rua. Confira antes de confirmar. O diagrama não substitui mapa cadastral nem levantamento topográfico.");
   Pin(6,320,225,"Via e norte (vêm da Implantação)","Lote, via e norte não são digitados aqui: vêm da Implantação já confirmada. Se algo estiver errado, corrija na Implantação e refaça a Situação.");return;
  }
  if(kind=="planta")
  {
   R(55,45,290,180);R(62,52,126,80);R(195,52,143,166);R(62,139,126,79);T(88,77,"QUARTO");T(92,96,"12,00 m²");T(230,112,"SALA");T(226,132,"24,50 m²");T(82,162,"COZINHA");T(90,181,"10,20 m²");L(55,248,345,248);L(55,242,55,254);L(345,242,345,254);T(184,257,"10,00");
   Pin(1,35,23,"Piso base","Escolha o piso do pavimento selecionado na tela inicial. Ele define o contorno geral da planta legal.");Pin(2,290,150,"Nomes e áreas","Os ambientes precisam existir e estar fechados no modelo. O plugin posiciona nomes e áreas dentro deles; ambientes pequenos podem exigir ajuste de leitura.");Pin(3,35,259,"Cotas e escala","As cotas usam referências do modelo e o estilo escolhido nas Preferências legais. Ali também ficam escala, cotas internas, espessuras e larguras de esquadrias. Uma referência ausente é relatada, sem inventar a medida.");return;
  }
  if(kind=="corte"||kind=="fachadas")
  {
   if(kind=="corte"){R(55,10,250,55);L(45,37,315,37,Accent,2);Arrow(120,37,120,21);T(315,29,"A");T(55,70,"Planta: corte no centro (50%)",11);}
   R(55,95,250,125,new SolidColorBrush(Color.FromRgb(241,246,247)));L(45,95,315,95,Ink,3);L(30,222,335,222,Ink,2);R(75,155,42,65);R(205,140,65,40);
   if(kind=="corte"){L(178,95,178,220,Ink,3);T(76,190,"SALA");T(213,190,"SUÍTE");}
   L(335,95,335,222);L(326,95,344,95);L(326,222,344,222);T(342,145,"H");T(60,243,"terreno do modelo");Arrow(175,275,175,237);
   Pin(1,45,59,"Piso e fonte","Escolha o piso do pavimento que serve de referência. A vista mostra a geometria existente no modelo.");
   if(kind=="corte")Pin(2,140,48,"Direção e posição","Corte: X percorre a largura; Y percorre a profundidade. A posição de 50% atravessa o meio do piso. Confira quais ambientes a linha realmente corta.");
   else Pin(2,140,48,"Giro das fachadas","Se a casa está girada em relação aos eixos do projeto, informe o ângulo em graus. 0 usa Norte/Sul/Leste/Oeste direto dos eixos. O giro só orienta as fachadas; não muda o norte verdadeiro do modelo.");
   Pin(3,152,266,"Sentido e profundidade","Inverter troca o lado para o qual você olha. Profundidade é quanto aparece atrás da linha de corte, em metros. Nas fachadas, distância posiciona o observador fora da casa.");
   Pin(4,354,95,"Alturas","A altura total usa o topo da edificação e o terreno do modelo. O pé-direito usa faces reais de piso e teto; quando não há referência válida, o plugin informa a pendência.");return;
  }
  if(kind=="folhas")
  {
   R(25,30,350,235);R(315,30,60,235);R(43,48,250,92);R(43,160,118,75);R(178,160,115,75);T(322,230,"LEG");
   Pin(1,85,65,"Entregável preparado","Escolha uma vista já gerada e conferida. A escala é mantida ao colocá-la na folha.");
   Pin(2,328,60,"Folha e carimbo","A faixa do carimbo fica reservada. O plugin tenta a folha selecionada e depois as outras LEG existentes.");
   Pin(3,170,240,"Composição","Revisar composição organiza as vistas do conjunto V02 e retira apenas complementares geradas que ficaram vazias. Conteúdo manual pode ocupar espaço.");return;
  }
  // Plan, roof, site and location share the same orientation: X right, Y up.
  R(55,50,250,185);R(305,50,22,185,new SolidColorBrush(Color.FromRgb(225,232,229)));R(327,50,58,185,new SolidColorBrush(Color.FromRgb(237,240,242)));
  R(95,90,165,100,new SolidColorBrush(Color.FromRgb(228,242,242)));T(127,140,kind=="situacao"?"LOTE":"EDIFICAÇÃO");
  if(kind=="planta"){L(180,90,180,190);L(95,130,180,130);T(107,106,"QUARTO");T(198,110,"SALA");}
  if(kind=="cobertura"||kind=="implantacao"){L(178,90,178,190);Arrow(138,113,138,137);Arrow(218,170,218,145);T(111,165,"5%*");T(197,108,"3%*");}
  L(55,255,305,255);L(55,249,55,261);L(305,249,305,261);T(125,263,"largura (eixo X)");
  L(35,50,35,235);L(29,50,41,50);L(29,235,41,235);Arrow(278,36,278,6);T(288,5,"N");Arrow(355,145,308,145);T(337,190,"VIA");
  Pin(1,118,61,"Piso / contorno da casa","Selecione o piso principal do pavimento. O contorno do térreo define as cotas e os afastamentos em relação ao lote.");
  Pin(2,10,133,"Dimensões do lote","Largura é a dimensão horizontal X; profundidade é a vertical Y deste desenho. Use metros: 12,5 significa doze metros e meio.");
  Pin(3,42,222,"Posição X e Y","X/Y localizam o canto inferior esquerdo do lote nas coordenadas do modelo. Não são afastamentos. Confira o retângulo sugerido a partir do terreno.");
  Pin(4,343,116,"Frente, passeio e rua","Escolha o lado por onde se acessa o lote. O passeio fica junto ao limite; a via vem depois. Larguras em metros e nome da rua aparecem fora do lote.");
  Pin(5,249,4,"Norte","0° aponta para cima. 90° aponta para a esquerda. É a indicação informada para este desenho; confira a orientação do seu projeto.");
  Pin(6,170,197,kind=="situacao"?"Confrontantes":"Caimento por água",kind=="situacao"?"Esquerda, direita e fundo seguem os eixos do desenho. Informe a identificação dos terrenos vizinhos.":"Cada água é uma superfície do telhado. Inclinações modeladas são lidas automaticamente. Em lajes planas, informe porcentagem e direção por água no botão Configurar caimentos. Os 5% e 3% da figura são apenas exemplos.");
  Pin(7,12,276,"Escala e leitura","Em 1:100, 1 metro do modelo ocupa 1 centímetro no papel. Escolha a escala nas preferências e confira a leitura antes de organizar as folhas.");
 }
 public void Show(int number)
 {if(!steps.TryGetValue(number,out var step))number=1;step=steps[number];foreach(var pin in pins)pins[pin.Key].Background=pin.Key==number?Brushes.DarkOrange:Accent;explanation.Text=number+". "+step.Title+"\n\n"+step.Help;}
 int Match(string label)
 {
  var s=label.ToLowerInvariant();
  if(kind=="cobertura")return s.Contains("refazer")?3:1;
  if(kind=="situacao")return s.Contains("esquerd")?2:s.Contains("direit")?3:s.Contains("fundo")?4:s.Contains("conferi")||s.Contains("refazer")?5:1;
  if(kind=="urbanistico"||kind=="areas")return s.Contains("área computável")||s.Contains("área permeável")?2:s.Contains("máxim")||s.Contains("mínim")?3:s.Contains("confer")?4:1;
  if(kind=="corte"||kind=="fachadas")return s.Contains("direção")||s.Contains("posição")||s.Contains("giro")?2:s.Contains("sentido")||s.Contains("profundidade")||s.Contains("distância")?3:s.Contains("altura")?4:1;
  if(kind=="folhas")return s.Contains("complementar")?3:s.Contains("folha")?2:1;
  return s.Contains("largura do lote")||s.Contains("profundidade do lote")?2:s.Contains("canto inferior")?3:s.Contains("frente")||s.Contains("via")||s.Contains("passeio")?4:s.Contains("norte")?5:s.Contains("caimento")||s.Contains("confrontante")?6:s.Contains("escala")?7:1;
 }
 public void Bind(FrameworkElement element,string label)
 {int n=Match(label);string help=steps.GetValueOrDefault(n,steps[1]).Help;element.ToolTip=help;AutomationProperties.SetHelpText(element,help);element.GotKeyboardFocus+=(_,_)=>Show(n);element.MouseEnter+=(_,_)=>Show(n);}
 public void BindFields(Panel body)
 {
  string label="";TextBlock? previousLabel=null;foreach(UIElement child in body.Children)
  {if(child is TextBlock text){label=text.Text;previousLabel=text;}else if(child is Panel panel){BindFields(panel);previousLabel=null;}else if(child is Control control){var name=control is ContentControl cc&&cc.Content is string caption?caption:label;Bind(control,name);if(previousLabel!=null&&control is TextBox or ComboBox or Slider){int number=Match(name);previousLabel.Text=number+" · "+label;previousLabel.TextWrapping=TextWrapping.Wrap;}previousLabel=null;}}
 }
 public static Grid Beside(UIElement form,LegalGuide guide)
 {var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(400)});grid.Children.Add(form);Grid.SetColumn(guide,1);grid.Children.Add(guide);return grid;}
 public static void Attach(Window dialog,Panel form,string kind)
 {var guide=new LegalGuide(kind);guide.BindFields(form);dialog.Width=Math.Min(1120,SystemParameters.WorkArea.Width-40);dialog.Height=Math.Min(800,SystemParameters.WorkArea.Height-40);dialog.MinWidth=860;if(dialog.Content is ContentControl old&&ReferenceEquals(old.Content,form))old.Content=null;if(ReferenceEquals(dialog.Content,form))dialog.Content=null;dialog.Content=Beside(new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},guide);}
}
