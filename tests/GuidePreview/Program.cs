using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vebram.Revit;
internal static class Program
{
 [STAThread] static void Main(string[] args)
 {
  string output=args[0];Directory.CreateDirectory(output);var app=new Application();
  foreach(var kind in new[]{"implantacao","situacao","cobertura","planta","corte","fachadas","urbanistico","areas","folhas"})
  {
   var guide=new LegalGuide(kind){Width=430,Height=700,Margin=new Thickness(0)};Render(guide,430,700,Path.Combine(output,kind+".png"));
   foreach(bool scroll in new[]{false,true})
   {var panel=new StackPanel();panel.Children.Add(new TextBlock{Text="Piso base"});panel.Children.Add(new ComboBox());var window=new Window();window.Content=scroll?new ScrollViewer{Content=panel}:panel;LegalGuide.Attach(window,panel,kind);((FrameworkElement)window.Content).Measure(new Size(1100,740));window.Close();}
  }
  File.WriteAllText(Path.Combine(output,"CONCLUIDO.txt"),"9 guias renderizados; 18 formulários com e sem ScrollViewer reparentados sem erro.");app.Shutdown();
 }
 static void Render(FrameworkElement element,int width,int height,string path)
 {element.Measure(new Size(width,height));element.Arrange(new Rect(0,0,width,height));element.UpdateLayout();var image=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);image.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var stream=File.Create(path);encoder.Save(stream);}
}
