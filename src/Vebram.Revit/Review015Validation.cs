using System.IO;
using System.Reflection;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
namespace Vebram.Revit;
internal static class Review015Validation
{
 public static void Run(Document doc,string output,string? nameReference=null)
 {
  var physical=SectionValidation.Physical(doc);var names=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Room>().ToDictionary(r=>r.UniqueId,r=>r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
  var repairs=new List<RoomNameCorrection>();
  if(!string.IsNullOrWhiteSpace(nameReference))
  {
   using var reference=doc.Application.OpenDocumentFile(nameReference);try{repairs=RoomNameReview.Preview(doc,reference);}finally{reference.Close(false);}
   RoomNameReview.Apply(doc,repairs);foreach(var repair in repairs)names[repair.UniqueId]=repair.After;
   File.WriteAllText(Path.Combine(output,"NOMES_CONFERIDOS_ORIGEM.json"),JsonSerializer.Serialize(repairs,new JsonSerializerOptions{WriteIndented=true}));
  }
  var source=Implantation.ModelPlans(doc).First(v=>v.Name=="PRE - Layout / Nível 1");var floor=Implantation.Footprints(doc,source).First();
  using(var tx=new Transaction(doc,"Teste isolado de cópia de nome"))
  {
   tx.Start();var room=doc.Create.NewRoom(doc.Phases.get_Item(doc.Phases.Size-1));room.get_Parameter(BuiltInParameter.ROOM_NAME).Set("SUÍTE 1");room.Number="TESTE07";
   var copied=doc.Create.NewRoom(doc.Phases.get_Item(doc.Phases.Size-1));copied.Name=RoomNameText.Display(room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString());copied.Number="TESTE08";
   if(copied.get_Parameter(BuiltInParameter.ROOM_NAME).AsString()!="SUÍTE 1")throw new Exception("Cópia anexou o número.");tx.RollBack();
  }
  if(LegalPreferenceStore.Read(doc).IncludeWholeTerrain)throw new Exception("Teste exige recorte local padrão.");
  var frame=BuildingFrame.Read(doc,source,floor);var whole=BuildingFrame.Read(doc,source,floor,true);var results=new List<ModuleResult>();
  foreach(var key in new[]{"corte-a","corte-b"})results.Add(LegalSections.Apply(doc,new(key,source.Id.Value,floor.Id.Value,key=="corte-a")));
  results.AddRange(LegalFacades.Apply(doc,new(source.Id.Value,floor.Id.Value)));results.Add(LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value)));results.Add(LegalModules.Apply(doc,new("cobertura",source.Id.Value,floor.Id.Value)));
  var saved=Implantation.Saved(doc)??throw new Exception("Use cópia de RVT com Implantação preparada.");Implantation.Apply(doc,Implantation.Plan(doc,saved));
  var urban=LegalContext.Saved(doc,"urbanistico")??throw new Exception("Quadro ausente.");LegalContext.Apply(doc,urban with{Confirmed=true});
  var situation=LegalContext.Saved(doc,"situacao")??throw new Exception("Situação ausente.");LegalContext.Apply(doc,situation with{Confirmed=true});
  int Count(long id)=>new FilteredElementCollector(doc).WhereElementIsNotElementType().Count(e=>e.OwnerViewId.Value==id);
  var counts=results.ToDictionary(r=>r.ViewId,r=>Count(r.ViewId));
  foreach(var key in new[]{"corte-a","corte-b"})LegalSections.Apply(doc,new(key,source.Id.Value,floor.Id.Value,key=="corte-a"));LegalFacades.Apply(doc,new(source.Id.Value,floor.Id.Value));LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));LegalModules.Apply(doc,new("cobertura",source.Id.Value,floor.Id.Value));
  if(counts.Any(p=>Count(p.Key)!=p.Value))throw new Exception("Repetição duplicou anotações.");
  var crops=results.Select(r=>doc.GetElement(new ElementId(r.ViewId))).OfType<ViewSection>().ToList();
  foreach(var view in crops)
  {
   if(new FilteredElementCollector(doc,view.Id).OfClass(typeof(Wall)).GetElementCount()==0)throw new Exception("Casa ausente: "+view.Name);
   var b=view.CropBox;var projected=frame.Corners.Select(b.Transform.Inverse.OfPoint).ToList();
   if(Math.Abs(b.Max.X-b.Min.X-(projected.Max(p=>p.X)-projected.Min(p=>p.X)+Implantation.Feet(3)))>.001)throw new Exception("Recorte não acompanha edificação: "+view.Name);
   if(Math.Abs(b.Transform.OfPoint(new(b.Min.X,b.Min.Y,0)).Z-frame.Bottom)>.001)throw new Exception("Base do recorte divergiu: "+view.Name);
  }
  using(var tx=new Transaction(doc,"Conferir estilos"))
  {tx.Start();var text=LegalStyles.Text(doc);var dim=LegalPreferenceStore.Dimension(doc);if(!text.Name.StartsWith("ARQ")||text.Name.Contains("VEBRAM"))throw new Exception("Texto não reutilizado: "+text.Name);if(!dim.Name.StartsWith("ARQ")||dim.Name.Contains("VEBRAM"))throw new Exception("Cota não reutilizada: "+dim.Name);tx.RollBack();}
  bool protectedEdit=false;
  using(var group=new TransactionGroup(doc,"Teste de proteção"))
  {
   group.Start();var plan=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="planta-1"))!;
   using(var tx=new Transaction(doc,"Simular edição")){tx.Start();new FilteredElementCollector(doc,plan.Id).OfClass(typeof(TextNote)).Cast<TextNote>().First(DrawingOwnership.IsAnnotation).Text+=" manual";tx.Commit();}
   try{LegalModules.Apply(doc,new("planta",source.Id.Value,floor.Id.Value));}catch(InvalidOperationException){protectedEdit=true;}group.RollBack();
  }
  if(!protectedEdit)throw new Exception("Edição manual não protegida.");
  bool reused=false;
  using(var group=new TransactionGroup(doc,"Teste de folhas existentes"))
  {
   group.Start();ViewSheet blocked;ViewDrafting sample;
   using(var tx=new Transaction(doc,"Criar cenário temporário"))
   {
    tx.Start();var title=new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().First();blocked=ViewSheet.Create(doc,title.GetTypeId());blocked.SheetNumber="LEG-90";
    var box=new FilteredElementCollector(doc,blocked.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).First().get_BoundingBox(blocked)!;
    TextNote.Create(doc,blocked.Id,new(box.Min.X,box.Max.Y,0),box.Max.X-box.Min.X,string.Join("\n",Enumerable.Repeat("BLOQUEIO TEMPORÁRIO",350)),LegalStyles.Text(doc).Id);
    var type=new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(t=>t.ViewFamily==ViewFamily.Drafting);sample=ViewDrafting.Create(doc,type.Id);sample.Name="LEGAL — TESTE";sample.get_Parameter(LegalCatalog.StageId).Set(LegalCatalog.Stage);sample.Scale=1;TextNote.Create(doc,sample.Id,XYZ.Zero,"Teste",LegalStyles.Text(doc).Id);tx.Commit();
   }
   int before=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();var placed=LegalSheets.Apply(doc,new(blocked.Id.Value,sample.Id.Value,true));reused=placed.ViewId!=blocked.Id.Value&&before==new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();
   if(!reused)throw new Exception("Criou folha antes de tentar as existentes.");var repeatPlace=LegalSheets.Apply(doc,new(blocked.Id.Value,sample.Id.Value,true));if(repeatPlace.ViewId!=placed.ViewId||repeatPlace.Annotations!=0)throw new Exception("Colocação repetida instável.");group.RollBack();
  }
  var missing=new List<string>();var referenceSheet=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().First(s=>s.SheetNumber=="LEG-01");
  foreach(var key in new[]{"planta-1","cobertura","corte-a","corte-b","fachada-norte","fachada-sul","fachada-leste","fachada-oeste","implantacao","situacao","urbanistico","areas"})
  {
   var view=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key==key))??throw new Exception("Entregável ausente: "+key);
   if(view is not ViewSchedule&&!new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Any(v=>v.ViewId==view.Id))missing.Add(key);
   LegalSheets.Apply(doc,new(referenceSheet.Id.Value,view.Id.Value,true));
  }
  File.WriteAllText(Path.Combine(output,"COLOCACOES_COMPLETADAS.json"),JsonSerializer.Serialize(missing));
  var composition=LegalSheetComposition.Apply(doc);var repeat=LegalSheetComposition.Apply(doc);if(repeat.HiddenGuides!=0||repeat.RemovedEmptySheets!=0)throw new Exception("Composição instável.");
  if(!physical.SequenceEqual(SectionValidation.Physical(doc))||names.Any(p=>doc.GetElement(p.Key)?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString()!=p.Value))throw new Exception("Modelo ou nomes manuais alterados.");
  var sheets=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(LegalSheets.IsLegalSheet).ToList();if(sheets.Count!=3)throw new Exception("Restaram folhas complementares; conferir conteúdo antes de concluir.");var diagnostics=Diagnostics.Inspect(doc);doc.Save();
  File.WriteAllText(Path.Combine(output,"REVISAO_015.json"),JsonSerializer.Serialize(new{Version=Assembly.GetExecutingAssembly().GetName().Version?.ToString(),NativeNameCopy=true,UnrelatedNamesPreserved=true,RestoredNames=repairs,SuspectNames=diagnostics.Findings.Where(f=>f.Subject=="Nome de ambiente"),StylesReused=true,TerrainOptIn=true,SurfaceMeasured=frame.SurfaceMeasured,BottomMeters=Implantation.Meters(frame.Bottom),LocalWidth=Implantation.Meters(frame.Corners.Max(p=>p.X)-frame.Corners.Min(p=>p.X)),WholeSiteWidth=Implantation.Meters(whole.Corners.Max(p=>p.X)-whole.Corners.Min(p=>p.X)),Repeat=true,ManualEditsProtected=protectedEdit,ExistingSheetsBeforeCreation=reused,Composition=composition,Sheets=sheets.Select(s=>s.SheetNumber),Crops=crops.Select(v=>new{v.Name,WidthMeters=Implantation.Meters(v.CropBox.Max.X-v.CropBox.Min.X),v.Scale}),PhysicalBoundsPreserved=true},new JsonSerializerOptions{WriteIndented=true}));
  foreach(var view in sheets.Cast<View>().Concat(crops))
  {using var options=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,"REVISAO"),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2800};options.SetViewsAndSheets(new List<ElementId>{view.Id});doc.ExportImage(options);}
 }
}
