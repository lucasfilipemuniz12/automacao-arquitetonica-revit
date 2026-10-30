using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;

internal static class Case04Legal
{
 internal static void Run(Document doc,string output)
 {
  if(!Path.GetFileName(doc.PathName).StartsWith("R3_TESTE_CT04",StringComparison.Ordinal))throw new InvalidOperationException("CT04 legal exige cópia R3_TESTE_CT04.rvt.");
  Directory.CreateDirectory(output);var before=SectionValidation.Physical(doc);var steps=new List<object>();
  void Step(string name,Func<string> action,bool expectedLimitation=false)
  {File.WriteAllText(Path.Combine(output,"PASSO.txt"),name);try{steps.Add(new{Name=name,Status="funcionou",Report=action()});}catch(Exception ex){steps.Add(new{Name=name,Status=expectedLimitation&&ex is InvalidOperationException?"aviso":"falhou",Report=ex.ToString()});}File.WriteAllText(Path.Combine(output,"PASSOS.json"),JsonSerializer.Serialize(steps,new JsonSerializerOptions{WriteIndented=true}));}
  var plans=Implantation.ModelPlans(doc).GroupBy(p=>p.GenLevel.Id).Select(g=>g.First()).ToList();var source=plans.First(p=>p.GenLevel.Name=="Térreo");var floor=Implantation.Footprints(doc,source).First();
  var profile=MunicipalLibrary.Read().FirstOrDefault(p=>p.Title.Contains("Belo Horizonte",StringComparison.OrdinalIgnoreCase));
  Step("Cadastro e perfil informado",()=>{ProjectRegistrationStore.Save(doc,new("Residência Tavares","CT04-ARQ-LEG","Rafael Moreira Tavares e Juliana Castro Tavares","Rua das Quaresmeiras, 215, esquina com Rua Canela-de-Ema — Vale das Encostas — CEP 31.999-215","Lote 06, Quadra 12 — CP 432-017-M — IPTU 432.017.006.0001-3 — matrícula 87.654, 6º Ofício","Belo Horizonte","MG","OP-2","Residencial unifamiliar","Construção nova","2 pavimentos + garagem em meio-nível","CPF 111.444.777-35 — RRT 14892301",profile is null?null:profile with{Zone="OP-2"}));return profile?.Title??"Perfil não encontrado: índices permanecem a verificar.";});
  Step("Carimbo",()=>StampData.Apply(doc,new(){{"autor","Arq. Helena Prado Siqueira — CAU A123456-7 — RRT 14892301"},{"emissao","26/10/2026"},{"projetado","Helena Prado Siqueira"},{"desenhado","VEBRAM Arquitetura"},{"verificado","Helena Prado Siqueira"},{"aprovado","Helena Prado Siqueira"}}));
  var options=new ImplantationOptions(source.Id.Value,floor.Id.Value,-1,0,16.2,26,200,"Abaixo",true,2,7,"Rua das Quaresmeiras",0,false,true,SecondaryStreet:new("Direita","Rua Canela-de-Ema",2,7),LotPolygon:Case04Model.Boundary);
  Step("Implantação — lote real de cinco lados",()=>Implantation.Apply(doc,Implantation.Plan(doc,options)).Report,true);
  Step("Projeções nativas das lajes, sem calcular índices do lote recusado",()=>
  {
   var garage=new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().First(f=>f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()=="CT04:LAJE GARAGEM");
   double house=OccupationFootprint.Measure(doc,options).Total,combined=OccupationFootprint.Measure(doc,options with{OccupationExtraIds=[garage.Id.Value]}).Total;
   return "Casa: "+house.ToString("0.00",Implantation.Pt)+" m²; casa + garagem: "+combined.ToString("0.00",Implantation.Pt)+" m². Nenhum índice foi calculado usando o retângulo envolvente.";
  });
  foreach(var key in new[]{"situacao","urbanistico"})Step(key,()=>LegalContext.Apply(doc,new(key,source.Id.Value,"Lote 06, Quadra 12","Lote 05 — Antônio Carlos Ribeiro Neves — Rua das Quaresmeiras, 205","Rua Canela-de-Ema — Quadra 13 / praça municipal","Lote 07 — Marlene Duarte Fonseca — Rua Canela-de-Ema, 88",Confirmed:true,ReplaceEdited:true)).Report,true);
  foreach(var plan in plans.Where(p=>p.GenLevel.Name is "Térreo" or "Superior"))
  {
   var slab=Implantation.Footprints(doc,plan).FirstOrDefault();if(slab==null)continue;
   Step("Planta "+plan.GenLevel.Name,()=>LegalModules.Apply(doc,new("planta",plan.Id.Value,slab.Id.Value,true)).Report);
   Step("Memória "+plan.GenLevel.Name,()=>MemoryAreas.Apply(doc,plan,true).Report,true);
  }
  foreach(var key in new[]{"cobertura","areas","esquadrias"})Step(key,()=>LegalModules.Apply(doc,new(key,source.Id.Value,floor.Id.Value,true)).Report);
  foreach(var key in new[]{"corte-a","corte-b"})Step(key,()=>LegalSections.Apply(doc,new(key,source.Id.Value,floor.Id.Value,key=="corte-a",50,false,25,true)).Report);
  Step("Fachadas",()=>string.Join("\n",LegalFacades.Apply(doc,new(source.Id.Value,floor.Id.Value,3,0,true)).Select(r=>r.Report)));
  Step("Organizar entregáveis que puderam ser preparados",()=>{var plan=SheetPlanner.Preview(doc,"preferencial",SheetPlanner.Items(doc).Select(i=>i.ViewId));var r=SheetPlanner.Apply(doc,plan);File.WriteAllText(Path.Combine(output,"FOLHAS.json"),JsonSerializer.Serialize(plan,new JsonSerializerOptions{WriteIndented=true}));return r.Report;});
  foreach(var sheet in SheetPlanner.Sheets(doc))Step("Exportar "+sheet.SheetNumber,()=>{using var image=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,"FOLHA"),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2200};image.SetViewsAndSheets([sheet.Id]);doc.ExportImage(image);return sheet.Name;});
  StampSurvey.Run(doc,output);AreaSurvey.Run(doc,output);bool preserved=before.SequenceEqual(SectionValidation.Physical(doc));File.WriteAllText(Path.Combine(output,"PRESERVACAO.json"),JsonSerializer.Serialize(new{PhysicalPreserved=preserved,WarningCount=doc.GetWarnings().Count}));
  if(!preserved)throw new InvalidOperationException("Modelo alterado durante teste CT04.");doc.Save();
 }
}
