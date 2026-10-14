using System.IO;
using System.Reflection;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Vebram.Revit;
namespace Vebram.Revit;
internal sealed class SectionValidation:IExternalApplication
{
 record Job(string Project,string Output,string Mode="sections");
 string path="";
 public Result OnStartup(UIControlledApplication app){path=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,"section-validation-job.json");if(File.Exists(path)){app.Idling+=Run;}return Result.Succeeded;}
 public Result OnShutdown(UIControlledApplication app)=>Result.Succeeded;
 void Run(object? sender,IdlingEventArgs args)
 {
  if(sender is not UIApplication ui)return;ui.Idling-=Run;
  if(!File.Exists(path))return;var job=JsonSerializer.Deserialize<Job>(File.ReadAllText(path))!;File.Move(path,path+".executado-"+DateTime.UtcNow.Ticks);
  Document? doc=null;
  try
  {
   doc=ui.Application.OpenDocumentFile(job.Project);
   if(job.Mode=="facades055"){ValidateFacades055(doc,job.Output);doc.Save();doc.Close(false);doc=null;File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Fachadas e linhas de situação validadas na cópia de teste.");return;}
   if(job.Mode=="legal05"){ValidateLegal05(doc,job.Output);doc.Save();doc.Close(false);doc=null;File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Módulos legais 0.5 verificados na cópia de teste.\n"+job.Project);return;}
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
    if(marks.Any(n=>n.Text.Contains("relativo ao nível fonte")))throw new Exception("Texto duplicado de nível ainda presente.");
    var contourBounds=Implantation.GetContour(doc,floor);var inverse=view.CropBox.Transform.Inverse;
    double buildingLeft=contourBounds.Min(p=>inverse.OfPoint(p).X);
    var levels=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Where(l=>l.Elevation>=source.GenLevel.Elevation-.1&&l.Elevation<=source.GenLevel.Elevation+Implantation.Feet(4)).ToList();
    foreach(var level in levels)
    {
     var curve=level.GetCurvesInView(DatumExtentType.ViewSpecific,view).OfType<Line>().FirstOrDefault()??throw new Exception("Linha de nível ausente.");
     if(new[]{curve.GetEndPoint(0),curve.GetEndPoint(1)}.Any(p=>inverse.OfPoint(p).X>=buildingLeft-Implantation.Feet(1)))throw new Exception("Linha de nível atravessa a edificação.");
     int bubbles=(level.IsBubbleVisibleInView(DatumEnds.End0,view)?1:0)+(level.IsBubbleVisibleInView(DatumEnds.End1,view)?1:0);
     if(bubbles!=1)throw new Exception("Símbolo de nível duplicado ou ausente.");
    }
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
   var state=new{Version="0.4.4",Results=results,NativeLevelsOutsideBuilding=true,OneBubblePerLevel=true,DuplicateLevelTextRemoved=true,Repeat=true,EditedAnnotationsProtected=protectedEdit,Reverse=true,Position25Percent=true,Depth8And15=true,PhysicalBoundsPreserved=true,LevelsPreserved=true,ViewsPreserved=true};
   File.WriteAllText(Path.Combine(job.Output,"TESTES_CORTES.json"),JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true}));doc.Save();doc.Close(false);doc=null;
   File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Cortes A/B preparados e repetidos; posição, sentido, profundidade, níveis e proteção de edição verificados.\n"+job.Project);
  }
  catch(Exception ex){File.WriteAllText(Path.Combine(job.Output,"ERRO.txt"),ex.ToString());}
  finally{if(doc?.IsValidObject==true)doc.Close(false);}
 }
 static void ValidateFacades055(Document doc,string output)
 {
  var before=Physical(doc);var source=Implantation.ModelPlans(doc).First(v=>v.Name=="PRE - Layout / Nível 1");var floor=Implantation.Footprints(doc,source).First();
  var results=LegalFacades.Apply(doc,new(source.Id.Value,floor.Id.Value));WriteFacadeGeometry(doc,results,output);foreach(var r in results)if(new FilteredElementCollector(doc,new ElementId(r.ViewId)).OfClass(typeof(Wall)).GetElementCount()<1)throw new Exception("Fachada sem paredes: "+r.ViewId);var counts=results.Select(r=>Count(doc,r.ViewId)).ToList();var again=LegalFacades.Apply(doc,new(source.Id.Value,floor.Id.Value));if(!counts.SequenceEqual(again.Select(r=>Count(doc,r.ViewId))))throw new Exception("Fachadas instáveis ao repetir.");
  foreach(var result in again){var view=(ViewSection)doc.GetElement(new ElementId(result.ViewId));if(view.ViewType!=ViewType.Elevation)throw new Exception("Fachada sem elevação nativa.");var direction=view.Name.EndsWith("Norte")?XYZ.BasisY:view.Name.EndsWith("Sul")?-XYZ.BasisY:view.Name.EndsWith("Leste")?XYZ.BasisX:-XYZ.BasisX;if(view.ViewDirection.DotProduct(direction)<.999)throw new Exception("Direção incorreta.");}
  var context=LegalContext.Saved(doc,"situacao")??new ContextSelection("situacao",source.Id.Value,"Lote de estudo",Confirmed:true);var situation=LegalContext.Apply(doc,context with{Confirmed=true});var site=(View)doc.GetElement(new ElementId(situation.ViewId));var lines=doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);if(site.GetCategoryHidden(lines.Id))throw new Exception("Categoria de linhas oculta.");
  var curves=new FilteredElementCollector(doc,site.Id).OfClass(typeof(CurveElement)).Cast<CurveElement>().Where(c=>c.OwnerViewId==site.Id&&DrawingOwnership.IsAnnotation(c)).ToList();if(curves.Count<7||curves.Any(c=>site.GetCategoryHidden(c.Category.Id)))throw new Exception("Linhas do diagrama ausentes ou ocultas.");
  if(!before.SequenceEqual(Physical(doc)))throw new Exception("Modelagem física alterada.");File.WriteAllText(Path.Combine(output,"TESTES_FACHADAS_SITUACAO.json"),JsonSerializer.Serialize(new{Version=Assembly.GetExecutingAssembly().GetName().Version?.ToString(),Facades=again,Situation=situation,Repeated=true,NativeDirections=true,SituationLinesVisible=true,CurveCount=curves.Count,PhysicalBoundsPreserved=true},new JsonSerializerOptions{WriteIndented=true}));
 }
 static void WriteFacadeGeometry(Document doc,IReadOnlyList<ModuleResult> results,string output)
 {
  double[] P(XYZ p)=>new[]{p.X,p.Y,p.Z};
  var data=results.Select(r=>{var v=(ViewSection)doc.GetElement(new ElementId(r.ViewId));var box=v.CropBox;var inv=box.Transform.Inverse;var physical=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e is Wall or RoofBase or Floor).Select(e=>new{Id=e.Id.Value,Type=e.GetType().Name,Name=e.Name,Hidden=e.IsHidden(v),Box=e.get_BoundingBox(null)}).Where(e=>e.Box!=null).Select(e=>new{e.Id,e.Type,e.Name,e.Hidden,Min=P(inv.OfPoint(e.Box!.Min)),Max=P(inv.OfPoint(e.Box.Max))});return new{v.Name,Origin=P(v.Origin),Direction=P(v.ViewDirection),CropOrigin=P(box.Transform.Origin),CropMin=P(box.Min),CropMax=P(box.Max),WallCategoryHidden=v.GetCategoryHidden(new ElementId(BuiltInCategory.OST_Walls)),VisibleWalls=new FilteredElementCollector(doc,v.Id).OfClass(typeof(Wall)).GetElementCount(),Phase=v.get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId().Value,PhaseFilter=v.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER).AsElementId().Value,Physical=physical.ToList()};});
  File.WriteAllText(Path.Combine(output,"GEOMETRIA_FACHADAS.json"),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
 }
 static void ValidateLegal05(Document doc,string output)
 {
  var physical=Physical(doc);var views=LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order().ToList();var source=Implantation.ModelPlans(doc).First(v=>v.Name=="PRE - Layout / Nível 1");var floor=Implantation.Footprints(doc,source).First();
  var inventory=new FilteredElementCollector(doc).OfClass(typeof(ElevationMarker)).Cast<ElevationMarker>().Select(m=>new{Id=m.Id.Value,m.CurrentViewCount,Views=Enumerable.Range(0,4).Select(i=>m.GetViewId(i).Value).ToList()});File.WriteAllText(Path.Combine(output,"MARCADORES.json"),JsonSerializer.Serialize(inventory,new JsonSerializerOptions{WriteIndented=true}));
  var results=new List<ModuleResult>();var situation=new ContextSelection("situacao",source.Id.Value,"Lote de estudo — Casa Terras","Lote vizinho fictício A","Lote vizinho fictício B","Lote vizinho fictício C",Confirmed:true);
  var urbanistic=new ContextSelection("urbanistico",source.Id.Value,"ESTUDO FICTÍCIO",ComputableArea:195,PermeableArea:50,MaximumOccupation:70,MaximumCoefficient:1.5,MinimumPermeability:20,Confirmed:true);
  foreach(var input in new[]{situation,urbanistic}){var first=LegalContext.Apply(doc,input);int count=Count(doc,first.ViewId);var second=LegalContext.Apply(doc,input);if(first.ViewId!=second.ViewId||count!=Count(doc,second.ViewId))throw new Exception("Repetição instável: "+input.Key);results.Add(second);}
  var view=(View)doc.GetElement(new ElementId(results[1].ViewId));var note=new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>().First(n=>DrawingOwnership.IsAnnotation(n));using(var tx=new Transaction(doc,"Teste de proteção do quadro")){tx.Start();note.Text+=" edição";tx.Commit();}bool protectedEdit=false;try{LegalContext.Apply(doc,urbanistic);}catch(InvalidOperationException){protectedEdit=true;}if(!protectedEdit)throw new Exception("Edição do quadro não protegida.");results[1]=LegalContext.Apply(doc,urbanistic with{ReplaceEdited=true});
  var facadeInput=new FacadeSelection(source.Id.Value,floor.Id.Value);var facades=LegalFacades.Apply(doc,facadeInput);var countFacades=facades.Select(r=>Count(doc,r.ViewId)).ToList();var repeat=LegalFacades.Apply(doc,facadeInput);if(!countFacades.SequenceEqual(repeat.Select(r=>Count(doc,r.ViewId))))throw new Exception("Repetição instável nas fachadas.");results.AddRange(repeat);
  foreach(var result in repeat){var elevation=(ViewSection)doc.GetElement(new ElementId(result.ViewId));if(elevation.ViewType!=ViewType.Elevation)throw new Exception("Fachada não é elevação nativa.");var expected=elevation.Name.EndsWith("Norte")?XYZ.BasisY:elevation.Name.EndsWith("Sul")?-XYZ.BasisY:elevation.Name.EndsWith("Leste")?XYZ.BasisX:-XYZ.BasisX;if(elevation.ViewDirection.DotProduct(expected)<.999)throw new Exception("Direção cardinal divergente.");}
  var sheets=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s=>s.SheetNumber is "LEG-01" or "LEG-02").OrderBy(s=>s.SheetNumber).ToList();if(sheets.Count!=2)throw new Exception("Folhas legais ausentes.");
  var sheetResults=new List<ModuleResult>();var placements=new List<SheetSelection>();
  foreach(var result in results)
  {
   bool placed=false;foreach(var sheet in sheets){try{var input=new SheetSelection(sheet.Id.Value,result.ViewId);var first=LegalSheets.Apply(doc,input);int count=Count(doc,sheet.Id.Value);var second=LegalSheets.Apply(doc,input);if(count!=Count(doc,sheet.Id.Value)||second.Annotations!=0)throw new Exception("Colocação duplicada.");sheetResults.Add(first);placements.Add(input);placed=true;break;}catch(InvalidOperationException){}}
   if(!placed)throw new Exception("Entregável sem espaço nas duas folhas: "+result.ViewId);
  }
  foreach(var sheet in sheets){var elements=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().Where(e=>e is Viewport or ScheduleSheetInstance).ToList();var rectangles=elements.Select(e=>LegalSheets.Rect(e,sheet)!.Value).ToList();for(int a=0;a<rectangles.Count;a++)for(int b=a+1;b<rectangles.Count;b++)if(rectangles[a].Intersects(rectangles[b],0))throw new Exception("Sobreposição na folha "+sheet.SheetNumber);}
  if(!physical.SequenceEqual(Physical(doc))||views.Except(LegalCatalog.Views(doc).Select(v=>v.Id.Value)).Any())throw new Exception("Geometria ou identidade das vistas alterada.");
  File.WriteAllText(Path.Combine(output,"TESTES_LEGAIS_05.json"),JsonSerializer.Serialize(new{Version="0.5.2",Results=results,SheetResults=sheetResults,Placements=placements,Repeat=true,EditedAnnotationsProtected=protectedEdit,NativeFacades=true,CardinalDirections=true,SheetRepeat=true,SheetOverlapCheck=true,PhysicalBoundsPreserved=true,OriginalViewsPreserved=true,FictitiousData=true},new JsonSerializerOptions{WriteIndented=true}));
 }
 static int Count(Document doc,long view)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Count(e=>e.OwnerViewId.Value==view);
 static List<string> Physical(Document doc)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e is HostObject or FamilyInstance or DirectShape).Select(e=>e.Id.Value+"/"+(e.get_BoundingBox(null) is {} b?string.Join("/",new[]{b.Min.X,b.Min.Y,b.Min.Z,b.Max.X,b.Max.Y,b.Max.Z}.Select(v=>Math.Round(v,6))):"none")).Order().ToList();
}








