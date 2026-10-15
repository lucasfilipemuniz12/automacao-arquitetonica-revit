using System.Reflection;
using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Vebram.Revit;
namespace Vebram.LegalNative03;
public sealed class NativeApp:IExternalApplication
{
 record Job(string Project,string Output,string SimpleProject);
 string path="";Job? job;
 public Result OnStartup(UIControlledApplication app){path=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,"legal-test-job.json");if(File.Exists(path)){new Vebram.Revit.App().OnStartup(app);app.Idling+=Run;}return Result.Succeeded;}
 public Result OnShutdown(UIControlledApplication app)=>Result.Succeeded;
 void Run(object? sender,IdlingEventArgs e)
 {
  if(sender is not UIApplication ui)return;
  ui.Idling-=Run;if(!File.Exists(path))return;job=JsonSerializer.Deserialize<Job>(File.ReadAllText(path))!;File.Move(path,path+".executado-"+DateTime.UtcNow.Ticks,false);
  Document? doc=null;bool closeDoc=false;
  try
  {
   doc=ui.Application.Documents.Cast<Document>().FirstOrDefault(d=>d.PathName.Equals(job.Project,StringComparison.OrdinalIgnoreCase));
   if(doc!=null&&ui.ActiveUIDocument?.Document==doc)throw new InvalidOperationException("A cópia de teste está ativa. Ative outro projeto antes de executar.");
   doc??=ui.Application.OpenDocumentFile(job.Project);closeDoc=true;
   var physical=Physical(doc);var views=LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order().ToList();
   var source=Implantation.ModelPlans(doc).First(v=>v.Name=="PRE - Layout / Nível 1");var floor=Implantation.Footprints(doc,source).First();
   var results=new List<ModuleResult>();
   foreach(var key in new[]{"planta","cobertura","areas"})
   {
    var input=new ModuleSelection(key,source.Id.Value,floor.Id.Value);var first=LegalModules.Apply(doc,input);var count=Annotations(doc,first.ViewId);var second=LegalModules.Apply(doc,input);
    if(first.ViewId!=second.ViewId||count!=Annotations(doc,second.ViewId))throw new Exception("Falha de repetição: "+key);
    results.Add(second);
   }
   var pview=(View)doc.GetElement(new ElementId(results[0].ViewId));
   var planDimensions=new FilteredElementCollector(doc,pview.Id).OfClass(typeof(Dimension)).Cast<Dimension>().Where(DrawingOwnership.IsAnnotation).ToList();
   var realDimensions=planDimensions.Where(d=>d.References.Cast<Reference>().All(r=>doc.GetElement(r.ElementId) is Wall)).ToList();
   if(realDimensions.Count==0||realDimensions.Any(d=>d.Value is not double value||value<=0))throw new Exception("Não foram criadas cotas internas válidas entre paredes.");
   var labels=new FilteredElementCollector(doc,pview.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(DrawingOwnership.IsAnnotation).ToList();
   if(labels.Any(n=>n.LeaderCount>0))throw new Exception("Rótulos gerados ainda possuem chamadas externas.");
   var labelsNoLeaders=labels.Where(n=>n.LeaderCount==0&&n.Text.Contains("m²")).ToList();
   bool labelsOverlap=false;for(int i=0;i<labelsNoLeaders.Count;i++)for(int j=i+1;j<labelsNoLeaders.Count;j++)
   {var a=labelsNoLeaders[i];var b=labelsNoLeaders[j];if(Math.Abs(a.Coord.X-b.Coord.X)<(a.Width+b.Width)*pview.Scale/2&&Math.Abs(a.Coord.Y-b.Coord.Y)<(a.Height+b.Height)*pview.Scale/2)labelsOverlap=true;}
   File.WriteAllText(Path.Combine(job.Output,"DIAGNOSTICO_ROTULOS.json"),JsonSerializer.Serialize(labels.Select(n=>new{n.Text,n.Coord.X,n.Coord.Y,n.Width,n.Height,Box=n.get_BoundingBox(pview) is {} b?new[]{b.Min.X,b.Min.Y,b.Max.X,b.Max.Y}:null}),new JsonSerializerOptions{WriteIndented=true}));
   if(labelsOverlap)throw new Exception("Há sobreposição entre rótulos internos gerados.");
   File.WriteAllText(Path.Combine(job.Output,"ROTULOS_E_COTAS.json"),JsonSerializer.Serialize(new{InternalWallDimensions=realDimensions.Select(d=>new{Id=d.Id.Value,Meters=d.Value*.3048,References=d.References.Cast<Reference>().Select(r=>r.ElementId.Value)}),Labels=labels.Select(n=>new{n.Text,n.LeaderCount}),InteriorLabelsOverlap=labelsOverlap},new JsonSerializerOptions{WriteIndented=true}));
   var generated=labels.First();
   using(var t=new Transaction(doc,"Teste de proteção de edição")){t.Start();generated.Text+=" — edição de teste";t.Commit();}
   bool protectedEdit=false;try{LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));}catch(InvalidOperationException){protectedEdit=true;}
   if(!protectedEdit)throw new Exception("Edição manual não protegida.");LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value,true));
   DimensionType custom;
   using(var t=new Transaction(doc,"Teste de preferências")){t.Start();custom=new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>().FirstOrDefault(d=>d.Name=="VEBRAM TESTE — estilo escolhido")??(DimensionType)LegalPreferenceStore.Dimension(doc).Duplicate("VEBRAM TESTE — estilo escolhido");LegalPreferenceStore.Save(doc,new(custom.UniqueId,100,"Informação adicional de teste"));t.Commit();}
   var withStyle=LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));
   if(new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>().Where(d=>d.OwnerViewId==pview.Id&&DrawingOwnership.IsAnnotation(d)).Any(d=>d.GetTypeId()!=custom.Id))throw new Exception("Estilo escolhido não aplicado.");
   using(var t=new Transaction(doc,"Teste de opções de cotagem")){t.Start();LegalPreferenceStore.Save(doc,new(null,100,"ESTUDO FICTÍCIO — REVISAR ANTES DE EMITIR",true,true,true));t.Commit();}
   var optional=LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));
   File.WriteAllText(Path.Combine(job.Output,"COTAGEM_OPCIONAL.txt"),optional.Report);
   using(var t=new Transaction(doc,"Concluir preferências do piloto")){t.Start();LegalPreferenceStore.Save(doc,new(null,100,"ESTUDO FICTÍCIO — REVISAR ANTES DE EMITIR",true,false,false));t.Commit();}
   results[0]=LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));results[1]=LegalModules.Apply(doc,new("cobertura",source.Id.Value,floor.Id.Value));
   var simple=ui.Application.Documents.Cast<Document>().FirstOrDefault(d=>d.PathName.Equals(job.SimpleProject,StringComparison.OrdinalIgnoreCase));bool closeSimple=false;
   if(simple==null){simple=ui.Application.OpenDocumentFile(job.SimpleProject);closeSimple=true;}
   var simpleRoofs=new FilteredElementCollector(simple).OfClass(typeof(RoofBase)).Cast<Element>().ToList();
   var simpleWaters=RoofWaters.Read(simple,simpleRoofs);
   var expectedSlopes=new List<double>();
   foreach(var roof in simpleRoofs.OfType<FootPrintRoof>())foreach(ModelCurveArray loop in roof.GetProfiles())foreach(ModelCurve curve in loop)
    if(roof.get_DefinesSlope(curve))expectedSlopes.Add(roof.get_SlopeAngle(curve)*100);
   File.WriteAllText(Path.Combine(job.Output,"DIAGNOSTICO_TELHADO.json"),JsonSerializer.Serialize(new{ExpectedFromSketch=expectedSlopes,ReadFromFaces=simpleWaters.Select(w=>new{w.Slope,w.RoofName})},new JsonSerializerOptions{WriteIndented=true}));
   if(simpleWaters.Count!=2||expectedSlopes.Count!=2||!simpleWaters.Select(w=>w.Slope).Order().Zip(expectedSlopes.Order()).All(pair=>Math.Abs(pair.First-pair.Second)<.01))throw new Exception("Leitura das águas divergiu das inclinações gravadas no contorno do telhado. Consulte DIAGNOSTICO_TELHADO.json.");
   if(closeSimple)simple.Close(false);
   if(!physical.SequenceEqual(Physical(doc))||!views.SequenceEqual(LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order()))throw new Exception("Elementos físicos ou identidade das vistas foram alterados.");
   var report=new{Version="0.3.5",Results=results,Repeat=true,EditedMarksProtected=protectedEdit,ChosenStyle=true,PhysicalPreserved=true,ViewsPreserved=true,SimpleRoofSketchSlopes=expectedSlopes,SimpleRoofFaces=simpleWaters.Select(w=>new{w.Slope,Direction=w.Direction==null?null:new[]{w.Direction.X,w.Direction.Y,w.Direction.Z}})};
   File.WriteAllText(Path.Combine(job.Output,"TESTES_NATIVOS.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
   doc.Save();doc.Close(false);closeDoc=false;
   File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Módulos 0.3.3 executados e repetidos na Casa Terras; preferências e proteção de edição verificadas; duas águas da casa básica conferidas contra as inclinações do contorno nativo.\n"+job.Project);
  }
  catch(Exception ex){File.WriteAllText(Path.Combine(job!.Output,"ERRO.txt"),ex.ToString());}
  finally{if(closeDoc&&doc?.IsValidObject==true)doc.Close(false);}
 }
 static List<long> Physical(Document doc)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e is HostObject||e is FamilyInstance||e is DirectShape||e is Autodesk.Revit.DB.Architecture.Room).Select(e=>e.Id.Value).Order().ToList();
 static int Annotations(Document doc,long id)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Count(e=>e.OwnerViewId.Value==id);
}







