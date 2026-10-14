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
   if(job.Mode=="modelreview"){ModelReviewExport.Run(doc,job.Output);doc.Close(false);doc=null;File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Cópia só de modelagem salva e verificada; origem preservada.");return;}
   if(job.Mode=="composition014"){ValidateComposition014(doc,job.Output);doc.Save();doc.Close(false);doc=null;File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Composição V02 limpa e centralizada na cópia de teste.");return;}
   if(job.Mode=="facades055"){ValidateFacades055(doc,job.Output);doc.Save();doc.Close(false);doc=null;File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Fachadas e linhas de situação validadas na cópia de teste.");return;}
   if(job.Mode=="legal05"){ValidateLegal05(doc,job.Output);doc.Save();doc.Close(false);doc=null;File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Urbanismo e folhas verificados na cópia de teste.\n"+job.Project);return;}
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
  void Step(string value)=>File.WriteAllText(Path.Combine(output,"PASSO.txt"),value);
  var results=new List<ModuleResult>();
  var urbanistic=new ContextSelection("urbanistico",source.Id.Value,"ESTUDO FICTÍCIO",ComputableArea:195,PermeableArea:50,MaximumOccupation:70,MaximumCoefficient:1.5,MinimumPermeability:20,Confirmed:true);
  var initialView=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="urbanistico"))??throw new Exception("Quadro ausente.");
  var manual=new FilteredElementCollector(doc,initialView.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==initialView.Id&&!DrawingOwnership.IsAnnotation(e)).ToDictionary(e=>e.UniqueId,DrawingOwnership.Fingerprint);
  Step("Quadro: campos ausentes");
  var pending=LegalContext.Apply(doc,new("urbanistico",source.Id.Value,"ESTUDO FICTÍCIO — campos ausentes",Confirmed:true));
  var view=(View)doc.GetElement(new ElementId(pending.ViewId));
  List<string> Texts()=>new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(DrawingOwnership.IsAnnotation).Select(n=>n.Text.Trim()).ToList();
  if(!Texts().Contains("NÃO INFORMADA")||!Texts().Any(s=>s.StartsWith("PENDENTE")))throw new Exception("Dados ausentes sem pendência explícita.");
  Step("Quadro: cálculo e repetição");
  var first=LegalContext.Apply(doc,urbanistic);int count=Count(doc,first.ViewId);var second=LegalContext.Apply(doc,urbanistic);
  if(first.ViewId!=second.ViewId||count!=Count(doc,second.ViewId))throw new Exception("Repetição instável do quadro.");
  var lot=Implantation.Saved(doc)!;double lotArea=lot.LotWidth*lot.LotDepth;double projection=LegalMetrics.PolygonArea(Implantation.GetContour(doc,floor).Select(p=>(Implantation.Meters(p.X),Implantation.Meters(p.Y))).ToList());
  string N(double value)=>value.ToString("0.00",Implantation.Pt);
  foreach(var expected in new[]{N(lotArea)+" m²",N(195)+" m²",N(50)+" m²",N(projection/lotArea*100)+"% · limite: 70,00%",N(195/lotArea)+" · limite: 1,50",N(50/lotArea*100)+"% · mínimo: 20,00%","fora do limite informado"})
   if(!Texts().Contains(expected))throw new Exception("Texto calculado ausente: "+expected);
  Step("Quadro: proteção de edição");
  var note=new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>().First(n=>DrawingOwnership.IsAnnotation(n));using(var tx=new Transaction(doc,"Teste de proteção do quadro")){tx.Start();note.Text+=" edição";tx.Commit();}bool protectedEdit=false;try{LegalContext.Apply(doc,urbanistic);}catch(InvalidOperationException){protectedEdit=true;}if(!protectedEdit)throw new Exception("Edição do quadro não protegida.");results.Add(LegalContext.Apply(doc,urbanistic with{ReplaceEdited=true}));
  if(manual.Any(p=>doc.GetElement(p.Key) is not {} e||DrawingOwnership.Fingerprint(e)!=p.Value))throw new Exception("Conteúdo manual do quadro alterado.");
  File.WriteAllText(Path.Combine(output,"QUADRO_URBANISTICO.json"),JsonSerializer.Serialize(new{Version=Assembly.GetExecutingAssembly().GetName().Version?.ToString(),LotArea=lotArea,Projection=projection,TO=projection/lotArea*100,CA=195/lotArea,TP=50/lotArea*100,Texts=Texts(),MissingDataPending=true,Repeat=true,EditedAnnotationsProtected=protectedEdit,ManualContentPreserved=true,FictitiousData=true},new JsonSerializerOptions{WriteIndented=true}));
  doc.Save();
  // Use prepared references only. Do not regenerate already approved facade or situation content.
  foreach(var key in new[]{"situacao","implantacao","planta-1","cobertura","corte-a","corte-b","fachada-norte","fachada-sul","fachada-leste","fachada-oeste","areas"})
  {var prepared=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key==key));if(prepared!=null)results.Add(new(prepared.Id.Value,0,0,"Referência existente preservada: "+key));}
  var sheets=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s=>s.SheetNumber is "LEG-01" or "LEG-02").OrderBy(s=>s.SheetNumber).ToList();if(sheets.Count!=2)throw new Exception("Folhas legais ausentes.");
  var sheetResults=new List<ModuleResult>();var placements=new List<SheetSelection>();var unplaced=new List<object>();
  var existingPlacements=sheets.SelectMany(s=>new FilteredElementCollector(doc,s.Id).WhereElementIsNotElementType().Where(e=>e is Viewport or ScheduleSheetInstance).Select(e=>new{e.UniqueId,Sheet=s.Id,Bounds=LegalSheets.Rect(e,s)})).ToList();
  var initialOverlaps=new HashSet<string>();
  string Pair(Element a,Element b)=>string.Join("/",new[]{a.Id.Value,b.Id.Value}.Order());
  foreach(var s in sheets){var es=new FilteredElementCollector(doc,s.Id).WhereElementIsNotElementType().Where(e=>e is Viewport or ScheduleSheetInstance).ToList();for(int a=0;a<es.Count;a++)for(int b=a+1;b<es.Count;b++)if(LegalSheets.Rect(es[a],s)!.Value.Intersects(LegalSheets.Rect(es[b],s)!.Value,0))initialOverlaps.Add(Pair(es[a],es[b]));}
  foreach(var result in results)
  {
   Step("Folhas: "+doc.GetElement(new ElementId(result.ViewId)).Name);var failures=new List<string>();
   bool placed=false;foreach(var sheet in sheets){int beforeCount=Count(doc,sheet.Id.Value);try{var input=new SheetSelection(sheet.Id.Value,result.ViewId);var target=(View)doc.GetElement(new ElementId(result.ViewId));int scale=target.Scale;var placement=LegalSheets.Apply(doc,input);int placedCount=Count(doc,sheet.Id.Value);var repeated=LegalSheets.Apply(doc,input);if(placedCount!=Count(doc,sheet.Id.Value)||repeated.Annotations!=0||target.Scale!=scale)throw new Exception("Colocação duplicada ou escala alterada.");sheetResults.Add(placement);placements.Add(input);placed=true;doc.Save();break;}catch(InvalidOperationException ex){if(beforeCount!=Count(doc,sheet.Id.Value))throw new Exception("Falha de colocação deixou elementos na folha.",ex);failures.Add(sheet.SheetNumber+": "+ex.Message);}}
   if(!placed)unplaced.Add(new{result.ViewId,Name=doc.GetElement(new ElementId(result.ViewId)).Name,Reasons=failures});
  }
  var initialNoSpace=unplaced.ToList();unplaced.Clear();var newSheets=new List<string>();
  // Lucas explicitly chose additional sheets at the existing scales on 14/10/2026.
  foreach(var result in results.Where(r=>!placements.Any(p=>p.ViewId==r.ViewId)))
  {
   Step("Folha adicional: "+doc.GetElement(new ElementId(result.ViewId)).Name);var target=(View)doc.GetElement(new ElementId(result.ViewId));int scale=target.Scale;ModuleResult? placement=null;ViewSheet? destination=null;
   foreach(var candidate in sheets.Skip(2)){try{placement=LegalSheets.Apply(doc,new(candidate.Id.Value,result.ViewId));destination=candidate;break;}catch(InvalidOperationException){}}
   if(placement==null){placement=LegalSheets.Apply(doc,new(sheets[1].Id.Value,result.ViewId,true));destination=(ViewSheet)doc.GetElement(new ElementId(placement.ViewId));sheets.Add(destination);newSheets.Add(destination.SheetNumber);}
   int placedCount=Count(doc,destination!.Id.Value);var selection=new SheetSelection(destination.Id.Value,result.ViewId);var repeated=LegalSheets.Apply(doc,selection);
   if(placedCount!=Count(doc,destination.Id.Value)||repeated.Annotations!=0||target.Scale!=scale)throw new Exception("Folha adicional duplicada ou escala alterada.");
   sheetResults.Add(placement);placements.Add(selection);doc.Save();
  }
  int sheetsBeforeFailure=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();bool newSheetFailureRolledBack=false;
  try{LegalSheets.Apply(doc,new(sheets[1].Id.Value,results[0].ViewId,true));}catch(InvalidOperationException){newSheetFailureRolledBack=sheetsBeforeFailure==new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();}
  if(!newSheetFailureRolledBack)throw new Exception("Falha de nova folha deixou folha vazia.");
  File.WriteAllText(Path.Combine(output,"COLOCACOES.json"),JsonSerializer.Serialize(new{SheetResults=sheetResults,Placements=placements,InitialNoSpace=initialNoSpace,Unplaced=unplaced,NewSheets=newSheets,InitialOverlaps=initialOverlaps,Sheets=sheets.Select(s=>new{s.SheetNumber,Elements=new FilteredElementCollector(doc,s.Id).WhereElementIsNotElementType().Where(e=>e is Viewport or ScheduleSheetInstance).Select(e=>new{Id=e.Id.Value,Type=e.GetType().Name,Name=e is Viewport vp?doc.GetElement(vp.ViewId).Name:e is ScheduleSheetInstance si?doc.GetElement(si.ScheduleId).Name:e.Name,Bounds=LegalSheets.Rect(e,s)}).ToList()})},new JsonSerializerOptions{WriteIndented=true}));
  foreach(var sheet in sheets){var elements=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().Where(e=>e is Viewport or ScheduleSheetInstance).ToList();var rectangles=elements.Select(e=>LegalSheets.Rect(e,sheet)!.Value).ToList();for(int a=0;a<rectangles.Count;a++)for(int b=a+1;b<rectangles.Count;b++)if(rectangles[a].Intersects(rectangles[b],0)&&!initialOverlaps.Contains(Pair(elements[a],elements[b])))throw new Exception("Nova sobreposição na folha "+sheet.SheetNumber+": "+Pair(elements[a],elements[b]));}
  if(!physical.SequenceEqual(Physical(doc))||views.Except(LegalCatalog.Views(doc).Select(v=>v.Id.Value)).Any())throw new Exception("Geometria ou identidade das vistas alterada.");
  if(existingPlacements.Any(p=>doc.GetElement(p.UniqueId) is not {} e||LegalSheets.Rect(e,(ViewSheet)doc.GetElement(p.Sheet))!=p.Bounds))throw new Exception("Colocação anterior deslocada.");
  File.WriteAllText(Path.Combine(output,"TESTES_LEGAIS_05.json"),JsonSerializer.Serialize(new{Version=Assembly.GetExecutingAssembly().GetName().Version?.ToString(),Results=results,SheetResults=sheetResults,Placements=placements,Unplaced=unplaced,NewSheets=newSheets,AllDeliverablesPlaced=unplaced.Count==0,NewSheetFailureRolledBack=newSheetFailureRolledBack,Repeat=true,EditedAnnotationsProtected=protectedEdit,SheetRepeat=true,NoNewSheetOverlaps=true,PreExistingOverlapPairs=initialOverlaps.ToList(),ExistingPlacementsPreserved=true,FailedPlacementsRolledBack=true,PhysicalBoundsPreserved=true,OriginalViewsPreserved=true,FictitiousData=true},new JsonSerializerOptions{WriteIndented=true}));
 }
 static void ValidateComposition014(Document doc,string output)
 {
  var physical=Physical(doc);var viewIds=LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order().ToList();var scales=LegalCatalog.Views(doc).ToDictionary(v=>v.Id.Value,v=>v.Scale);
  var result=LegalSheetComposition.Apply(doc);doc.Save();
  string PlacementState()=>string.Join("\n",new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().OrderBy(v=>v.Id.Value).Select(v=>v.Id.Value+"/"+v.ViewId.Value+"/"+v.SheetId.Value+"/"+v.GetBoxCenter().ToString()+"/"+v.LabelOffset.ToString()));
  string placed=PlacementState();var repeat=LegalSheetComposition.Apply(doc);
  if(placed!=PlacementState()||repeat.HiddenGuides!=0||repeat.RemovedEmptySheets!=0)throw new Exception("Revisão de composição instável ao repetir.");
  var sheets=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(LegalSheets.IsLegalSheet).OrderBy(s=>s.SheetNumber).ToList();
  if(sheets.Count!=3)throw new Exception("Composição deveria usar três folhas.");
  foreach(var sheet in sheets)LegalSheetComposition.Validate(doc,sheet);
  if(!physical.SequenceEqual(Physical(doc))||!viewIds.SequenceEqual(LegalCatalog.Views(doc).Select(v=>v.Id.Value).Order())||scales.Any(p=>((View)doc.GetElement(new ElementId(p.Key))).Scale!=p.Value))throw new Exception("Geometria, identidade das vistas ou escala alterada.");
  var roof=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="cobertura"))!;
  var south=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="fachada-sul"))!;
  foreach(var view in new[]{roof,south})if(new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Single(v=>v.ViewId==view.Id).SheetId!=sheets.Single(s=>s.SheetNumber=="LEG-02").Id)throw new Exception("Conteúdo da antiga LEG-04 não foi para LEG-02.");
  var urban=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="urbanistico"))!;
  var texts=new FilteredElementCollector(doc,urban.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>DrawingOwnership.IsAnnotation(n)).ToList();
  // TextNote bounding boxes include invisible margins. The table uses top-left
  // anchors at scale 1; use the native paper Width/Height for reading collisions.
  var boxes=texts.Select(n=>new LegalPaperRect(n.Coord.X,n.Coord.Y-n.Height,n.Coord.X+n.Width,n.Coord.Y)).ToList();for(int a=0;a<boxes.Count;a++)for(int b=a+1;b<boxes.Count;b++)if(boxes[a].Intersects(boxes[b],0))throw new Exception("Textos sobrepostos no quadro compacto: "+texts[a].Text+" / "+texts[b].Text);
  File.WriteAllText(Path.Combine(output,"COMPOSICAO.json"),JsonSerializer.Serialize(new{Version=Assembly.GetExecutingAssembly().GetName().Version?.ToString(),Result=result,Repeat=true,AllTitlesInsideMargins=true,NoViewportOverlaps=true,CompactTableTextNoOverlap=true,ScalesPreserved=true,PhysicalBoundsPreserved=true,OriginalViewsPreserved=true,RoofAndSouthOnLeg02=true,Sheets=sheets.Select(s=>new{s.SheetNumber,s.Name,Area=LegalSheetComposition.Area(doc,s),Viewports=new FilteredElementCollector(doc,s.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Where(v=>!v.IsHidden(s)).Select(v=>new{Id=v.Id.Value,View=doc.GetElement(v.ViewId).Name,Scale=((View)doc.GetElement(v.ViewId)).Scale,Bounds=LegalSheetComposition.Bounds(v,s),LabelOffset=new[]{v.LabelOffset.X,v.LabelOffset.Y},Detail=v.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.AsString()}),VisibleSheetNotes=new FilteredElementCollector(doc,s.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>!n.IsHidden(s)).Select(n=>n.Text).ToList()})},new JsonSerializerOptions{WriteIndented=true}));
 }
 static int Count(Document doc,long view)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Count(e=>e.OwnerViewId.Value==view);
 static List<string> Physical(Document doc)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>e is HostObject or DirectShape||e is FamilyInstance&&e.Category?.CategoryType==CategoryType.Model).Select(e=>e.Id.Value+"/"+(e.get_BoundingBox(null) is {} b?string.Join("/",new[]{b.Min.X,b.Min.Y,b.Min.Z,b.Max.X,b.Max.Y,b.Max.Z}.Select(v=>Math.Round(v,6))):"none")).Order().ToList();
}








