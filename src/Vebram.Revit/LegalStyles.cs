using Autodesk.Revit.DB;
namespace Vebram.Revit;

public static class LegalStyles
{
 public static TextNoteType Text(Document doc,double millimeters=2.5,bool bold=false)
 {
  var types=new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().OrderBy(t=>t.Name,StringComparer.Ordinal).ToList();
  bool Fits(TextNoteType t)=>Math.Abs(t.get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble()-Implantation.Feet(millimeters/1000))<1e-7&&(t.get_Parameter(BuiltInParameter.TEXT_STYLE_BOLD)?.AsInteger()==1)==bold;
  var exact=types.FirstOrDefault(t=>(t.Name=="ARQ A1 texto 2.5"||t.Name=="ARQ A1 texto 2,5")&&Fits(t));if(exact!=null)return exact;
  var arq=types.FirstOrDefault(t=>t.Name.StartsWith("ARQ",StringComparison.OrdinalIgnoreCase)&&!t.Name.Contains("VEBRAM",StringComparison.OrdinalIgnoreCase)&&Fits(t));if(arq!=null)return arq;
  string name="VEBRAM — texto "+millimeters.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+" mm"+(bold?" negrito":"");
  var existing=types.FirstOrDefault(t=>t.Name==name&&Fits(t));if(existing!=null)return existing;
  var source=types.FirstOrDefault(t=>t.Name.StartsWith("ARQ",StringComparison.OrdinalIgnoreCase))??types.FirstOrDefault()??throw new InvalidOperationException("Nenhum tipo de texto disponível.");
  if(types.Any(t=>t.Name==name))throw new InvalidOperationException("O estilo "+name+" foi editado; confira antes de gerar.");
  var created=(TextNoteType)source.Duplicate(name);created.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(Implantation.Feet(millimeters/1000));created.get_Parameter(BuiltInParameter.TEXT_STYLE_BOLD).Set(bold?1:0);return created;
 }
 public static DimensionType Dimension(Document doc)
 {
  var types=new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(t=>t.StyleType==DimensionStyleType.Linear).OrderBy(t=>t.Name,StringComparer.Ordinal).ToList();
  var preferred=types.FirstOrDefault(t=>t.Name.Equals("ARQ cotas metros",StringComparison.OrdinalIgnoreCase));if(preferred!=null&&Meters(preferred))return preferred;
  bool Meters(DimensionType t){var format=t.GetUnitsFormatOptions();if(format.UseDefault)format=doc.GetUnits().GetFormatOptions(SpecTypeId.Length);return format.GetUnitTypeId()==UnitTypeId.Meters&&format.Accuracy<=.0100001;}
  var arq=types.FirstOrDefault(t=>t.Name.StartsWith("ARQ",StringComparison.OrdinalIgnoreCase)&&!t.Name.Contains("VEBRAM",StringComparison.OrdinalIgnoreCase)&&Meters(t));if(arq!=null)return arq;
  const string name="VEBRAM — cotas metros";var existing=types.FirstOrDefault(t=>t.Name==name);if(existing!=null)return existing;
  var source=types.FirstOrDefault(t=>t.Name.StartsWith("ARQ",StringComparison.OrdinalIgnoreCase))??types.FirstOrDefault()??throw new InvalidOperationException("Nenhum estilo de cota linear disponível.");
  var created=(DimensionType)source.Duplicate(name);created.SetUnitsFormatOptions(new FormatOptions(UnitTypeId.Meters){UseDefault=false,Accuracy=.01,SuppressTrailingZeros=false});return created;
 }
}
