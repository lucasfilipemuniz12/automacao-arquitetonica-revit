using System.IO;
using System.Reflection;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Vebram.Revit;
namespace Vebram.SectionNative04;
public sealed class NativeApp:IExternalApplication
{
 record Job(string Project,string Output);
 string path="";
 public Result OnStartup(UIControlledApplication app){path=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,"section-test-job.json");if(File.Exists(path)){new Vebram.Revit.App().OnStartup(app);app.Idling+=Run;}return Result.Succeeded;}
 public Result OnShutdown(UIControlledApplication app)=>Result.Succeeded;
 void Run(object? sender,IdlingEventArgs args)
 {
  if(sender is not UIApplication ui)return;ui.Idling-=Run;
  if(!File.Exists(path))return;var job=JsonSerializer.Deserialize<Job>(File.ReadAllText(path))!;File.Move(path,path+".executado-"+DateTime.UtcNow.Ticks);
  Document? doc=null;
  try
  {
   doc=ui.Application.OpenDocumentFile(job.Project);
   var physical=Physical(doc);var ids=LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order().ToList();var elevations=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToDictionary(l=>l.Id.Value,l=>l.Elevation);
   var source=Implantation.ModelPlans(doc).First(v=>v.Name=="PRE - Layout / Nível 1");var floor=Implantation.Footprints(doc,source).First();
   var inventory=LegalCatalog.Items.Where(i=>i.Key.StartsWith("corte-")).Select(i=>LegalCatalog.Reference(LegalCatalog.Views(doc),i)).OfType<ViewSection>().Select(v=>new{v.Name,Id=v.Id.Value,Origin=new[]{v.Origin.X,v.Origin.Y,v.Origin.Z},Direction=new[]{v.ViewDirection.X,v.ViewDirection.Y,v.ViewDirection.Z},Annotations=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==v.Id).Select(e=>new{e.Id.Value,Type=e.GetType().Name})});
   File.WriteAllText(Path.Combine(job.Output,"INVENTARIO_CORTES.json"),JsonSerializer.Serialize(inventory,new JsonSerializerOptions{WriteIndented=true}));
   var results=new List<ModuleResult>();
   foreach(var key in new[]{"corte-a","corte-b"})
   {
    var input=new SectionSelection(key,source.Id.Value,floor.Id.Value,key=="corte-a",50,false,15);
    File.WriteAllText(Path.Combine(job.Output,"PASSO.txt"),key);var first=LegalSections.Apply(doc,input);var count=Count(doc,first.ViewId);var second=LegalSections.Apply(doc,input);
    if(first.ViewId!=second.ViewId||count!=Count(doc,second.ViewId))throw new Exception("Corte duplicado ou quantidade instável: "+key);
    var view=(ViewSection)doc.GetElement(new ElementId(second.ViewId));var desired=key=="corte-a"?XYZ.BasisY:XYZ.BasisX;
    if(view.ViewDirection.DotProduct(desired)<.999||Math.Abs(view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).AsDouble()-Implantation.Feet(15))>.0001)throw new Exception("Direção/profundidade divergentes: "+key);
    var marks=new FilteredElementCollector(doc).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>n.OwnerViewId==view.Id&&DrawingOwnership.IsAnnotation(n)).ToList();
    if(marks.Count<2)throw new Exception("Níveis não foram identificados: "+key);
    results.Add(second);
   }
   var a=(ViewSection)doc.GetElement(new ElementId(results[0].ViewId));var note=new FilteredElementCollector(doc).OfClass(typeof(TextNote)).Cast<TextNote>().First(n=>n.OwnerViewId==a.Id&&DrawingOwnership.IsAnnotation(n));
   using(var tx=new Transaction(doc,"Teste de edição do corte")){tx.Start();note.Text+=" — edição de teste";tx.Commit();}
   bool protectedEdit=false;try{LegalSections.Apply(doc,new("corte-a",source.Id.Value,floor.Id.Value));}catch(InvalidOperationException){protectedEdit=true;}
   if(!protectedEdit)throw new Exception("Edição não protegida.");
   var shifted=LegalSections.Apply(doc,new("corte-a",source.Id.Value,floor.Id.Value,true,25,true,8,true));
   if(a.ViewDirection.DotProduct(-XYZ.BasisY)<.999||Math.Abs(a.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).AsDouble()-Implantation.Feet(8))>.0001)throw new Exception("Inversão/profundidade não aplicadas.");
   var contour=Implantation.GetContour(doc,floor);var expectedY=contour.Min(p=>p.Y)+(contour.Max(p=>p.Y)-contour.Min(p=>p.Y))*.25;
   if(Math.Abs(a.Origin.Y-expectedY)>.01)throw new Exception("Posição do corte não corresponde a 25%.");
   results[0]=LegalSections.Apply(doc,new("corte-a",source.Id.Value,floor.Id.Value,true,50,false,15));
   if(!physical.SequenceEqual(Physical(doc))||!ids.SequenceEqual(LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order())||elevations.Any(p=>((Level)doc.GetElement(new ElementId(p.Key))).Elevation!=p.Value))throw new Exception("Geometria, níveis ou identidade das vistas alterados.");
   var state=new{Version="0.4.2",Results=results,Repeat=true,EditedAnnotationsProtected=protectedEdit,Reverse=true,Position25Percent=true,Depth8And15=true,PhysicalBoundsPreserved=true,LevelsPreserved=true,ViewsPreserved=true};
   File.WriteAllText(Path.Combine(job.Output,"TESTES_CORTES.json"),JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true}));doc.Save();doc.Close(false);doc=null;
   File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Cortes A/B preparados e repetidos; posição, sentido, profundidade, níveis e proteção de edição verificados.\n"+job.Project);
  }
  catch(Exception ex){File.WriteAllText(Path.Combine(job.Output,"ERRO.txt"),ex.ToString());}
  finally{if(doc?.IsValidObject==true)doc.Close(false);}
 }
 static int Count(Document doc,long view)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Count(e=>e.OwnerViewId.Value==view);
 static List<string> Physical(Document doc)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e is HostObject or FamilyInstance or DirectShape).Select(e=>e.Id.Value+"/"+(e.get_BoundingBox(null) is {} b?string.Join("/",new[]{b.Min.X,b.Min.Y,b.Min.Z,b.Max.X,b.Max.Y,b.Max.Z}.Select(v=>Math.Round(v,6))):"none")).Order().ToList();
}

