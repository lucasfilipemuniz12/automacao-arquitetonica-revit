using System.Globalization;
namespace Vebram.Revit;

/// <summary>Suggestions only: saved deliverable values, including deliberately empty ones, take precedence.</summary>
internal static class ProjectDataIntegration
{
 internal static string CompactForDrawing(string text)
 {
  var lines=text.Replace("\r","").Split('\n').Where(s=>!string.IsNullOrWhiteSpace(s)).ToList();
  bool hasSource=lines.RemoveAll(s=>s.StartsWith("Fonte informada:",StringComparison.Ordinal))>0;
  if(hasSource)lines.Add("Fonte e condições completas: consultar Cadastro do projeto.");
  return string.Join("\n",lines);
 }
 internal static bool Matches(ProjectRegistration data)
 {
  var p=data.Profile;if(p==null)return false;
  bool Same(string a,string b)=>string.Equals(a.Trim(),b.Trim(),StringComparison.OrdinalIgnoreCase);
  return Same(data.City,p.City)&&Same(data.State,p.State)&&Same(data.Zone,p.Zone)&&
   (p.Use=="Não definido"||Same(data.Use,p.Use))&&(p.Intervention=="Não definida"||Same(data.Intervention,p.Intervention));
 }
 internal static ContextSelection Context(ProjectRegistration data,string key,long source,ContextSelection? saved,ContextSelection? other=null)
 {
  if(saved!=null)return saved; // Do not refill a field the user intentionally cleared.
  double? Rule(string name,bool percent)
  {
   if(!Matches(data)||!data.Profile!.Rules.TryGetValue(name,out var value))return null;
   return double.TryParse(value.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var n)&&double.IsFinite(n)&&n>=0&&(!percent||n<=100)?n:null;
  }
  var lot=other?.SourceId==source?other.Identification:data.Lot;
  return new(key,source,lot,MaximumOccupation:key=="urbanistico"?Rule("to",true):null,
   MaximumCoefficient:key=="urbanistico"?Rule("ca",false):null,MinimumPermeability:key=="urbanistico"?Rule("tp",true):null,
   RegistrationText:Summary(data));
 }
 internal static string Summary(ProjectRegistration data)
 {
  var lines=new List<string>();
  void Add(string label,string value){if(!string.IsNullOrWhiteSpace(value))lines.Add(label+": "+value.Trim());}
  Add("Município",string.Join(" / ",new[]{data.City,data.State}.Where(s=>!string.IsNullOrWhiteSpace(s))));
  Add("Zoneamento informado",data.Zone);
  if(data.Use!="Não definido")Add("Uso informado",data.Use);
  if(data.Intervention!="Não definida")Add("Intervenção informada",data.Intervention);
  if(data.Profile is {} p&&Matches(data))
  {Add("Perfil informado",p.Title+" · revisão "+p.Revision);Add("Fonte informada",p.Source);Add("Conferência informada",p.ReferenceDate);}
  else if(data.Profile!=null)lines.Add("Perfil diverge do cadastro: limites não sugeridos. Revise município, zona, uso e intervenção.");
  return string.Join("\n",lines);
 }
 internal static string Notes(ProjectRegistration data)=>string.Join("\n",new[]{
  string.IsNullOrWhiteSpace(data.Lot)?"":"Lote informado: "+data.Lot,Summary(data)}.Where(s=>s.Length>0));
 internal static IReadOnlyList<string> Pending(ContextSelection value)
 {
  var p=new List<string>();if(string.IsNullOrWhiteSpace(value.Identification))p.Add("identificação do lote");
  if(value.Key=="situacao")
  {if(string.IsNullOrWhiteSpace(value.LeftNeighbor))p.Add("confrontante esquerdo");if(string.IsNullOrWhiteSpace(value.RightNeighbor))p.Add("confrontante direito");if(string.IsNullOrWhiteSpace(value.RearNeighbor))p.Add("confrontante ao fundo");}
  else
  {if(value.ComputableArea==null)p.Add("área computável");if(value.PermeableArea==null)p.Add("área permeável");if(value.MaximumOccupation==null)p.Add("TO máxima");if(value.MaximumCoefficient==null)p.Add("CA máximo");if(value.MinimumPermeability==null)p.Add("TP mínima");}
  return p;
 }
}
