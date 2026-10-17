using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class RegistrationValidation
{
 internal static void Run(Document doc,string output)
 {
  var physical=SectionValidation.Physical(doc);var previous=MunicipalLibrary.ValidationFolder;
  try
  {
   MunicipalLibrary.ValidationFolder=Path.Combine(output,"biblioteca-isolada");
   var original=MunicipalLibrary.Save(new(Guid.NewGuid().ToString("N"),"DEMONSTRAÇÃO — SEM REGRAS VALIDADAS","Belo Horizonte","MG","Zona de teste — não usar","Residencial unifamiliar","Construção nova",new(),"Perfil fictício de teste. Não usar para aprovação.","Nenhuma legislação cadastrada — demonstração","17/10/2026"));
   if(MunicipalLibrary.Read().Single().Revision!=1)throw new Exception("Biblioteca não foi persistida.");
   ProjectRegistrationStore.Save(doc,new("Casa Terras — protótipo de cadastro","UI-023","","Endereço de demonstração — conferir no modelo","Lote de demonstração","Belo Horizonte","MG",original.Zone,original.Use,original.Intervention,"1","Cópia de teste; sem validação municipal.",original));
   var updated=MunicipalLibrary.Save(original with{Title="DEMONSTRAÇÃO — REVISÃO 2"});
   if(updated.Revision!=2)throw new Exception("Revisão municipal não incrementada.");
   bool conflict=false;try{MunicipalLibrary.Save(original);}catch(InvalidOperationException){conflict=true;}
   if(!conflict)throw new Exception("Edição de revisão antiga não foi bloqueada.");
   var saved=ProjectRegistrationStore.Read(doc);
   if(saved.Profile?.Revision!=1||saved.Profile.Title!=original.Title)throw new Exception("Atualizar biblioteca alterou a cópia do projeto.");
   if(doc.ProjectInformation.get_Parameter(BuiltInParameter.PROJECT_NAME)?.AsString()!=saved.Name)throw new Exception("Nome do projeto não atualizado.");
   if(!physical.SequenceEqual(SectionValidation.Physical(doc)))throw new Exception("Modelo físico alterado.");
   doc.Save();
   File.WriteAllText(Path.Combine(output,"CADASTRO_023.json"),JsonSerializer.Serialize(new{Version="0.5.23",LibraryPersisted=true,RevisionIncremented=true,StaleEditRejected=true,ProjectSnapshotPreserved=true,PhysicalPreserved=true,Project=saved},new JsonSerializerOptions{WriteIndented=true}));
  }
  finally{MunicipalLibrary.ValidationFolder=previous;}
 }
}
