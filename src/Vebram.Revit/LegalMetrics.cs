namespace Vebram.Revit;
/// <summary>Explicit arithmetic only; these values do not establish legal computability.</summary>
public static class LegalMetrics
{
 public static double PolygonArea(IReadOnlyList<(double X,double Y)> points)
 {
  if(points.Count<3||points.Any(p=>!double.IsFinite(p.X)||!double.IsFinite(p.Y)))throw new ArgumentException("Contorno inválido.");
  // Translate near the origin to reduce cancellation for large survey coordinates.
  double x=points[0].X,y=points[0].Y;return Math.Abs(points.Select((p,i)=>{var q=points[(i+1)%points.Count];return(p.X-x)*(q.Y-y)-(q.X-x)*(p.Y-y);}).Sum())*.5;
 }
 public static double Ratio(double numerator,double lot,bool percentage)
 {if(!double.IsFinite(numerator)||!double.IsFinite(lot)||numerator<0||lot<=0)throw new ArgumentOutOfRangeException(nameof(lot));return numerator/lot*(percentage?100:1);}
 public static string Compare(double value,double? limit,bool maximum)
 {if(!limit.HasValue)return "limite não informado";return(maximum?value<=limit.Value+1e-9:value>=limit.Value-1e-9)?"dentro do limite informado":"fora do limite informado";}
}
