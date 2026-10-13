using System.Reflection;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.DB.Structure;
namespace Vebram.HouseBasic;
public sealed class HouseApp:IExternalApplication
{
 record Job(string Title,string Output,string? Project=null);
 string path="";
 public Result OnStartup(UIControlledApplication a){path=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,"house-job.json");if(File.Exists(path))a.Idling+=Run;return Result.Succeeded;}
 public Result OnShutdown(UIControlledApplication a)=>Result.Succeeded;
 static double F(double m)=>m/.3048;
 static XYZ P(double x,double y,double z=0)=>new(F(x),F(y),F(z));
 static CurveLoop Rect(double x,double y,double w,double h){var pts=new[]{P(x,y),P(x+w,y),P(x+w,y+h),P(x,y+h)};var l=new CurveLoop();for(int i=0;i<4;i++)l.Append(Line.CreateBound(pts[i],pts[(i+1)%4]));return l;}
 void Run(object? sender,IdlingEventArgs a)
 {
  if(sender is not UIApplication ui)return;
  if(!File.Exists(path)){ui.Idling-=Run;return;}
  var job=JsonSerializer.Deserialize<Job>(File.ReadAllText(path))!;
  if(ui.ActiveUIDocument==null){if(job.Project==null)return;ui.OpenAndActivateDocument(job.Project);return;}
  var doc=ui.ActiveUIDocument.Document;if(doc.Title!=job.Title)return;
  ui.Idling-=Run;File.Move(path,path+".executado",false);Directory.CreateDirectory(job.Output);
  try
  {
   if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new Exception("Usar o projeto RVT, não editar o template.");
   var existing=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e is Wall||e is Floor||e is RoofBase||e is Ceiling||e is Toposolid).ToList();
   if(existing.Count>0)throw new Exception("O projeto já contém elementos físicos. Não sobrepor uma nova casa automaticamente.");
   var plans=new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(v=>!v.IsTemplate).ToList();
   var plan=plans.First(v=>v.Name=="PRE - Layout / Nível 1");var level=plan.GenLevel;
   var level2=plans.First(v=>v.Name=="PRE - Layout / Nível 2").GenLevel;
   if(Math.Abs(level.Elevation)>F(.001)||Math.Abs(level2.Elevation-F(3))>F(.001))throw new Exception("Este estudo espera níveis 0 e 3 m do V02.");
   var wt=new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Where(t=>t.Kind==WallKind.Basic).OrderBy(t=>t.Name.Contains("Externa",StringComparison.OrdinalIgnoreCase)?0:1).ThenBy(t=>Math.Abs(t.Width-F(.2))).First();
   var floorTypes=new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().ToList();
   var ft=floorTypes.OrderBy(t=>t.Name.Contains("Laje",StringComparison.OrdinalIgnoreCase)?0:1).First();
   var ct=new FilteredElementCollector(doc).OfClass(typeof(CeilingType)).Cast<CeilingType>().OrderBy(t=>t.Name.Contains("Gesso",StringComparison.OrdinalIgnoreCase)?0:1).First();
   var rt=new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().OrderBy(t=>t.Name.Contains("Telha",StringComparison.OrdinalIgnoreCase)?0:1).First();
   var terrainType=new FilteredElementCollector(doc).OfClass(typeof(ToposolidType)).Cast<ToposolidType>().First();
   FamilySymbol Symbol(BuiltInCategory cat)=>new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(cat).Cast<FamilySymbol>().OrderBy(s=>s.Name.Contains("90",StringComparison.OrdinalIgnoreCase)?0:1).First();
   var door=Symbol(BuiltInCategory.OST_Doors);var window=Symbol(BuiltInCategory.OST_Windows);
   File.WriteAllText(Path.Combine(job.Output,"TIPOS.json"),JsonSerializer.Serialize(new{Wall=wt.Name,Floor=ft.Name,Ceiling=ct.Name,Roof=rt.Name,Door=door.FamilyName+" / "+door.Name,Window=window.FamilyName+" / "+window.Name}));
   var made=new List<Element>();using var tx=new Transaction(doc,"Casa básica 10 × 15 m — pré-projeto");tx.Start();
   var pts=new[]{P(0,0),P(10,0),P(10,15),P(0,15)};var walls=new List<Wall>();
   for(int i=0;i<4;i++){var wall=Wall.Create(doc,Line.CreateBound(pts[i],pts[(i+1)%4]),wt.Id,level.Id,F(3),0,false,false);walls.Add(wall);made.Add(wall);}
   made.Add(Floor.Create(doc,new List<CurveLoop>{Rect(0,0,10,15)},ft.Id,level.Id));
   made.Add(Floor.Create(doc,new List<CurveLoop>{Rect(0,0,10,15)},ft.Id,level2.Id));
   var ceiling=Ceiling.Create(doc,new List<CurveLoop>{Rect(.2,.2,9.6,14.6)},ct.Id,level.Id);ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).Set(F(2.7));made.Add(ceiling);
   var terrain=Toposolid.Create(doc,new List<CurveLoop>{Rect(-5,-5,20,25)},terrainType.Id,level.Id);ElementTransformUtils.MoveElement(doc,terrain.Id,P(0,0,-.3));made.Add(terrain);
   var outline=new CurveArray();foreach(var c in Rect(-.4,-.4,10.8,15.8))outline.Append(c);
   ModelCurveArray roofEdges=new ModelCurveArray();var roof=doc.Create.NewFootPrintRoof(outline,level2,rt,out roofEdges);
   foreach(ModelCurve edge in roofEdges){bool side=Math.Abs(edge.GeometryCurve.GetEndPoint(0).X-edge.GeometryCurve.GetEndPoint(1).X)<.001;roof.set_DefinesSlope(edge,side);if(side)roof.set_SlopeAngle(edge,Math.Atan(.2));}made.Add(roof);
   if(!door.IsActive)door.Activate();if(!window.IsActive)window.Activate();doc.Regenerate();
   made.Add(doc.Create.NewFamilyInstance(P(5,0),door,walls[0],level,StructuralType.NonStructural));
   foreach(var item in new[]{(P(2,0,1),walls[0]),(P(8,0,1),walls[0]),(P(10,7.5,1),walls[1]),(P(5,15,1),walls[2])})
   {var instance=doc.Create.NewFamilyInstance(item.Item1,window,item.Item2,level,StructuralType.NonStructural);var sill=instance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);if(sill is {IsReadOnly:false})sill.Set(F(1));made.Add(instance);}
   var phase=plan.get_Parameter(BuiltInParameter.VIEW_PHASE)?.AsElementId();foreach(var el in made){var p=el.get_Parameter(BuiltInParameter.PHASE_CREATED);if(phase!=null&&p is {IsReadOnly:false})p.Set(phase);}
   foreach(var view in plans.Where(v=>v.Name is "PRE - Layout / Nível 1" or "PRE - Layout / Nível 2")){var b=view.CropBox;b.Min=new XYZ(F(-7),F(-7),b.Min.Z);b.Max=new XYZ(F(17),F(22),b.Max.Z);view.CropBox=b;}
   if(tx.Commit()!=TransactionStatus.Committed)throw new Exception("Transação revertida.");
   var dest=Path.Combine(job.Output,"Casa_Basica_10x15.rvt");if(File.Exists(dest))throw new Exception("Arquivo de saída já existe; não substituir.");
   doc.SaveAs(dest,new SaveAsOptions{OverwriteExistingFile=false,MaximumBackups=1});ui.ActiveUIDocument.RequestViewChange(plan);
   File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Casa básica criada no projeto solicitado.\n"+dest+"\nCasa: 10 × 15 m (eixos das paredes); terreno: 20 × 25 m; paredes: 3 m; forro: 2,70 m; cobertura duas águas: 20%.\n"+string.Join("\n",made.Select(el=>el.Id.Value+" / "+el.Name)));
  }
  catch(Exception ex){File.WriteAllText(Path.Combine(job.Output,"ERRO.txt"),ex.ToString());}
 }
}


