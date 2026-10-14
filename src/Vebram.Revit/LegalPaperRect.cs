namespace Vebram.Revit;
/// <summary>Paper rectangles use feet consistently with the Revit sheet API.</summary>
public readonly record struct LegalPaperRect(double Left,double Bottom,double Right,double Top)
{
 public LegalPaperRect Move(double x,double y)=>new(Left+x,Bottom+y,Right+x,Top+y);
 public bool Intersects(LegalPaperRect r,double gap)=>Left<r.Right+gap&&Right>r.Left-gap&&Bottom<r.Top+gap&&Top>r.Bottom-gap;
 public bool Contains(LegalPaperRect r,double tolerance=0)=>r.Left>=Left-tolerance&&r.Right<=Right+tolerance&&r.Bottom>=Bottom-tolerance&&r.Top<=Top+tolerance;
 public LegalPaperRect? FindFree(IReadOnlyList<LegalPaperRect> occupied,LegalPaperRect item,double step,double gap)
 {
  if(!double.IsFinite(step)||step<=0||!double.IsFinite(gap)||gap<0)throw new ArgumentOutOfRangeException(nameof(step));
  if(item.Right<item.Left||item.Top<item.Bottom||item.Right-item.Left>Right-Left||item.Top-item.Bottom>Top-Bottom)return null;
  for(double yy=Top;yy>=Bottom;yy-=step)for(double xx=Left;xx<=Right;xx+=step){var candidate=item.Move(xx-item.Left,yy-item.Top);if(Contains(candidate)&&!occupied.Any(r=>candidate.Intersects(r,gap)))return candidate;}
  return null;
 }
}
