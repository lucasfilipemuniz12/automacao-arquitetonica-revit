namespace Vebram.Revit;

public sealed record PaperItem(long Id,double Width,double Height);
public sealed record PaperPlacement(long Id,LegalPaperRect Rect);
public sealed record GroupedPaperItem(long Id,string Key,double Width,double Height);
/// <summary>Deterministic paper-space packing. Never rotates a drawing or changes its scale.</summary>
public static class SheetPacking
{
 /// <summary>Pack complete cuts/facade groups as blocks, so consolidation never scatters them.</summary>
 public static List<PaperPlacement>? Grouped(LegalPaperRect area,IReadOnlyList<LegalPaperRect> obstacles,IReadOnlyList<GroupedPaperItem> items,double gap)
 {
  var blocks=new List<(PaperItem Size,List<PaperPlacement> Children)>();long blockId=long.MinValue;
  var groups=items.GroupBy(i=>i.Key.StartsWith("fachada-")?"fachadas":i.Key is "corte-a" or "corte-b"?"cortes":i.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
  foreach(var group in groups)
  {
   var members=group.OrderBy(i=>i.Key,StringComparer.Ordinal).Select(i=>new PaperItem(i.Id,i.Width,i.Height)).ToList();List<PaperPlacement>? children;
   if(members.Count==1)children=new(){new(members[0].Id,new(0,0,members[0].Width,members[0].Height))};
   else
   {
    var rows=group.Key=="fachadas"?members.Chunk(2).Select(r=>(IReadOnlyList<PaperItem>)r).ToList():new List<IReadOnlyList<PaperItem>>{members};
    children=Rows(new(0,0,area.Right-area.Left,area.Top-area.Bottom),rows,gap);
    if(children==null&&group.Key=="cortes")children=Rows(new(0,0,area.Right-area.Left,area.Top-area.Bottom),members.Select(m=>(IReadOnlyList<PaperItem>)new[]{m}).ToList(),gap);
    if(children==null)return null;
   }
   double left=children.Min(c=>c.Rect.Left),bottom=children.Min(c=>c.Rect.Bottom),width=children.Max(c=>c.Rect.Right)-left,height=children.Max(c=>c.Rect.Top)-bottom;
   blocks.Add((new(blockId++,width,height),children.Select(c=>c with{Rect=c.Rect.Move(-left,-bottom)}).ToList()));
  }
  var packed=obstacles.Count==0?Rows(area,blocks.Select(b=>(IReadOnlyList<PaperItem>)new[]{b.Size}).ToList(),gap):null;
  packed??=Pack(area,obstacles,blocks.Select(b=>b.Size).ToList(),gap);if(packed==null)return null;
  return packed.SelectMany(p=>blocks.First(b=>b.Size.Id==p.Id).Children.Select(c=>c with{Rect=c.Rect.Move(p.Rect.Left,p.Rect.Bottom)})).ToList();
 }
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
  "planta-1" or "planta-2" or "areas" or "portas" or "janelas" or "memoria"=>2,
  "cobertura" or "corte-a" or "corte-b"=>3,
  _=>4
 };
 public static string SheetName(int group,string profile)=>group switch
 {1=>profile=="r20"?"Situação e quadros":"Implantação, situação e quadros",2=>"Plantas legais e áreas",3=>profile=="r20"?"Implantação, cobertura e cortes":"Cobertura e cortes",_=>"Fachadas"};
}
