using System.IO;
using System.Globalization;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
namespace Vebram.Revit;

// Developer fixture, opt-in only. NewProjectDocument(template) never opens/edits the RTE itself.
internal static class Case04Model
{
 static double F(double m)=>Implantation.Feet(m);
 static XYZ P(double x,double y,double z=0)=>new(F(x),F(y),F(z));
 internal static readonly List<SitePoint> Boundary=[new(0,0),new(12,0),new(14.5,2.5),new(15.2,26),new(-1,24.5)];
 static CurveLoop Loop(IEnumerable<XYZ> input){var p=input.ToList();return CurveLoop.Create(p.Select((a,i)=>(Curve)Line.CreateBound(a,p[(i+1)%p.Count])).ToList());}
 static CurveLoop Rect(double x,double y,double xx,double yy,double z=0)=>Loop([P(x,y,z),P(xx,y,z),P(xx,yy,z),P(x,yy,z)]);
 static double Natural(double x,double y)=>100+.16*y-.06*x+.002*(x-7)*(x-7)-100.4;
 static bool Inside(double x,double y)
 {bool yes=false;for(int i=0,j=Boundary.Count-1;i<Boundary.Count;j=i++)if((Boundary[i].Y>y)!=(Boundary[j].Y>y)&&x<(Boundary[j].X-Boundary[i].X)*(y-Boundary[i].Y)/(Boundary[j].Y-Boundary[i].Y)+Boundary[i].X)yes=!yes;return yes;}
 internal static void Build(UIApplication ui,string template,string destination,string dxf,string output)
 {
  if(File.Exists(destination))throw new InvalidOperationException("A CT04 original já existe; ela não será sobrescrita.");
  if(!template.EndsWith(".rte",StringComparison.OrdinalIgnoreCase)||!destination.EndsWith(".rvt",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use template RTE e destino RVT distintos.");
  Directory.CreateDirectory(output);Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
  var doc=ui.Application.NewProjectDocument(template);
  try
  {
   var wallTypes=new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().ToList();
   var floorTypes=new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().ToList();
   var symbols=new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().ToList();
   File.WriteAllText(Path.Combine(output,"TIPOS_TEMPLATE.json"),JsonSerializer.Serialize(new{Walls=wallTypes.Select(w=>new{w.Name,Kind=w.Kind.ToString(),Width=Implantation.Meters(w.Width)}),Floors=floorTypes.Select(f=>f.Name),Families=symbols.Select(s=>new{s.FamilyName,Name=s.Name})},new JsonSerializerOptions{WriteIndented=true}));
   if(new FilteredElementCollector(doc).OfClass(typeof(Wall)).Any()||new FilteredElementCollector(doc).OfClass(typeof(Floor)).Any())throw new InvalidOperationException("O template contém modelagem; conferir antes de criar a CT04. Nenhum elemento do template foi apagado.");
   var masonry=wallTypes.FirstOrDefault(w=>w.Kind==WallKind.Basic&&(w.Name.Contains("ALVEN",StringComparison.OrdinalIgnoreCase)||w.Name.Contains("cerâm",StringComparison.OrdinalIgnoreCase)||w.Name.Contains("ceram",StringComparison.OrdinalIgnoreCase)))??throw new InvalidOperationException("Alvenaria não encontrada no template; ver TIPOS_TEMPLATE.json.");
   var levels=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l=>l.Elevation).ToList();
   using(var tx=new Transaction(doc,"CT04 — níveis, paredes e pisos conforme ficha"))
   {
    tx.Start();
    Level LevelAt(string name,double height){var l=levels.FirstOrDefault(l=>Math.Abs(l.Elevation-F(height))<1e-5)??Level.Create(doc,F(height));l.Name=name;l.get_Parameter(BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT)?.Set(F(1.5));if(!levels.Contains(l))levels.Add(l);return l;}
    var ground=LevelAt("Térreo",0);var upper=LevelAt("Superior",3);var roofLevel=LevelAt("Cobertura",6);var garage=LevelAt("Garagem",-.6);LevelAt("Quintal",1);
    doc.ActiveProjectLocation.SetProjectPosition(XYZ.Zero,new ProjectPosition(0,0,F(100.4),0));
    WallType WallType(double width)
    {
     var found=wallTypes.FirstOrDefault(w=>w.Kind==WallKind.Basic&&Math.Abs(w.Width-F(width))<1e-5&&(w.Name.Contains("ALVEN",StringComparison.OrdinalIgnoreCase)||w.Name.Contains("cerâm",StringComparison.OrdinalIgnoreCase)));if(found!=null)return found;
     var t=(WallType)masonry.Duplicate("CT04 — ALVENARIA "+(width*100).ToString("0",CultureInfo.InvariantCulture)+" cm");var cs=t.GetCompoundStructure();
     cs.SetLayers([new CompoundStructureLayer(F(width),MaterialFunctionAssignment.Structure,cs.GetLayers().First().MaterialId)]);t.SetCompoundStructure(cs);wallTypes.Add(t);return t;
    }
    var walls=new List<Wall>();
    Wall W(double x,double y,double xx,double yy,Level l,double width=.1,double height=2.99)
    {var w=Wall.Create(doc,Line.CreateBound(P(x,y),P(xx,yy)),WallType(width).Id,l.Id,F(height),F(.01),false,false);w.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("CT04:alvenaria");walls.Add(w);return w;}
    void Shell(double x,double y,double xx,double yy,Level l,double height=2.99)
    {W(x+.075,y+.075,xx-.075,y+.075,l,.15,height);W(xx-.075,y+.075,xx-.075,yy-.075,l,.15,height);W(xx-.075,yy-.075,x+.075,yy-.075,l,.15,height);W(x+.075,yy-.075,x+.075,y+.075,l,.15,height);}
    Shell(1.5,3,9.5,11,ground);Shell(1.5,4.5,9.5,11,upper);
    W(2.8,3.15,2.8,10.85,ground);W(1.65,6.32,2.75,6.32,ground);W(2.85,7.05,9.35,7.05,ground);W(5.55,7.1,5.55,10.85,ground);W(2.85,9.4,5.5,9.4,ground);W(4.3,9.45,4.3,10.85,ground);
    W(2.8,4.65,2.8,10.85,upper);W(1.65,6.32,2.75,6.32,upper);W(5.25,4.65,5.25,10.85,upper);W(2.85,8.25,9.35,8.25,upper);W(7.25,8.3,7.25,10.85,upper);
    // Garage is a separate roof/slab block, with the supplied open front. Side/rear masonry supports it.
    W(9.675,3,9.675,8.425,garage,.15,2.79);W(12.525,3,12.525,8.425,garage,.15,2.79);W(9.675,8.425,12.525,8.425,garage,.15,2.79);
    FloorType FloorType(string token,double thickness)
    {
     var t=floorTypes.FirstOrDefault(t=>t.Name.Contains(token,StringComparison.OrdinalIgnoreCase)&&Math.Abs((t.GetCompoundStructure()?.GetWidth()??0)-F(thickness))<1e-5);
     if(t!=null)return t;
     var source=floorTypes.FirstOrDefault(t=>t.Name.Contains(token,StringComparison.OrdinalIgnoreCase))??floorTypes.First();var result=(FloorType)source.Duplicate("CT04 "+token+" / "+(thickness*100).ToString("0",CultureInfo.InvariantCulture)+" cm");var cs=source.GetCompoundStructure();cs.SetLayers([new CompoundStructureLayer(F(thickness),MaterialFunctionAssignment.Structure,cs.GetLayers().First().MaterialId)]);result.SetCompoundStructure(cs);result.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK)?.Set("CT04-"+token);floorTypes.Add(result);return result;
    }
    Floor Slab(string type,double x,double y,double xx,double yy,Level l,double top,double thickness,CurveLoop? hole=null)
    {var loops=new List<CurveLoop>{Rect(x,y,xx,yy)};if(hole!=null)loops.Add(hole);var f=Floor.Create(doc,loops,FloorType(type,thickness).Id,l.Id);f.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(F(top));f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("CT04:"+type);return f;}
    Slab("LAJE",1.5,3,9.5,11,ground,-.07,.15);
    var stairHole=Rect(1.65,6.37,2.75,10.85);
    Slab("LAJE",1.5,4.5,9.5,11,upper,-.07,.15,stairHole);
    Slab("LAJE GARAGEM",9.6,3,12.6,8.5,garage,-.07,.15);
    var rooms=new List<(string Name,double X,double Y,double XX,double YY,Level L)> {
     ("Entrada / hall",1.65,3.15,2.75,6.27,ground),("Estar / jantar",2.85,3.15,9.35,7,ground),("Circulação",2.85,7.1,5.5,9.35,ground),("Cozinha",5.6,7.1,9.35,10.85,ground),("Lavabo",2.85,9.45,4.25,10.85,ground),("Despensa",4.35,9.45,5.5,10.85,ground),("Escada",1.65,6.37,2.75,10.85,ground),
     ("Suíte",5.3,4.65,9.35,8.2,upper),("Quarto 02",2.85,4.65,5.2,8.2,upper),("Hall",2.85,8.3,5.2,10.85,upper),("Banho social",5.3,8.3,7.2,10.85,upper),("Banho suíte",7.3,8.3,9.35,10.85,upper),("Rouparia",1.65,4.65,2.75,6.27,upper),("Escada",1.65,6.37,2.75,10.85,upper)};
    int number=1;
    foreach(var r in rooms)
    {
     if(r.Name!="Escada")Slab("INTERNO",r.X,r.Y,r.XX,r.YY,r.L,0,.07);
     var room=doc.Create.NewRoom(r.L,new UV(F((r.X+r.XX)/2),F((r.Y+r.YY)/2)));room.Name=r.Name;room.Number=(number++).ToString("00");room.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET)?.Set(F(3));
    }
    Slab("EXTERNO",9.75,3,12.45,8.35,garage,0,.07);
    Slab("CALÇADA",9.6,0,12.6,3,garage,0,.07);Slab("CALÇADA",3,0,4.2,3,ground,0,.07);Slab("EXTERNO",1.5,11,9.5,13,ground,0,.07);
    // Shower footprints are not specified by the ficha; keep bathroom finishes intact and record this gap.
    var planType=new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(v=>v.ViewFamily==ViewFamily.FloorPlan);
    foreach(var l in new[]{ground,upper,garage})if(!new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Any(v=>!v.IsTemplate&&v.GenLevel?.Id==l.Id)){var p=ViewPlan.Create(doc,planType.Id,l.Id);p.Name="MODELO — "+l.Name;}
    var plan=new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().First(p=>!p.IsTemplate&&p.GenLevel?.Id==ground.Id);
    var garagePlan=new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().First(p=>!p.IsTemplate&&p.GenLevel?.Id==garage.Id);
    var frontBoundary=new CurveArray();frontBoundary.Append(Line.CreateBound(P(9.75,3,-.6),P(12.45,3,-.6)));
    doc.Create.NewRoomBoundaryLines(SketchPlane.Create(doc,Plane.CreateByNormalAndOrigin(XYZ.BasisZ,P(0,0,-.6))),frontBoundary,garagePlan);
    var garageRoom=doc.Create.NewRoom(garage,new UV(F(11.1),F(5.5)));garageRoom.Name="Garagem";garageRoom.Number="15";
    using var import=new DWGImportOptions{Unit=ImportUnit.Meter,Placement=ImportPlacement.Origin,ThisViewOnly=false,OrientToView=false,VisibleLayersOnly=false};
    if(!doc.Link(dxf,import,plan,out var linkId))throw new InvalidOperationException("O DXF não pôde ser vinculado.");doc.GetElement(linkId).Pinned=true;
    var roofType=new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().FirstOrDefault(t=>t.Name.Contains("cer",StringComparison.OrdinalIgnoreCase))??new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().First();
    var outline=new CurveArray();foreach(var c in Rect(.9,3.9,10.1,11.6))outline.Append(c);var mapping=new ModelCurveArray();var roof=doc.Create.NewFootPrintRoof(outline,roofLevel,roofType,out mapping);
    foreach(ModelCurve edge in mapping){var d=edge.GeometryCurve.GetEndPoint(1)-edge.GeometryCurve.GetEndPoint(0);roof.set_DefinesSlope(edge,Math.Abs(d.X)>Math.Abs(d.Y));if(Math.Abs(d.X)>Math.Abs(d.Y))roof.set_SlopeAngle(edge,.30);}
    var flat=(RoofType)roofType.Duplicate("CT04 LAJE IMPERMEABILIZADA / concreto 15 cm");var flatLayers=flat.GetCompoundStructure();flatLayers.SetLayers([new CompoundStructureLayer(F(.15),MaterialFunctionAssignment.Structure,FloorType("LAJE",.15).GetCompoundStructure().GetLayers().First().MaterialId)]);flat.SetCompoundStructure(flatLayers);
    RoofBase FlatRoof(double x,double y,double xx,double yy,Level l,double offset)
    {var ca=new CurveArray();foreach(var c in Rect(x,y,xx,yy))ca.Append(c);var edges=new ModelCurveArray();var r=doc.Create.NewFootPrintRoof(ca,l,flat,out edges);r.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).Set(F(offset-.15*Math.Sqrt(1.0004)));foreach(ModelCurve edge in edges){var a=edge.GeometryCurve.GetEndPoint(0);var b=edge.GeometryCurve.GetEndPoint(1);bool low=Math.Abs(a.Y-F(y))<1e-5&&Math.Abs(b.Y-F(y))<1e-5;r.set_DefinesSlope(edge,low);if(low)r.set_SlopeAngle(edge,.02);}return r;}
    FlatRoof(9.6,3,12.6,8.5,garage,2.8);FlatRoof(1.5,3,9.5,4.5,upper,0);
    doc.Regenerate();
    // Put all project plans in metres; the template's native unit formatting is retained.
    foreach(var p in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(p=>!p.IsTemplate))
    {p.Scale=100;var ids=new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).Where(e=>e.CanBeHidden(p)).Select(e=>e.Id).ToList();if(ids.Count>0)p.HideElements(ids);}
    tx.Commit();
   }
   Terrain(doc,output,dxf);
   Openings(doc,symbols,output);
   Stairs(doc,output);
   File.WriteAllText(Path.Combine(output,"MODELO_QC.json"),JsonSerializer.Serialize(new{Warnings=doc.GetWarnings().Select(w=>new{Description=w.GetDescriptionText(),Ids=w.GetFailingElements().Select(i=>i.Value)}),LotArea=LegalMetrics.PolygonArea(Boundary.Select(p=>(p.X,p.Y)).ToArray()),LinkedDXF=new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).Cast<ImportInstance>().All(i=>i.IsLinked),Rooms=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Room>().Select(r=>new{Name=r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString(),Area=r.Area*.09290304}),WallTypes=new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().Select(w=>w.WallType.Kind.ToString()).Distinct()},new JsonSerializerOptions{WriteIndented=true}));
   var opts=new SaveAsOptions{OverwriteExistingFile=false};doc.SaveAs(destination,opts);
  }
  finally
  {
   if(doc.IsModified&&string.IsNullOrWhiteSpace(doc.PathName))doc.SaveAs(Path.Combine(output,"CT04_INCOMPLETA_"+DateTime.Now.ToString("yyyyMMddHHmmss")+".rvt"),new SaveAsOptions{OverwriteExistingFile=false});
   if(!doc.IsModified)doc.Close(false);
  }
 }
 static void Terrain(Document doc,string output,string dxf)
 {
  using var tx=new Transaction(doc,"CT04 — curvas, corte e aterro");tx.Start();
  var level=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First(l=>l.Name=="Térreo");var sourceType=new FilteredElementCollector(doc).OfClass(typeof(ToposolidType)).Cast<ToposolidType>().First(t=>t.Name.StartsWith("Pradaria"));
  var loops=new List<CurveLoop>{Loop(Boundary.Select(p=>P(p.X,p.Y))),Rect(1.5,3,9.5,13)};
  var points=new List<XYZ>();
  foreach(var a in Boundary.Select((p,i)=>(P:p,Q:Boundary[(i+1)%Boundary.Count])))for(int i=0;i<=12;i++){double x=a.P.X+(a.Q.X-a.P.X)*i/12,y=a.P.Y+(a.Q.Y-a.P.Y)*i/12;points.Add(P(x,y,Natural(x,y)));}
  foreach(var r in new[]{(1.5,3d,9.5,13d),(9.6,3d,12.6,8.5)})foreach(var p in new[]{(r.Item1,r.Item2),(r.Item3,r.Item2),(r.Item3,r.Item4),(r.Item1,r.Item4)})points.Add(P(p.Item1,p.Item2,Natural(p.Item1,p.Item2)));
  var curvePoints=CurvePoints(dxf).Where(p=>Inside(p.X,p.Y)&&!(p.X>=1.5&&p.X<=9.5&&p.Y>=3&&p.Y<=13)).ToList();
  points.AddRange(curvePoints.Select(p=>P(p.X,p.Y,p.Z-100.4)));
  File.WriteAllText(Path.Combine(output,"CURVAS_LIDAS.json"),JsonSerializer.Serialize(new{Source=dxf,Units="metros",ContourPoints=curvePoints.Count,Boundary="Vértices/perímetro completados pela fórmula explícita da ficha",AbsoluteDatum=100.4}));
  var type=(ToposolidType)sourceType.Duplicate("CT04 terreno - espessura de modelagem");var soil=type.GetCompoundStructure();int variable=soil.VariableLayerIndex;soil.SetLayerWidth(variable,soil.GetLayerWidth(variable)+Math.Max(0,-points.Min(p=>p.Z)));type.SetCompoundStructure(soil);
  var topo=Toposolid.Create(doc,loops,points.DistinctBy(p=>(Math.Round(p.X,6),Math.Round(p.Y,6))).ToList(),type.Id,level.Id);topo.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("CT04:terreno pelas curvas DXF; corte nativo da garagem e aterro sob a casa");
  var infills=new List<Toposolid>();
  foreach(var r in new[]{(1.5,3d,9.5,11d,-.22),(1.5,11d,9.5,13d,-.07)})
  {var infill=Toposolid.Create(doc,[Rect(r.Item1,r.Item2,r.Item3,r.Item4)],[P(r.Item1,r.Item2,r.Item5),P(r.Item3,r.Item2,r.Item5),P(r.Item3,r.Item4,r.Item5),P(r.Item1,r.Item4,r.Item5)],type.Id,level.Id);infill.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("CT04:corte/aterro até a face inferior da laje");infills.Add(infill);}
  doc.Regenerate();foreach(var floor in new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>())if(topo.CanBeExcavatedBy(floor.Id))topo.ExcavateBy(floor.Id);foreach(var infill in infills)JoinGeometryUtils.JoinGeometry(doc,topo,infill);
  var wt=new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().First(t=>t.Name=="ARQ PAR-01 / cerâmico 9 + revestimentos 3+3");
  Wall.Create(doc,Line.CreateBound(P(.9,13),P(10,13)),wt.Id,level.Id,F(1),0,false,false).get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("CT04:arrimo y=13; espessura do tipo de alvenaria — hipótese registrada");
  foreach(var edge in new[]{(A:Boundary[3],B:Boundary[4],H:1.8),(A:Boundary[4],B:Boundary[0],H:1.8),(A:Boundary[0],B:Boundary[1],H:.4),(A:Boundary[1],B:Boundary[2],H:.4),(A:Boundary[2],B:Boundary[3],H:.4)})
  {
   var a=P(edge.A.X,edge.A.Y,Natural(edge.A.X,edge.A.Y));var b=P(edge.B.X,edge.B.Y,Natural(edge.B.X,edge.B.Y));
   var profile=Loop([a,b,b+XYZ.BasisZ*F(edge.H),a+XYZ.BasisZ*F(edge.H)]).ToList();
   Wall.Create(doc,profile,wt.Id,level.Id,false).get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("CT04:divisa; altura conforme ficha, espessura do tipo");
  }
  using(var sub=new SubTransaction(doc))
  {
   sub.Start();try
   {
    var source=new FilteredElementCollector(doc).OfClass(typeof(RailingType)).Cast<RailingType>().First();var railingType=(RailingType)source.Duplicate("CT04 — gradil de frente 1,80 m");railingType.TopRailHeight=F(1.8);
    foreach(var edge in new[]{(Boundary[0],Boundary[1]),(Boundary[1],Boundary[2]),(Boundary[2],Boundary[3])})
    {var rail=Railing.Create(doc,CurveLoop.Create([Line.CreateBound(P(edge.Item1.X,edge.Item1.Y),P(edge.Item2.X,edge.Item2.Y))]),railingType.Id,level.Id);rail.HostId=topo.Id;rail.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("CT04:gradil 1,80 m sobre terreno; mureta 0,40 m");}
    sub.Commit();
   }
   catch(Exception ex){sub.RollBack();File.WriteAllText(Path.Combine(output,"GRADIL_PENDENTE.txt"),ex.Message);}
  }
  if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Terreno não confirmado; modelo incompleto preservado.");
 }
 static IEnumerable<XYZ> CurvePoints(string path)
 {
  var lines=File.ReadAllLines(path);string entity="",layer="";double elevation=0;var points=new List<(double X,double Y)>();double? x=null;
  var result=new List<XYZ>();
  void Flush(){if(entity=="LWPOLYLINE"&&layer.StartsWith("CURVAS",StringComparison.OrdinalIgnoreCase))result.AddRange(points.Where((p,i)=>i%4==0||i==points.Count-1).Select(p=>new XYZ(p.X,p.Y,elevation)));}
  for(int i=0;i+1<lines.Length;i+=2)
  {
   string code=lines[i].Trim(),value=lines[i+1].Trim();
   if(code=="0"){Flush();entity=value;layer="";elevation=0;points.Clear();x=null;}
   else if(code=="8")layer=value;
   else if(code=="38")elevation=double.Parse(value,CultureInfo.InvariantCulture);
   else if(code=="10")x=double.Parse(value,CultureInfo.InvariantCulture);
   else if(code=="20"&&x.HasValue){points.Add((x.Value,double.Parse(value,CultureInfo.InvariantCulture)));x=null;}
  }
  Flush();return result;
 }
 static void Openings(Document doc,List<FamilySymbol> symbols,string output)
 {
  using var tx=new Transaction(doc,"CT04 — portas e janelas");tx.Start();
  var levels=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToDictionary(l=>l.Name);
  var walls=new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
  var used=new List<object>();
  void Place(string id,string level,bool window,double x,double y,double width,double height,double sill=0)
  {
   var l=levels[level];var p=P(x,y,Implantation.Meters(l.Elevation));var w=walls.Where(w=>w.LevelId==l.Id&&w.Location is LocationCurve).OrderBy(w=>((LocationCurve)w.Location).Curve.Distance(p)).First();
   var source=symbols.Where(s=>s.Category?.Id.Value==(long)(window?BuiltInCategory.OST_Windows:BuiltInCategory.OST_Doors)).OrderBy(s=>s.FamilyName.Contains(window?"correr":"abrir",StringComparison.OrdinalIgnoreCase)?0:1).FirstOrDefault()??throw new InvalidOperationException("Família de esquadria ausente no template.");
   var type=(FamilySymbol)source.Duplicate("CT04 "+id+" "+width.ToString(CultureInfo.InvariantCulture)+"x"+height.ToString(CultureInfo.InvariantCulture));
   foreach(var key in new[]{BuiltInParameter.FAMILY_WIDTH_PARAM,BuiltInParameter.DOOR_WIDTH,BuiltInParameter.WINDOW_WIDTH})if(type.get_Parameter(key) is {IsReadOnly:false,StorageType:StorageType.Double} q)q.Set(F(width));
   foreach(var key in new[]{BuiltInParameter.FAMILY_HEIGHT_PARAM,BuiltInParameter.DOOR_HEIGHT,BuiltInParameter.WINDOW_HEIGHT})if(type.get_Parameter(key) is {IsReadOnly:false,StorageType:StorageType.Double} q)q.Set(F(height));
   type.Activate();doc.Regenerate();var projected=((LocationCurve)w.Location).Curve.Project(p).XYZPoint;var inst=doc.Create.NewFamilyInstance(new XYZ(projected.X,projected.Y,l.Elevation),type,w,l,Autodesk.Revit.DB.Structure.StructuralType.NonStructural);inst.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM)?.Set(F(sill));inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).Set(id);used.Add(new{id,Family=source.FamilyName,width,height});
  }
  Place("P01","Térreo",false,2.2,3.075,.9,2.1);Place("P02","Térreo",false,7.4,10.925,.8,2.1);Place("P03","Térreo",false,3.55,9.4,.7,2.1);Place("P04","Térreo",false,4.95,9.4,.7,2.1);Place("P05","Térreo",false,9.425,4,.8,2.1);
  Place("P06","Superior",false,6.1,8.25,.8,2.1);Place("P07","Superior",false,4,8.25,.8,2.1);Place("P08","Superior",false,5.25,9.5,.7,2.1);Place("P09","Superior",false,7.25,9.5,.7,2.1);
  Place("J01a","Térreo",true,4.5,3.075,2,1.2,1);Place("J01b","Térreo",true,7.5,3.075,2,1.2,1);Place("J03","Térreo",true,9.425,9,1.5,1,1.1);Place("J04","Térreo",true,3.5,10.925,.6,.6,1.6);Place("J05","Térreo",true,1.575,8.5,.6,1.5,1.2);
  Place("J06","Superior",true,7.3,4.575,2,1.2,1);Place("J07","Superior",true,9.425,6.4,1.2,1.2,1);Place("J08","Superior",true,4,4.575,1.5,1.2,1);Place("J09","Superior",true,6.2,10.925,.6,.6,1.6);Place("J10","Superior",true,8.3,10.925,.6,.6,1.6);
  tx.Commit();File.WriteAllText(Path.Combine(output,"ESQUADRIAS.json"),JsonSerializer.Serialize(used));
 }
 static void Stairs(Document doc,string output)
 {
  var levels=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToDictionary(l=>l.Name);
  using var scope=new StairsEditScope(doc,"CT04 — escada reta, 17 espelhos");var id=scope.Start(levels["Térreo"].Id,levels["Superior"].Id);
  using(var tx=new Transaction(doc,"CT04 — lance de escada")){tx.Start();var stairs=(Stairs)doc.GetElement(id);stairs.DesiredRisersNumber=17;stairs.ActualTreadDepth=F(.28);
   var run=StairsRun.CreateStraightRun(doc,id,Line.CreateBound(P(2.2,6.37),P(2.2,10.85)),StairsRunJustification.Center);run.ActualRunWidth=F(1.1);tx.Commit();}
  scope.Commit(new StairFailures());
 }
 sealed class StairFailures:IFailuresPreprocessor
 {public FailureProcessingResult PreprocessFailures(FailuresAccessor a)=>a.GetFailureMessages().Any(f=>f.GetSeverity()==FailureSeverity.Error)?FailureProcessingResult.ProceedWithRollBack:FailureProcessingResult.Continue;}
}
