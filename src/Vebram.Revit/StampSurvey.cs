using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>Read-only survey of project information, sheets and title blocks: which fields exist and which are filled. Changes nothing.</summary>
internal static class StampSurvey
{
 internal static void Run(Document doc,string output)
 {
  static string Value(Parameter p)=>p.StorageType switch{StorageType.String=>p.AsString()??"",StorageType.Integer=>p.AsInteger().ToString(),StorageType.Double=>p.AsValueString()??p.AsDouble().ToString(),StorageType.ElementId=>p.AsValueString()??"",_=>""};
  static object Parameters(Element e)=>e.Parameters.Cast<Parameter>().Where(p=>p.HasValue&&!string.IsNullOrWhiteSpace(Value(p)))
   .Select(p=>new{Nome=p.Definition.Name,Valor=Value(p),SoLeitura=p.IsReadOnly}).OrderBy(p=>p.Nome).ToList();
  var sheets=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().OrderBy(s=>s.SheetNumber).ToList();
  var result=new
  {
   Aviso="Levantamento somente leitura. Nada foi alterado no modelo.",
   InformacoesDoProjeto=Parameters(doc.ProjectInformation),
   Folhas=sheets.Select(s=>new
   {
    s.SheetNumber,s.Name,Parametros=Parameters(s),
    Carimbo=new FilteredElementCollector(doc,s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().Cast<FamilyInstance>()
     .Select(t=>new{Familia=t.Symbol.FamilyName,Tipo=t.Name,Parametros=Parameters(t)}).ToList(),
    Quantidade=new FilteredElementCollector(doc,s.Id).WhereElementIsNotElementType().Count()
   }).ToList(),
   FamiliasDeCarimbo=new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType().Cast<FamilySymbol>().Select(t=>t.FamilyName+" : "+t.Name).Distinct().Order().ToList()
  };
  File.WriteAllText(Path.Combine(output,"CARIMBO_LEVANTAMENTO.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
 }
}

internal static class StampDataValidation
{
 /// <summary>On a copy only: writes obviously fictitious test values, reads them back and checks that empty boxes change nothing.</summary>
 internal static void Run(Document doc,string output)
 {
  var before=StampData.Read(doc);
  var report=StampData.Apply(doc,new(){["nome"]="TESTE — nome do projeto",["desenhado"]="TESTE — desenhista",["cliente"]=""});
  var after=StampData.Read(doc);
  if(after["nome"]!="TESTE — nome do projeto"||after["desenhado"]!="TESTE — desenhista")throw new Exception("Valores não foram gravados.");
  if(after["endereco"]!=before["endereco"]||after["cliente"]!=before["cliente"])throw new Exception("Campo vazio foi alterado.");
  var ex=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().First(s=>s.SheetNumber.StartsWith("EX-"));
  if((ex.get_Parameter(BuiltInParameter.SHEET_DRAWN_BY)?.AsString()??"")=="TESTE — desenhista")throw new Exception("Folha EX foi alterada.");
  File.WriteAllText(Path.Combine(output,"RELATORIO.txt"),report+"\nLeitura de volta: nome="+after["nome"]+"; desenhado="+after["desenhado"]);
  doc.Save();
 }
}