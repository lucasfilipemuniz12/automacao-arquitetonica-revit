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
   var generated=new FilteredElementCollector(doc).WhereElementIsNotElementType().OfClass(typeof(TextNote)).Cast<TextNote>().First(t=>t.OwnerViewId==pview.Id&&DrawingOwnership.IsAnnotation(t));
   using(var t=new Transaction(doc,"Teste de proteção de edição")){t.Start();generated.Text+=" — edição de teste";t.Commit();}
   bool protectedEdit=false;try{LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));}catch(InvalidOperationException){protectedEdit=true;}
   if(!protectedEdit)throw new Exception("Edição manual não protegida.");LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value,true));
   DimensionType custom;
   using(var t=new Transaction(doc,"Teste de preferências")){t.Start();custom=(DimensionType)LegalPreferenceStore.Dimension(doc).Duplicate("VEBRAM TESTE — estilo escolhido");LegalPreferenceStore.Save(doc,new(custom.UniqueId,100,"Informação adicional de teste"));t.Commit();}
   var withStyle=LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));
   if(new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>().Where(d=>d.OwnerViewId==pview.Id&&DrawingOwnership.IsAnnotation(d)).Any(d=>d.GetTypeId()!=custom.Id))throw new Exception("Estilo escolhido não aplicado.");
   using(var t=new Transaction(doc,"Concluir preferências do piloto")){t.Start();LegalPreferenceStore.Save(doc,new(null,100,"ESTUDO FICTÍCIO — REVISAR ANTES DE EMITIR"));t.Commit();}
   results[0]=LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));results[1]=LegalModules.Apply(doc,new("cobertura",source.Id.Value,floor.Id.Value));
   var simple=ui.Application.Documents.Cast<Document>().FirstOrDefault(d=>d.PathName.Equals(job.SimpleProject,StringComparison.OrdinalIgnoreCase));bool closeSimple=false;
   if(simple==null){simple=ui.Application.OpenDocumentFile(job.SimpleProject);closeSimple=true;}
   var simpleWaters=RoofWaters.Read(simple,new FilteredElementCollector(simple).OfClass(typeof(RoofBase)).Cast<Element>().ToList());
   if(simpleWaters.Count!=2||simpleWaters.Any(w=>Math.Abs(w.Slope-20)>.01))throw new Exception("Leitura das duas águas da casa básica divergiu de 20%.");
   if(closeSimple)simple.Close(false);
   if(!physical.SequenceEqual(Physical(doc))||!views.SequenceEqual(LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order()))throw new Exception("Elementos físicos ou identidade das vistas foram alterados.");
   var report=new{Results=results,Repeat=true,EditedMarksProtected=protectedEdit,ChosenStyle=true,PhysicalPreserved=true,ViewsPreserved=true,SimpleRoofFaces=simpleWaters.Select(w=>new{w.Slope,Direction=w.Direction==null?null:new[]{w.Direction.X,w.Direction.Y,w.Direction.Z}})};
   File.WriteAllText(Path.Combine(job.Output,"TESTES_NATIVOS.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
   doc.Save();doc.Close(false);closeDoc=false;
   File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Módulos 0.3 executados e repetidos na Casa Terras; preferências e proteção de edição verificadas; casa básica com duas águas de 20% reconhecida.\n"+job.Project);
  }
  catch(Exception ex){File.WriteAllText(Path.Combine(job!.Output,"ERRO.txt"),ex.ToString());}
  finally{if(closeDoc&&doc?.IsValidObject==true)doc.Close(false);}
 }
 static List<long> Physical(Document doc)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e is HostObject||e is FamilyInstance||e is DirectShape||e is Autodesk.Revit.DB.Architecture.Room).Select(e=>e.Id.Value).Order().ToList();
 static int Annotations(Document doc,long id)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Count(e=>e.OwnerViewId.Value==id);
}


