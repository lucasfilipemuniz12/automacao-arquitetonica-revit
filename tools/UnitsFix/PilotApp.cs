using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
namespace Vebram.ImplantationPilot;
public sealed class PilotApp : IExternalApplication
{
    record Job(string Source,string Destination,string Output);
    string jobPath="";
    public Result OnStartup(UIControlledApplication app)
    {jobPath=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,"units-job.json");if(File.Exists(jobPath))app.Idling+=Run;return Result.Succeeded;}
    public Result OnShutdown(UIControlledApplication app)=>Result.Succeeded;
    void Run(object? sender,IdlingEventArgs e)
    {
        if(sender is not UIApplication ui)return;
        ui.Idling-=Run;if(!File.Exists(jobPath))return;
        var job=JsonSerializer.Deserialize<Job>(File.ReadAllText(jobPath))!;
        File.Move(jobPath,jobPath+".executado",false);
        Document? doc=null;
        try
        {
            var root=Path.GetFullPath(job.Output).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(job.Source).StartsWith(root,StringComparison.OrdinalIgnoreCase)||
                !Path.GetFullPath(job.Destination).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Arquivo fora da pasta de correção.");
            if(File.Exists(job.Destination))throw new Exception("Destino já existe. Preservar antes de continuar.");
            doc=ui.Application.OpenDocumentFile(job.Source);
            object Snapshot()=>new {
                Document=doc.PathName,
                Units=Units.GetModifiableSpecs().Select(spec=>new{Spec=spec.TypeId,Unit=doc.GetUnits().GetFormatOptions(spec).GetUnitTypeId().TypeId,Accuracy=doc.GetUnits().GetFormatOptions(spec).Accuracy}).ToList(),
                Dimensions=new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>().Select(t=>new{t.Name,Id=t.Id.Value,Style=t.StyleType.ToString(),Format=Format(t)}).ToList(),
                Schedules=new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().Select(v=>new{v.Name,Fields=v.Definition.GetFieldOrder().Select(id=>{var f=v.Definition.GetField(id);try{return new {Name=f.GetName(),Spec=f.GetSpecTypeId().TypeId,Format=Describe(f.GetFormatOptions())};}catch{return new{Name=f.GetName(),Spec="",Format="indisponível"};}}).ToList()}).ToList()
            };
            Save("ANTES.json",Snapshot());var changes=new List<string>();
            using(var tx=new Transaction(doc,"Corrigir unidades e estilos coerentes com seus nomes"))
            {
                tx.Start();var units=doc.GetUnits();
                Ensure(SpecTypeId.Length,UnitTypeId.Meters,.01,"Comprimento geral");
                Ensure(SpecTypeId.Area,UnitTypeId.SquareMeters,.01,"Área geral");
                Ensure(SpecTypeId.Volume,UnitTypeId.CubicMeters,.01,"Volume geral");
                doc.SetUnits(units);
                foreach(var type in new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Cast<DimensionType>())
                {
                    if(type.StyleType is DimensionStyleType.Angular or DimensionStyleType.SpotSlope)continue;
                    var expected=UnitTypeId.Meters;
                    var old=type.GetUnitsFormatOptions();
                    var effective=old.UseDefault?units.GetFormatOptions(SpecTypeId.Length):old;
                    var accuracy=.01;
                    var fresh=new FormatOptions(expected){UseDefault=false,Accuracy=accuracy,SuppressTrailingZeros=false};
                    type.SetUnitsFormatOptions(fresh);
                    type.SetAlternateUnitsFormatOptions(new FormatOptions(UnitTypeId.Meters){UseDefault=false,Accuracy=.01});
                    changes.Add(type.Name+": "+Describe(old)+" → "+Describe(fresh));
                    var renamed=Regex.Replace(type.Name,@"\b(cotas?)\s+cm\b","$1 m",RegexOptions.IgnoreCase);
                    if(renamed!=type.Name){changes.Add("Nome: "+type.Name+" → "+renamed);type.Name=renamed;}
                }
                foreach(var table in new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>())
                    foreach(var id in table.Definition.GetFieldOrder())
                    {
                        var field=table.Definition.GetField(id);var spec=field.GetSpecTypeId();
                        ForgeTypeId? unit=spec==SpecTypeId.Length?UnitTypeId.Meters:spec==SpecTypeId.Area?UnitTypeId.SquareMeters:spec==SpecTypeId.Volume?UnitTypeId.CubicMeters:null;
                        if(unit==null)continue;
                        field.SetFormatOptions(new FormatOptions(unit){UseDefault=false,Accuracy=.01});
                        if(spec==SpecTypeId.Length)
                        {
                            var heading=Regex.Replace(field.ColumnHeading,@"\bcm\b|\bmm\b","m",RegexOptions.IgnoreCase);
                            if(!Regex.IsMatch(heading,@"\bm\b"))heading+=" (m)";
                            field.ColumnHeading=heading;
                        }
                        changes.Add("Tabela "+table.Name+" / "+field.GetName()+": "+unit.TypeId);
                    }
                if(tx.Commit()!=TransactionStatus.Committed)throw new Exception("Transação não confirmada.");
                void Ensure(ForgeTypeId spec,ForgeTypeId unit,double accuracy,string title)
                {var old=units.GetFormatOptions(spec);if(old.GetUnitTypeId()==unit)return;var fresh=new FormatOptions(unit){UseDefault=false,Accuracy=accuracy};units.SetFormatOptions(spec,fresh);changes.Add(title+": "+Describe(old)+" → "+Describe(fresh));}
            }
            Save("DEPOIS.json",Snapshot());Save("ALTERACOES.json",changes);
            doc.SaveAs(job.Destination,new SaveAsOptions{OverwriteExistingFile=false,MaximumBackups=1});doc.Close(false);doc=null;
            File.WriteAllText(Path.Combine(job.Output,"CONCLUIDO.txt"),"Template corrigido e salvo pela API Revit.\n"+job.Destination+"\n"+string.Join("\n",changes));
            void Save(string name,object value)=>File.WriteAllText(Path.Combine(job.Output,name),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(job.Output,"ERRO.txt"),ex.ToString());}
        finally{if(doc!=null&&doc.IsValidObject)doc.Close(false);}
    }
    static ForgeTypeId? Expected(string name)
    {
        // "2,5 mm Arial" descreve a fonte, não a unidade da distância.
        if(!name.StartsWith("ARQ",StringComparison.OrdinalIgnoreCase))return null;
        if(Regex.IsMatch(name,@"\bmm\b",RegexOptions.IgnoreCase))return UnitTypeId.Millimeters;
        if(Regex.IsMatch(name,@"\bcm\b",RegexOptions.IgnoreCase))return UnitTypeId.Centimeters;
        if(Regex.IsMatch(name,@"\bm\b|\bmetros?\b",RegexOptions.IgnoreCase))return UnitTypeId.Meters;
        return null;
    }
    static string Format(DimensionType t){try{return Describe(t.GetUnitsFormatOptions());}catch(Exception e){return e.Message;}}
    static string Describe(FormatOptions f)=>f.UseDefault?"Unidades gerais do projeto":f.GetUnitTypeId().TypeId+" / precisão "+f.Accuracy;
}
