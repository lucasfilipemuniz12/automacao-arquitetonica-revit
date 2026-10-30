using Vebram.Revit;
int tests=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);tests++;}
var area=new LegalPaperRect(0,0,600,500);var pair=new[]{new PaperItem(1,250,180),new PaperItem(2,250,180)};
var layout=SheetPacking.Pack(area,[],pair,12)!;Check(layout.Count==2,"pair fits");Check(layout.All(p=>area.Contains(p.Rect)),"inside");Check(!layout[0].Rect.Intersects(layout[1].Rect,12-1e-6),"gap");
var obstacle=new LegalPaperRect(0,0,300,500);var constrained=SheetPacking.Pack(area,new[]{obstacle},pair,12)!;Check(constrained.All(p=>!p.Rect.Intersects(obstacle,12-1e-6)),"manual obstacle");
Check(SheetPacking.Pack(area,[],new[]{new PaperItem(1,601,20)},12)==null,"oversize");Check(SheetPacking.Pack(area,[],new[]{new PaperItem(1,500,600)},12)==null,"no rotation");
Check(SheetPacking.Pack(area,[],new[]{new PaperItem(1,600,500)},12)?.Count==1,"exact fit");Check(SheetPacking.Pack(area,[],pair,12)!.SequenceEqual(layout),"deterministic");
Check(SheetPacking.PreferredSheet("implantacao","preferencial")==SheetPacking.PreferredSheet("situacao","preferencial"),"site pair");Check(SheetPacking.PreferredSheet("implantacao","r20")==3,"R20 site");Check(SheetPacking.PreferredSheet("planta-1","r20")==2,"R20 floor");
Check(SheetPacking.PreferredSheet("corte-a","preferencial")==SheetPacking.PreferredSheet("corte-b","preferencial"),"cuts pair");Check(new[]{"fachada-norte","fachada-sul","fachada-leste","fachada-oeste"}.All(k=>SheetPacking.PreferredSheet(k,"preferencial")==4),"facades group");
var rows=SheetPacking.Rows(area,new IReadOnlyList<PaperItem>[]{new[]{new PaperItem(3,500,200)},pair},12)!;Check(rows.Count==3,"rows fit");Check(rows[0].Rect.Bottom>rows[1].Rect.Top,"roof above cuts");Check(Math.Abs(rows[1].Rect.Top-rows[2].Rect.Top)<1e-6,"cuts aligned");Check(SheetPacking.Rows(area,new IReadOnlyList<PaperItem>[]{new[]{new PaperItem(1,700,100)}},12)==null,"rows oversize");
var adaptiveArea=new LegalPaperRect(0,0,576,524);
var adaptiveItems=new[]{new GroupedPaperItem(100,"planta-1",322.6,237.2),new GroupedPaperItem(101,"fachada-norte",258.6,89.1),new GroupedPaperItem(102,"fachada-sul",258.6,89.1),new GroupedPaperItem(103,"fachada-leste",178.4,89.1),new GroupedPaperItem(104,"fachada-oeste",178.4,89.1)};
var combined=SheetPacking.Grouped(adaptiveArea,[],adaptiveItems,12)!;Check(combined.Count==5&&combined.All(p=>adaptiveArea.Contains(p.Rect,1e-6)),"adaptive small house");Check(combined.First(p=>p.Id==100).Rect.Bottom>combined.Where(p=>p.Id!=100).Max(p=>p.Rect.Top),"plan above facades");
for(int i=0;i<combined.Count;i++)for(int j=i+1;j<combined.Count;j++)Check(!combined[i].Rect.Intersects(combined[j].Rect,12-1e-6),"adaptive gap");
Check(SheetPacking.Grouped(adaptiveArea,[],adaptiveItems.Select(i=>i with{Width=i.Width*1.25,Height=i.Height*1.25}).ToList(),12)==null,"larger house needs separate sheets");
Check(SheetPacking.Grouped(adaptiveArea,[],adaptiveItems,12)!.SequenceEqual(combined),"adaptive deterministic");
var random=new Random(17);for(int trial=0;trial<100;trial++){var items=Enumerable.Range(0,8).Select(i=>new PaperItem(i,random.Next(30,180),random.Next(30,140))).ToList();var packed=SheetPacking.Pack(area,[],items,12);if(packed==null)continue;Check(packed.Count==items.Count&&packed.All(p=>area.Contains(p.Rect,1e-6)),"random bounds");for(int i=0;i<packed.Count;i++)for(int j=i+1;j<packed.Count;j++)Check(!packed[i].Rect.Intersects(packed[j].Rect,12-1e-6),"random collision");}
Check(SheetPacking.PreferredSheet("memoria","preferencial")==2&&SheetPacking.PreferredSheet("memoria","r20")==2,"memory accompanies plans");
Check(SheetPacking.PreferredSheet("areas-descobertas","preferencial")==2&&SheetPacking.PreferredSheet("areas-descobertas","r20")==2,"uncovered areas accompany useful area table, outside its sum");
Console.WriteLine($"{tests} verificações passaram.");
