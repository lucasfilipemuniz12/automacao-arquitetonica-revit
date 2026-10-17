using System.Windows;
using System.Windows.Media;
namespace Vebram.Revit;

/// <summary>Original scalable line icons matching the illustrated deliverable cards.</summary>
internal static class RibbonIcons
{
 public static ImageSource For(string key)
 {
  var drawing=new DrawingGroup();var ink=new SolidColorBrush(Color.FromRgb(0,128,128));var pale=new SolidColorBrush(Color.FromRgb(224,245,239));var accent=new SolidColorBrush(Color.FromRgb(44,91,111));
  var pen=new Pen(ink,1.8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};
  using(var d=drawing.Open())
  {
   d.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,32,32));
   void Line(double x,double y,double a,double b)=>d.DrawLine(pen,new Point(x,y),new Point(a,b));
   void Rect(double x,double y,double w,double h)=>d.DrawRoundedRectangle(pale,pen,new Rect(x,y,w,h),1,1);
   void Dot(double x,double y)=>d.DrawEllipse(ink,null,new Point(x,y),1.4,1.4);
   if(key=="Cadastro")
   {
    Rect(4,4,23,25);Rect(10,2,11,5);Line(8,12,14,12);Line(8,17,14,17);Line(8,22,14,22);
    d.DrawEllipse(Brushes.White,pen,new Point(23,19),5,5);Line(20,23,23,28);Line(23,28,26,23);Dot(23,19);
   }
   else if(key=="Municipios")
   {
    Line(2,11,16,3);Line(16,3,30,11);Line(2,11,30,11);Line(4,27,28,27);Line(2,30,30,30);
    foreach(var x in new[]{7,16,25}){Line(x,14,x,24);Dot(x,7);}
   }
   else if(key=="Preparar")
   {
    Rect(3,4,23,23);Line(15,4,15,18);Line(3,18,20,18);Line(20,18,20,27);Line(15,11,26,11);
    d.DrawEllipse(Brushes.White,pen,new Point(25,25),5,5);Line(22.5,25,24.3,27);Line(24.3,27,28,23);
   }
   else if(key=="Preferencias")
   {
    Line(4,8,28,8);Line(4,16,28,16);Line(4,24,28,24);Rect(9,5,4,6);Rect(21,13,4,6);Rect(13,21,4,6);
   }
   else if(key=="Parametros")
   {
    Rect(3,4,26,24);Line(3,11,29,11);Line(12,11,12,28);Dot(7,16);Dot(7,23);Line(16,16,25,16);Line(16,23,23,23);
   }
   else if(key=="Demo")
   {
    Rect(5,2,20,27);Line(19,2,19,8);Line(19,8,25,8);Line(9,17,16,11);Line(16,11,23,17);Rect(11,17,10,9);Line(16,21,16,26);
   }
   else if(key=="Diagnosticar")
   {
    Rect(3,3,18,25);Line(7,8,17,8);Line(7,13,14,13);Line(7,18,11,18);
    d.DrawEllipse(Brushes.White,pen,new Point(21,21),6,6);Line(25.4,25.4,30,30);d.DrawLine(new Pen(accent,1.6),new Point(18,21),new Point(20,23));d.DrawLine(new Pen(accent,1.6),new Point(20,23),new Point(24,19));
   }
  }
  drawing.Freeze();var image=new DrawingImage(drawing);image.Freeze();return image;
 }
}

