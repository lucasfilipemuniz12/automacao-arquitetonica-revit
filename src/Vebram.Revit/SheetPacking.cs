namespace Vebram.Revit;

public sealed record PaperItem(long Id,double Width,double Height);
public sealed record PaperPlacement(long Id,LegalPaperRect Rect);
/// <summary>Deterministic paper-space packing. Never rotates a drawing or changes its scale.</summary>
public static class SheetPacking
{
 public static List<PaperPlacement>? Rows(LegalPaperRect area,IReadOnlyList<IReadOnlyList<PaperItem>> rows,double gap)
 {
  var present=rows.Where(r=>r.Count>0).ToList();if(present.Count==0)return [];
  double height=present.Sum(r=>r.Max(i=>i.Height))+gap*(present.Count-1);
  if(height>area.Top-area.Bottom+1e-9||present.Any(r=>r.Sum(i=>i.Width)+gap*(r.Count-1)>area.Right-area.Left+1e-9))return null;
  double top=(area.Top+area.Bottom+height)/2;var result=new List<PaperPlacement>();
  foreach(var row in present){double rowHeight=row.Max(i=>i.Height),width=row.Sum(i=>i.Width)+gap*(row.Count-1),left=(area.Left+area.Right-width)/2;foreach(var item in row){double y=top-(rowHeight-item.Height)/2;result.Add(new(item.Id,new(left,y-item.Height,left+item.Width,y)));left+=item.Width+gap;}top-=rowHeight+gap;}
  return result;
 }
 public static List<PaperPlacement>? Pack(LegalPaperRect area,IReadOnlyList<LegalPaperRect> obstacles,IReadOnlyList<PaperItem> items,double gap)
 {
  if(gap<0||!double.IsFinite(gap)||items.Any(i=>!double.IsFinite(i.Width)||!double.IsFinite(i.Height)||i.Width<=0||i.Height<=0))throw new ArgumentException("Dimensões de folha inválidas.");
  // Try alternate deterministic orders before deciding that a preferred group needs splitting.
  foreach(var order in new[]{items.OrderByDescending(i=>i.Width*i.Height).ThenBy(i=>i.Id),items.OrderByDescending(i=>i.Height).ThenByDescending(i=>i.Width).ThenBy(i=>i.Id),items.OrderByDescending(i=>i.Width).ThenByDescending(i=>i.Height).ThenBy(i=>i.Id)})
  {
   var occupied=obstacles.ToList();var result=new List<PaperPlacement>();bool failed=false;
   foreach(var item in order)
   {
    var xs=new[]{area.Left}.Concat(occupied.Select(r=>r.Right+gap)).Distinct().Order();
    var ys=new[]{area.Top}.Concat(occupied.Select(r=>r.Bottom-gap)).Distinct().OrderDescending();
    LegalPaperRect? found=null;
    foreach(var y in ys){foreach(var x in xs){var r=new LegalPaperRect(x,y-item.Height,x+item.Width,y);if(area.Contains(r,1e-9)&&!occupied.Any(o=>r.Intersects(o,gap-1e-9))){found=r;break;}}if(found!=null)break;}
    if(found==null){failed=true;break;}result.Add(new(item.Id,found.Value));occupied.Add(found.Value);
   }
   if(!failed)
   {
    // Center a whole composition only if no manual content constrains its position.
    if(obstacles.Count==0&&result.Count>0){double dx=(area.Left+area.Right-result.Min(p=>p.Rect.Left)-result.Max(p=>p.Rect.Right))/2,dy=(area.Bottom+area.Top-result.Min(p=>p.Rect.Bottom)-result.Max(p=>p.Rect.Top))/2;result=result.Select(p=>p with{Rect=p.Rect.Move(dx,dy)}).ToList();}
    return result;
   }
  }
  return null;
 }
 public static int PreferredSheet(string key,string profile)=>key switch
 {
  "situacao" or "urbanistico"=>1,
  "implantacao"=>profile=="r20"?3:1,
  "planta-1" or "planta-2" or "areas"=>2,
  "cobertura" or "corte-a" or "corte-b"=>3,
  _=>4
 };
 public static string SheetName(int group,string profile)=>group switch
 {1=>profile=="r20"?"Situação e quadros":"Implantação, situação e quadros",2=>"Plantas legais e áreas",3=>profile=="r20"?"Implantação, cobertura e cortes":"Cobertura e cortes",_=>"Fachadas"};
}
