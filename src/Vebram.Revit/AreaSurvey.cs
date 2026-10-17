using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
namespace Vebram.Revit;

/// <summary>Read-only survey of what the model offers for a coloured area memory. Changes nothing; writes AREAS_LEVANTAMENTO.json.</summary>
internal static class AreaSurvey
{
 internal static void Run(Document doc,string output)
 {
  string Name(Element e)=>e.Name??"";
  double M2(double feet)=>Math.Round(feet*0.09290304,2);
  string LevelName(ElementId id)=>doc.GetElement(id)?.Name??"(sem nível)";
  var rooms=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Room>().Where(r=>r.Area>0)
   .Select(r=>new{r.Number,Nome=r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString(),Nivel=LevelName(r.LevelId),AreaM2=M2(r.Area),
    Departamento=r.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.AsString()}).OrderBy(r=>r.Nivel).ThenBy(r=>r.Number).ToList();
  object Group(BuiltInCategory category)=>new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType()
   .GroupBy(e=>LevelName(e.LevelId)+" | "+(doc.GetElement(e.GetTypeId())?.Name??Name(e))).OrderBy(g=>g.Key)
   .Select(g=>new{Grupo=g.Key,Quantidade=g.Count(),AreaM2=Math.Round(g.Sum(e=>e.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble()??0)*0.09290304,2)}).ToList();
  var roomParameters=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Room>().Where(r=>r.Area>0).Take(1)
   .SelectMany(r=>r.Parameters.Cast<Parameter>()).Select(p=>p.Definition.Name).Distinct().Order().ToList();
  var result=new
  {
   Aviso="Levantamento somente leitura. Nada foi alterado no modelo.",
   Ambientes=rooms,
   ParametrosDoAmbiente=roomParameters,
   Pisos=Group(BuiltInCategory.OST_Floors),
   Coberturas=Group(BuiltInCategory.OST_Roofs),
   Topografia=Group(BuiltInCategory.OST_Topography),
   Planta=Group(BuiltInCategory.OST_Site),
   Vegetacao=Group(BuiltInCategory.OST_Planting),
   Piscina_Deck_Candidatos=new FilteredElementCollector(doc).WhereElementIsNotElementType()
    .Where(e=>e.Category!=null&&(Name(e).Contains("iscina",StringComparison.OrdinalIgnoreCase)||Name(e).Contains("deck",StringComparison.OrdinalIgnoreCase)||(doc.GetElement(e.GetTypeId())?.Name??"").Contains("deck",StringComparison.OrdinalIgnoreCase)||(doc.GetElement(e.GetTypeId())?.Name??"").Contains("iscina",StringComparison.OrdinalIgnoreCase)))
    .Select(e=>new{e.Category!.Name,Tipo=doc.GetElement(e.GetTypeId())?.Name,Nivel=LevelName(e.LevelId),e.Id.Value}).Take(40).ToList(),
   EsquemasDeArea=new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Select(Name).ToList(),
   AreasDefinidas=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Area>().Where(a=>a.Area>0)
    .GroupBy(a=>a.AreaScheme?.Name+" | "+LevelName(a.LevelId)).Select(g=>new{g.Key,Quantidade=g.Count(),AreaM2=M2(g.Sum(a=>a.Area))}).ToList(),
   PlantasDeArea=new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(v=>v.ViewType==ViewType.AreaPlan).Select(v=>v.Name).ToList(),
   PreenchimentosExistentes=new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).Select(Name).ToList(),
   Niveis=new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l=>l.Elevation).Select(l=>new{l.Name,ElevacaoM=Math.Round(l.Elevation*0.3048,2)}).ToList()
  };
  File.WriteAllText(Path.Combine(output,"AREAS_LEVANTAMENTO.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
 }
}
