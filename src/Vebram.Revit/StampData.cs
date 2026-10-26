using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>
/// Fills the data the V02 title block already reads (Project Information and sheet fields) with what the user types.
/// Nothing is invented: empty boxes are left untouched, and placeholder text such as "A PREENCHER" shows as empty in the form.
/// </summary>
public static class StampData
{
 public sealed record StampField(string Key,string Label,string Help,bool Project,BuiltInParameter Parameter);
 public static readonly StampField[] Fields=
 {
  new("nome","Nome do projeto","Aparece no carimbo de todas as folhas.",true,BuiltInParameter.PROJECT_NAME),
  new("numero","Número do projeto","Código interno ou número do processo.",true,BuiltInParameter.PROJECT_NUMBER),
  new("cliente","Nome do cliente","Proprietário ou contratante.",true,BuiltInParameter.CLIENT_NAME),
  new("endereco","Endereço do projeto","Rua, número, bairro e cidade.",true,BuiltInParameter.PROJECT_ADDRESS),
  new("autor","Autor do projeto","Profissional ou escritório responsável.",true,BuiltInParameter.PROJECT_AUTHOR),
  new("emissao","Data de emissão","Digite a data como quer vê-la no carimbo (por exemplo 15/10/2026).",true,BuiltInParameter.PROJECT_ISSUE_DATE),
  new("projetado","Projetado por","Vale para as folhas legais (LEG).",false,BuiltInParameter.SHEET_DESIGNED_BY),
  new("desenhado","Desenhado por","Vale para as folhas legais (LEG).",false,BuiltInParameter.SHEET_DRAWN_BY),
  new("verificado","Verificado por","Vale para as folhas legais (LEG).",false,BuiltInParameter.SHEET_CHECKED_BY),
  new("aprovado","Aprovado por","Vale para as folhas legais (LEG).",false,BuiltInParameter.SHEET_APPROVED_BY)
 };
 static readonly string[] Placeholders={"A PREENCHER","Data de emissão","Status do projeto","Projetista"};
 static bool IsPlaceholder(string value)=>string.IsNullOrWhiteSpace(value)||Placeholders.Any(p=>string.Equals(p,value.Trim(),StringComparison.OrdinalIgnoreCase));
 public static List<ViewSheet> LegalSheetsOf(Document doc)=>new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(LegalSheets.IsLegalSheet).OrderBy(s=>s.SheetNumber).ToList();
 /// <summary>Current values for the form, with placeholders shown as empty.</summary>
 public static Dictionary<string,string> Read(Document doc)
 {
  var sheet=LegalSheetsOf(doc).FirstOrDefault();var result=new Dictionary<string,string>();
  foreach(var field in Fields)
  {
   Element? holder=field.Project?doc.ProjectInformation:sheet;var value=holder?.get_Parameter(field.Parameter)?.AsString()??"";
   result[field.Key]=IsPlaceholder(value)?"":value;
  }
  return result;
 }
 /// <summary>Writes only the filled boxes. Returns a plain-language report.</summary>
 public static string Apply(Document doc,Dictionary<string,string> values)
 {
  if(doc.IsReadOnly||doc.IsFamilyDocument)throw new InvalidOperationException("Use um RVT local editável.");
  var sheets=LegalSheetsOf(doc);int projectFields=0,sheetFields=0;var skipped=new List<string>();
  using var tx=new Transaction(doc,"VEBRAM — dados do carimbo");tx.Start();
  try
  {
   foreach(var field in Fields)
   {
    var text=values.GetValueOrDefault(field.Key)?.Trim()??"";if(text.Length==0)continue;
    if(field.Project)
    {
     var parameter=doc.ProjectInformation.get_Parameter(field.Parameter);
     if(parameter==null||parameter.IsReadOnly){skipped.Add(field.Label);continue;}
     if(parameter.AsString()!=text){parameter.Set(text);projectFields++;}
    }
    else foreach(var sheet in sheets)
    {
     var parameter=sheet.get_Parameter(field.Parameter);
     if(parameter==null||parameter.IsReadOnly){if(!skipped.Contains(field.Label))skipped.Add(field.Label);continue;}
     if(parameter.AsString()!=text){parameter.Set(text);sheetFields++;}
    }
   }
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Dados do carimbo revertidos pelo Revit.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
  return $"{projectFields} campo(s) do projeto e {sheetFields} campo(s) em {sheets.Count} folha(s) legal(is) atualizados. Campos vazios não foram alterados."+
   (skipped.Count>0?"\nNão foi possível gravar: "+string.Join(", ",skipped)+".":"")+"\nO carimbo mostra esses dados automaticamente; as folhas EX e PRE não foram alteradas.";
 }
}
