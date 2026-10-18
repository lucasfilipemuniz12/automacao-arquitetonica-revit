using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class IntegrationValidation
{
 internal static void Run(Document doc,string output)
 {
  Directory.CreateDirectory(output);var before=SectionValidation.Physical(doc);
  var lot=Implantation.Saved(doc)??throw new Exception("A cópia precisa de Implantação confirmada.");
  var original=ProjectRegistrationStore.Read(doc);
  // Synthetic numbers exercise mapping only. They are never municipal reference data.
  var profile=new MunicipalProfile("fixture024","TESTE SINTÉTICO — NÃO USAR","Cidade de teste","MG","Zona teste","Residencial unifamiliar","Construção nova",new(){["to"]="55,5",["ca"]="1,2",["tp"]="25"},"Dados inventados só para teste de software.","FIXTURE DE SOFTWARE — SEM LEGISLAÇÃO","17/10/2026",1);
  var fixture=original with{City=profile.City,State=profile.State,Zone=profile.Zone,Use=profile.Use,Intervention=profile.Intervention,Profile=profile,Lot="LOTE TESTE 024"};
  var defaults=ProjectDataIntegration.Context(fixture,"urbanistico",lot.SourceId,null);
  if(defaults.Identification!=fixture.Lot||defaults.MaximumOccupation!=55.5||defaults.MaximumCoefficient!=1.2||defaults.MinimumPermeability!=25)throw new Exception("Mapeamento do cadastro incorreto.");
  var own=defaults with{Identification="",MaximumOccupation=null,MaximumCoefficient=.8,ComputableArea=123,RegistrationText="Fonte editada no entregável"};
  if(ProjectDataIntegration.Context(fixture,"urbanistico",lot.SourceId,own)!=own)throw new Exception("Ajustes e campos apagados foram substituídos.");
  var mismatch=ProjectDataIntegration.Context(fixture with{Zone="Outra zona"},"urbanistico",lot.SourceId,null);
  if(mismatch.MaximumOccupation!=null||mismatch.MaximumCoefficient!=null||mismatch.MinimumPermeability!=null)throw new Exception("Perfil incompatível sugeriu limites.");
  var missing=ProjectDataIntegration.Context(fixture with{Profile=null},"urbanistico",lot.SourceId,null);
  if(missing.MaximumOccupation!=null||!ProjectDataIntegration.Pending(missing).Contains("TO máxima"))throw new Exception("Ausência de regra ocultada.");
  var other=defaults with{Identification="Identificação já conferida"};
  if(ProjectDataIntegration.Context(fixture,"situacao",lot.SourceId,null,other).Identification!=other.Identification)throw new Exception("Identificação compartilhada perdida.");
  if(ProjectDataIntegration.Context(fixture,"situacao",lot.SourceId+1,null,other).Identification!=fixture.Lot)throw new Exception("Identificação usada com outra fonte.");
  // Drawing tests use the user's existing demonstrative registration, with no synthetic rules written.
  var outputs=new List<long>();
  var implantation=Implantation.Apply(doc,Implantation.Plan(doc,lot with{ReplaceEdited=true}));
  var implantationAgain=Implantation.Apply(doc,Implantation.Plan(doc,lot with{ReplaceEdited=true}));
  if(implantation.ViewId!=implantationAgain.ViewId||implantation.Annotations!=implantationAgain.Annotations)throw new Exception("Implantação duplicada ou instável.");
  if(!new FilteredElementCollector(doc,new ElementId(implantation.ViewId)).OfClass(typeof(TextNote)).Cast<TextNote>().Any(n=>n.Text.Contains(original.Lot)&&n.Text.Contains(original.City)))throw new Exception("Cadastro ausente na Implantação.");
  outputs.Add(implantation.ViewId);
  foreach(var key in new[]{"situacao","urbanistico"})
  {
   var input=ProjectDataIntegration.Context(original,key,lot.SourceId,null) with{Confirmed=true,ReplaceEdited=true};
   var first=LegalContext.Apply(doc,input);var second=LegalContext.Apply(doc,input);
   if(first.ViewId!=second.ViewId||first.Annotations!=second.Annotations)throw new Exception("Regeneração instável: "+key);
   var read=LegalContext.Saved(doc,key)??throw new Exception("Configuração não persistida.");
   if(read.Identification!=original.Lot||read.RegistrationText!=input.RegistrationText)throw new Exception("Cadastro não chegou ao entregável: "+key);
   if(input.RegistrationText.Length>0&&!new FilteredElementCollector(doc,new ElementId(second.ViewId)).OfClass(typeof(TextNote)).Cast<TextNote>().Any(n=>n.Text.Contains(original.City)))throw new Exception("Município não aparece no desenho.");
   outputs.Add(second.ViewId);
  }
  if(!before.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo físico alterado.");
  using var image=new ImageExportOptions{ExportRange=ExportRange.SetOfViews,FilePath=Path.Combine(output,"INTEGRACAO"),HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG,ZoomType=ZoomFitType.FitToPage,PixelSize=2000};image.SetViewsAndSheets(outputs.Select(id=>new ElementId(id)).ToList());doc.ExportImage(image);
  File.WriteAllText(Path.Combine(output,"INTEGRACAO_024.json"),JsonSerializer.Serialize(new{Version="0.5.24",ProfileMapping=true,EmptyOverridesPreserved=true,IncompatibleProfileRejected=true,MissingRulesReported=true,SharedIdentification=true,SourceIsolation=true,NativeRegeneration=true,RegistrationInDrawings=true,ImplantationRegistration=true,ImplantationRegeneration=true,PhysicalPreserved=true,SyntheticValuesPersisted=false},new JsonSerializerOptions{WriteIndented=true}));doc.Save();
 }
}
