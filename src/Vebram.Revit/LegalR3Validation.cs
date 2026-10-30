using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;

// Explicit opt-in startup validation, only on fresh copies prepared by the developer.
internal static class LegalR3Validation
{
 internal static void Run(Document doc,string output,bool ct03)
 {
  if(!Path.GetFileName(doc.PathName).StartsWith("R3_TESTE_",StringComparison.Ordinal)||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("R3 exige cópia R3_TESTE_*.rvt.");
  Directory.CreateDirectory(output);var before=SectionValidation.Physical(doc);var steps=new List<object>();
  void Step(string name,Func<string> action)
  {
   File.WriteAllText(Path.Combine(output,"PASSO.txt"),name);
   try{steps.Add(new{Name=name,Status="executado",Report=action()});}
   catch(Exception ex){steps.Add(new{Name=name,Status="falhou",Report=ex.ToString()});}
   File.WriteAllText(Path.Combine(output,"PASSOS.json"),JsonSerializer.Serialize(steps,new JsonSerializerOptions{WriteIndented=true}));
  }
  var plans=Implantation.ModelPlans(doc);var source=ct03?plans.First(v=>Math.Abs(Implantation.Meters(v.GenLevel.Elevation)-.15)<.01):plans.First(v=>v.Name=="PRE - Layout / Nível 1");
  var floors=Implantation.Footprints(doc,source);var floor=ct03?floors.First(f=>f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()=="CT03:slab:terreo_principal"):floors.First();
  var options=ct03?new ImplantationOptions(source.Id.Value,floor.Id.Value,0,-25,15,25,100,"Acima",true,2.5,9,"Rua das Paineiras",0,false,true):Implantation.Saved(doc)??throw new InvalidOperationException("Sobrado sem dados da Implantação: use a cópia de teste com o cadastro já confirmado.");
  options=options with{SourceId=source.Id.Value,FootprintId=floor.Id.Value,ReplaceEdited=true,SecondaryStreet=ct03?new StreetSide("Direita","Rua dos Jacarandás",2,8):options.SecondaryStreet};
  if(ct03)
  {
   using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"casa.json")));
   var registration=fixture.RootElement.GetProperty("cadastro");string C(string key)=>registration.GetProperty(key).GetString()!;
   Step("Cadastro com Pavimentos em texto",()=>
   {
    ProjectRegistrationStore.Save(doc,new(C("projeto"),C("codigo"),C("cliente"),C("endereco")+" — "+C("bairro")+" — CEP "+C("cep"),C("lote")+" — "+C("inscricao_cadastral"),C("cidade"),C("uf"),C("zoneamento"),C("uso"),C("intervencao"),C("pavimentos"),C("observacao")));
    if(ProjectRegistrationStore.Read(doc).Floors!=C("pavimentos"))throw new Exception("Descrição de pavimentos alterada.");return ProjectRegistrationStore.Read(doc).Floors;
   });
   Step("Carimbo",()=>StampData.Apply(doc,new Dictionary<string,string>{{"nome",C("projeto")},{"numero",C("codigo")},{"cliente",C("cliente")},{"endereco",C("endereco")+" — "+C("bairro")+" — "+C("cidade")+"/"+C("uf")},{"autor",C("autor")+" — "+C("registro_profissional")},{"emissao",C("data_emissao")},{"projetado",C("projetado_por")},{"desenhado",C("desenhado_por")},{"verificado",C("verificado_por")},{"aprovado",C("aprovado_por")}}));
  }
  var frame=BuildingFrame.Read(doc,source,floor);
  File.WriteAllText(Path.Combine(output,"ENVELOPE.json"),JsonSerializer.Serialize(new{Bottom=Implantation.Meters(frame.Bottom),Top=Implantation.Meters(frame.Top),Members=frame.Members.Select(e=>new{Id=e.Id.Value,Class=e.GetType().Name,Name=e.Name}),Corners=frame.Corners.Select(p=>new[]{Implantation.Meters(p.X),Implantation.Meters(p.Y),Implantation.Meters(p.Z)})},new JsonSerializerOptions{WriteIndented=true}));
  Step("Implantação e nível da fonte",()=>Implantation.Apply(doc,Implantation.Plan(doc,options)).Report);
  if(ct03)
  {
   var extra=floors.First(f=>f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()=="CT03:slab:terraco");
   Step("TO uma laje",()=>OccupationFootprint.Measure(doc,options).Total.ToString("F2",Implantation.Pt));
   Step("TO duas lajes",()=>OccupationFootprint.Measure(doc,options with{OccupationExtraIds=[extra.Id.Value]}).Total.ToString("F2",Implantation.Pt));
  }
  foreach(var key in new[]{"situacao","urbanistico"})
   Step(key,()=>LegalContext.Apply(doc,ct03?new ContextSelection(key,source.Id.Value,"Lote 07 — Quadra 22","Lote 06 — Carlos Eduardo Lima e Ana Paula Lima, Rua das Paineiras, 235","Rua dos Jacarandás (via pública, esquina)","Lote 08 — terreno vago, Imobiliária Serra Verde Ltda.",MaximumOccupation:50,MaximumCoefficient:1,MinimumPermeability:20,Confirmed:true,ReplaceEdited:true): (LegalContext.Saved(doc,key)??new ContextSelection(key,source.Id.Value,Confirmed:true)) with{SourceId=source.Id.Value,Confirmed=true,ReplaceEdited=true}).Report);
  foreach(var plan in plans.GroupBy(v=>v.GenLevel.Id).Select(g=>g.First()))
  {
   var slab=Implantation.Footprints(doc,plan).FirstOrDefault();if(slab==null)continue;
   Step("Planta "+plan.GenLevel.Name,()=>LegalModules.Apply(doc,new("planta",plan.Id.Value,slab.Id.Value,true)).Report);
   Step("Memória "+plan.GenLevel.Name,()=>LegalModules.Apply(doc,new("memoria",plan.Id.Value,0,true)).Report);
  }
  foreach(var key in new[]{"cobertura","areas","esquadrias"})Step(key,()=>LegalModules.Apply(doc,new(key,source.Id.Value,floor.Id.Value,true)).Report);
  Step("Corte A",()=>LegalSections.Apply(doc,new("corte-a",source.Id.Value,floor.Id.Value,!ct03,ct03?17:50,false,20,true)).Report);
  Step("Corte B",()=>LegalSections.Apply(doc,new("corte-b",source.Id.Value,floor.Id.Value,ct03,ct03?85:50,false,20,true)).Report);
  if(ct03)
  {
   var lower=plans.First(v=>v.GenLevel.Elevation<source.GenLevel.Elevation);var slab=Implantation.Footprints(doc,lower).First(f=>f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()=="CT03:slab:inferior_sobre_solo");
   Step("Corte com fonte Inferior",()=>LegalSections.Apply(doc,new("corte-a",lower.Id.Value,slab.Id.Value,false,17,false,20,true)).Report);
   Step("Restaurar Corte A fonte Térreo",()=>LegalSections.Apply(doc,new("corte-a",source.Id.Value,floor.Id.Value,false,17,false,20,true)).Report);
  }
  Step("Fachadas",()=>string.Join("\n",LegalFacades.Apply(doc,new(source.Id.Value,floor.Id.Value,3,0,true)).Select(r=>r.Report)));
  foreach(var view in LegalCatalog.Views(doc).OfType<ViewSection>().Where(v=>!v.IsTemplate&&LegalCatalog.IsLegal(v)))
  {
   Step("Exportar "+view.Name,()=>{Export(doc,view,output,"VISTA");return view.Name;});
  }
  Step("Organizar folhas — Implantação 1:200",()=>
  {
   var items=SheetPlanner.Items(doc);var scale=items.Where(i=>i.Key=="implantacao").ToDictionary(i=>i.ViewId,i=>200);
   var originalScales=items.Where(i=>!i.Schedule).ToDictionary(i=>i.ViewId,i=>((View)doc.GetElement(new ElementId(i.ViewId))).Scale);
   var originalIds=new FilteredElementCollector(doc).WhereElementIsNotElementType().Select(e=>e.Id.Value).Order().ToList();
   var plan=SheetPlanner.Preview(doc,"preferencial",items.Select(i=>i.ViewId),true,scale);
   if(originalScales.Any(p=>((View)doc.GetElement(new ElementId(p.Key))).Scale!=p.Value)||!originalIds.SequenceEqual(new FilteredElementCollector(doc).WhereElementIsNotElementType().Select(e=>e.Id.Value).Order()))throw new Exception("Prévia não reverteu escalas/elementos.");
   var report=SheetPlanner.Apply(doc,plan).Report;
   foreach(var pair in originalScales)if(((View)doc.GetElement(new ElementId(pair.Key))).Scale!=(scale.GetValueOrDefault(pair.Key,pair.Value)))throw new Exception("Escala final divergente da escolha.");
   File.WriteAllText(Path.Combine(output,"FOLHAS.json"),JsonSerializer.Serialize(plan,new JsonSerializerOptions{WriteIndented=true}));return report;
  });
  Step("Conferir numeração, total, responsáveis e Q04",()=>
  {
   var sheets=SheetPlanner.Sheets(doc);if(!sheets.Select(s=>s.SheetNumber).SequenceEqual(Enumerable.Range(1,sheets.Count).Select(i=>"LEG-"+i.ToString("00"))))throw new Exception("Numeração com lacuna.");
   foreach(var sheet in sheets)
   {
    if(!new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Any(n=>SheetNumberStamp.Generated(n)&&n.Text.Trim()==sheet.SheetNumber+"/"+sheets.Count.ToString("00")))throw new Exception("Número/total ausente em "+sheet.SheetNumber);
    if(ct03&&sheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY)?.AsString()!="Beatriz N. Prado")throw new Exception("Responsável não herdado em "+sheet.SheetNumber);
   }
   var tables=LegalCatalog.Views(doc).OfType<ViewSchedule>().Where(s=>s.Name.Contains("Q04")).Select(s=>new{View=s.Name,Rooms=new FilteredElementCollector(doc,s.Id).OfClass(typeof(SpatialElement)).OfType<Autodesk.Revit.DB.Architecture.Room>().Where(r=>r.Area>0).Select(r=>new{r.Number,Name=r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString(),Area=r.Area*.09290304}).ToList()}).ToList();
   if(ct03&&tables.Where(t=>t.View!=AreaScheduleTotals.UncoveredName).Any(t=>t.Rooms.Any(r=>r.Name=="Terraço")))throw new Exception("Terraço ainda no total útil.");
   File.WriteAllText(Path.Combine(output,"Q04_CONFERENCIA.json"),JsonSerializer.Serialize(tables,new JsonSerializerOptions{WriteIndented=true}));
   var second=SheetPlanner.Preview(doc,"preferencial",SheetPlanner.Items(doc).Select(i=>i.ViewId));if(second.Pages.Count!=sheets.Count)throw new Exception("Reorganização mudou quantidade de folhas sem alterar conteúdo.");
   return "Sequência e total corretos; responsáveis herdados; terraço separado. Nova prévia repetida sem mudar a quantidade de folhas.";
  });
  foreach(var sheet in SheetPlanner.Sheets(doc))Step("Exportar "+sheet.SheetNumber,()=>{Export(doc,sheet,output,"FOLHA");return sheet.Name;});
  StampSurvey.Run(doc,output);
  AreaSurvey.Run(doc,output);
  bool preserved=before.SequenceEqual(SectionValidation.Physical(doc));
  File.WriteAllText(Path.Combine(output,"PRESERVACAO.json"),JsonSerializer.Serialize(new{PhysicalPreserved=preserved,WarningCount=doc.GetWarnings().Count,Steps=steps.Count}));
  if(!preserved)throw new InvalidOperationException("Geometria física alterada durante teste; preservar sessão para investigação.");
  doc.Save();
 }
 static void Export(Document doc,View view,string output,string prefix)
 {
  using var image=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,prefix),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2200};image.SetViewsAndSheets([view.Id]);doc.ExportImage(image);
 }
}
