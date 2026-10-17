using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;

public sealed record MunicipalProfile(string Id,string Title,string City,string State,string Zone,string Use,string Intervention,
 Dictionary<string,string> Rules,string Conditions,string Source,string ReferenceDate,int Revision=0);
public sealed record ProjectRegistration(string Name,string Code,string Client,string Address,string Lot,string City,string State,
 string Zone,string Use,string Intervention,string Floors,string Notes,MunicipalProfile? Profile=null);

/// <summary>Personal reusable profiles; all rules are user supplied, never assumed from the city name.</summary>
public static class MunicipalLibrary
{
 internal static string? ValidationFolder{get;set;}
 public static string Folder=>ValidationFolder??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VEBRAM","Municipios");
 public static string FilePath=>Path.Combine(Folder,"municipios-v1.json");
 static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
 public static List<MunicipalProfile> Read()
 {
  if(!File.Exists(FilePath))return [];
  try{return JsonSerializer.Deserialize<List<MunicipalProfile>>(File.ReadAllText(FilePath))??throw new InvalidDataException("Biblioteca sem lista de perfis.");}
  catch(JsonException ex){throw new InvalidDataException("A biblioteca municipal não pôde ser lida. O arquivo foi preservado: "+FilePath,ex);}
 }
 public static MunicipalProfile Save(MunicipalProfile draft)
 {
  if(string.IsNullOrWhiteSpace(draft.Title)||string.IsNullOrWhiteSpace(draft.City)||!System.Text.RegularExpressions.Regex.IsMatch(draft.State,"^[A-Z]{2}$"))
   throw new InvalidOperationException("Informe nome do perfil, município e UF.");
  using var mutex=new Mutex(false,"Local\\VebramMunicipalLibraryV1");
  bool acquired=false;
  try
  {
   try{acquired=mutex.WaitOne(TimeSpan.FromSeconds(5));}catch(AbandonedMutexException){acquired=true;}
   if(!acquired)throw new InvalidOperationException("Outra janela está gravando a biblioteca. Tente novamente.");
   var profiles=Read();var previous=profiles.SingleOrDefault(p=>p.Id==draft.Id);
   if((previous?.Revision??0)!=draft.Revision)throw new InvalidOperationException("Este perfil foi atualizado em outra janela. Reabra a biblioteca antes de editar.");
   var saved=draft with{Revision=draft.Revision+1};profiles.RemoveAll(p=>p.Id==draft.Id);profiles.Add(saved);
   Directory.CreateDirectory(Folder);string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
   try
   {
    File.WriteAllText(temp,JsonSerializer.Serialize(profiles.OrderBy(p=>p.City).ThenBy(p=>p.Title),Json));
    if(File.Exists(FilePath))File.Copy(FilePath,FilePath+".bak",true);
    File.Move(temp,FilePath,true);
   }
   finally{if(File.Exists(temp))File.Delete(temp);}
   return saved;
  }
  finally{if(acquired)mutex.ReleaseMutex();}
 }
}

public static class ProjectRegistrationStore
{
 static readonly Guid Id=new("5c492a97-5ef8-4f2b-adeb-f53320cd6b71");
 static Schema Schema()
 {var existing=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(Id);if(existing!=null)return existing;var b=new SchemaBuilder(Id);b.SetSchemaName("VebramProjectRegistrationV1");b.AddSimpleField("Payload",typeof(string));return b.Finish();}
 public static ProjectRegistration Read(Document doc)
 {
  var schema=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(Id);
  if(schema!=null){var entity=doc.ProjectInformation.GetEntity(schema);if(entity.IsValid())return JsonSerializer.Deserialize<ProjectRegistration>(entity.Get<string>(schema.GetField("Payload")))??throw new InvalidDataException("Cadastro vazio.");}
  var stamp=StampData.Read(doc);
  return new(stamp.GetValueOrDefault("nome")??"",stamp.GetValueOrDefault("numero")??"",stamp.GetValueOrDefault("cliente")??"",stamp.GetValueOrDefault("endereco")??"","","","","","Não definido","Não definida","","");
 }
 public static void Save(Document doc,ProjectRegistration data)
 {
  if(doc.IsReadOnly||doc.IsFamilyDocument||doc.IsWorkshared)throw new InvalidOperationException("Use um RVT local editável para este protótipo.");
  if(string.IsNullOrWhiteSpace(data.Name)||string.IsNullOrWhiteSpace(data.City)||!System.Text.RegularExpressions.Regex.IsMatch(data.State,"^[A-Z]{2}$"))throw new InvalidOperationException("Preencha o nome do projeto, o município e a UF.");
  if(data.Floors.Length>0&&(!int.TryParse(data.Floors,out int floors)||floors<1))throw new InvalidOperationException("O número de pavimentos deve ser um inteiro positivo, ou ficar vazio.");
  using var tx=new Transaction(doc,"VEBRAM — cadastro do projeto");tx.Start();
  var schema=Schema();var entity=new Entity(schema);entity.Set(schema.GetField("Payload"),JsonSerializer.Serialize(data));doc.ProjectInformation.SetEntity(entity);
  foreach(var entry in new[]{(BuiltInParameter.PROJECT_NAME,data.Name),(BuiltInParameter.PROJECT_NUMBER,data.Code),(BuiltInParameter.CLIENT_NAME,data.Client),(BuiltInParameter.PROJECT_ADDRESS,data.Address)})
  {
   if(string.IsNullOrWhiteSpace(entry.Item2))continue;
   var parameter=doc.ProjectInformation.get_Parameter(entry.Item1);
   if(parameter==null||parameter.IsReadOnly)throw new InvalidOperationException("Campo do projeto indisponível: "+entry.Item1);
   parameter.Set(entry.Item2);
  }
  if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Cadastro revertido pelo Revit.");
 }
}
