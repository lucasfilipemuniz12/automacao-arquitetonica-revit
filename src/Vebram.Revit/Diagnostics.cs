using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Text;
using System.Text.Json;

namespace Vebram.Revit;

public sealed record Finding(string Severity, string Subject, string Detail, string? ElementId = null);
public sealed record ViewEvidence(string Name, string UniqueId, string Type, string Stage, string Role,
    int Scale, string? Level, string Template);
public sealed record DiagnosticReport(string Profile, DateTimeOffset Time, string RevitVersion,
    string Document, string DocumentPath, bool IsWorkshared, IReadOnlyList<Finding> Findings,
    IReadOnlyList<ViewEvidence> Views);

public static class Diagnostics
{
    public static DiagnosticReport Inspect(Document doc)
    {
        var findings = new List<Finding>();
        var views = LegalCatalog.Views(doc);
        // GUID, tipo e binding são necessários: um parâmetro homônimo não substitui o contrato.
        CheckParameter(LegalCatalog.FunctionId, "ARQ Funcao", [BuiltInCategory.OST_Views, BuiltInCategory.OST_Sheets]);
        CheckParameter(LegalCatalog.StageId, "ARQ Etapa", [BuiltInCategory.OST_Views, BuiltInCategory.OST_Sheets]);
        CheckParameter(LegalCatalog.StatusId, "ARQ Status", [BuiltInCategory.OST_Sheets]);
        foreach (var item in LegalCatalog.Items)
        {
            var view = LegalCatalog.Reference(views, item);
            if (view == null)
                findings.Add(new("PENDENTE", item.Label, "Referência V02 ausente ou renomeada. Escolha uma fonte compatível no formulário."));
            else
            {
                bool roleOk = item.Type == ViewType.Schedule || LegalCatalog.HasRole(view, item);
                // Schedules do V02 são agrupadas por prefixo, não necessariamente por parâmetros.
                bool stageOk = item.Type == ViewType.Schedule || LegalCatalog.IsLegal(view);
                findings.Add(new(roleOk && stageOk ? "ENCONTRADO" : "CONFLITO", item.Label,
                    roleOk && stageOk ? "Referência localizada; conteúdo e enquadramento requerem conferência no projeto." :
                    "Nome reconhecido, mas etapa/função não correspondem ao contrato.", view.UniqueId));
            }
        }
        foreach (var number in new[] { "LEG-01", "LEG-02" })
        {
            var sheet = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
                .FirstOrDefault(s => s.SheetNumber == number);
            findings.Add(new(sheet == null ? "PENDENTE" : "ENCONTRADO", number,
                sheet == null ? "Folha de referência ausente." : "Folha localizada; guias e composição não certificadas.", sheet?.UniqueId));
        }
        var titleblocks = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks)
            .WhereElementIsElementType().Cast<FamilySymbol>().ToList();
        foreach (var format in new[] { "A1", "A0" })
            findings.Add(new(titleblocks.Any(t => t.FamilyName == "ARQ Carimbo " + format) ? "ENCONTRADO" : "PENDENTE",
                "Carimbo " + format, "Procura a família ARQ Carimbo " + format + "; não mede suas margens nesta versão."));
        foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(x => x.Elevation))
            findings.Add(new("INFORMAÇÃO", "Nível", $"{level.Name}: {UnitUtils.ConvertFromInternalUnits(level.Elevation, UnitTypeId.Meters):0.###} m", level.UniqueId));
        var units = doc.GetUnits();
        findings.Add(new("INFORMAÇÃO", "Unidades gerais", "Comprimento: " + units.GetFormatOptions(SpecTypeId.Length).GetUnitTypeId().TypeId +
            "; área: " + units.GetFormatOptions(SpecTypeId.Area).GetUnitTypeId().TypeId + ". Formatos individuais de cotas e tabelas ainda exigem conferência."));
        foreach (var category in new[] { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows })
        {
            var elements = new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType().ToList();
            foreach (var e in elements.Where(e => string.IsNullOrWhiteSpace(e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString())))
                findings.Add(new("PENDENTE", "Marca de esquadria", $"{e.Name}: marca vazia.", e.UniqueId));
            foreach (var group in elements.GroupBy(e => e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString())
                         .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1))
                findings.Add(new("REVISAR", "Marca repetida", $"{group.Key}: {group.Count()} ocorrências na categoria {category}. Verificar intenção de agrupamento."));
        }
        int roomCount = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).Cast<SpatialElement>().OfType<Room>().Count();
        findings.Add(new(roomCount == 0 ? "PENDENTE" : "INFORMAÇÃO", "Áreas de ambientes", $"{roomCount} ambientes. Q04 não equivale a memória de áreas urbanísticas."));
        findings.Add(new("PENDENTE", "Dados legais", "Confirmar lote, levantamento, município, critérios de áreas, norte e responsáveis. Este diagnóstico não verifica legislação."));
        var evidence = views.Select(v => new ViewEvidence(v.Name, v.UniqueId, v.ViewType.ToString(),
            LegalCatalog.Text(v, LegalCatalog.StageId), LegalCatalog.Text(v, LegalCatalog.FunctionId),
            v.Scale, v.GenLevel?.Name, (doc.GetElement(v.ViewTemplateId) as View)?.Name ?? "Sem preset")).ToList();
        return new("ARQ_AUTOMACAO_V02 / legal / 0.1", DateTimeOffset.Now, doc.Application.VersionNumber + " / " + doc.Application.VersionBuild,
            doc.Title, doc.PathName, doc.IsWorkshared, findings, evidence);

        void CheckParameter(Guid guid, string name, BuiltInCategory[] categories)
        {
            var parameter = SharedParameterElement.Lookup(doc, guid);
            if (parameter == null) { findings.Add(new("BLOQUEIO", name, "GUID ausente: " + guid)); return; }
            var definition = parameter.GetDefinition();
            var binding = doc.ParameterBindings.get_Item(definition) as InstanceBinding;
            if (definition.GetDataType() != SpecTypeId.String.Text || binding == null)
            { findings.Add(new("BLOQUEIO", name, "Esperado parâmetro de texto com binding de instância.")); return; }
            foreach (var category in categories)
            {
                bool found = binding.Categories.Cast<Category>().Any(c => c.Id == new ElementId(category));
                findings.Add(new(found ? "ENCONTRADO" : "BLOQUEIO", name,
                    "Binding " + category + (found ? " presente." : " ausente.")));
            }
        }
    }

    public static string Markdown(DiagnosticReport report)
    {
        var text = new StringBuilder($"# Diagnóstico VEBRAM\n\nDocumento: {report.Document}\nRevit: {report.RevitVersion}\nPerfil: {report.Profile}\nData: {report.Time:O}\n\n");
        foreach (var f in report.Findings)
            text.AppendLine($"- **{f.Severity} — {f.Subject}:** {f.Detail}" + (f.ElementId == null ? "" : $" [UniqueId: {f.ElementId}]"));
        return text.ToString();
    }
    public static string Json(DiagnosticReport report) => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
}
