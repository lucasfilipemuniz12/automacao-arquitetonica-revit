using Autodesk.Revit.DB;

namespace Vebram.Revit;

public sealed record Deliverable(string Key, string Label, string Role, ViewType Type,
    string ReferenceName, string? ScheduleToken = null);

public static class LegalCatalog
{
    public const string Stage = "02 PROJETO LEGAL";
    public static readonly Guid FunctionId = new("e77358af-9299-4c0d-9c8f-0d2443295a55");
    public static readonly Guid StageId = new("d3c5bf6c-c2ad-4cba-a6b6-c743ddcf3db3");
    public static readonly Guid StatusId = new("d4fb1f37-ce6c-46a7-8628-88a5acb22078");

    // Estes nomes vêm do registro final do V02, não de IDs históricos do RVT.
    public static readonly IReadOnlyList<Deliverable> Items = new[]
    {
        new Deliverable("planta-1", "Planta — nível 1", "LEGAL_PLANTA", ViewType.FloorPlan, "LEGAL - Planta baixa / Nível 1"),
        new Deliverable("planta-2", "Planta — nível 2", "LEGAL_PLANTA", ViewType.FloorPlan, "LEGAL - Planta baixa / Nível 2"),
        new Deliverable("implantacao", "Implantação", "IMPLANTACAO", ViewType.FloorPlan, "LEGAL - Implantação"),
        new Deliverable("situacao", "Situação", "SITUACAO", ViewType.FloorPlan, "LEGAL - Situação"),
        new Deliverable("cobertura", "Cobertura", "COBERTURA", ViewType.FloorPlan, "LEGAL - Cobertura / Nível 3"),
        new Deliverable("corte-a", "Corte A", "CORTES", ViewType.Section, "LEGAL - Corte A"),
        new Deliverable("corte-b", "Corte B", "CORTES", ViewType.Section, "LEGAL - Corte B"),
        new Deliverable("fachada-norte", "Fachada norte", "FACHADAS", ViewType.Elevation, "LEGAL - Fachada Norte"),
        new Deliverable("fachada-sul", "Fachada sul", "FACHADAS", ViewType.Elevation, "LEGAL - Fachada Sul"),
        new Deliverable("fachada-leste", "Fachada leste", "FACHADAS", ViewType.Elevation, "LEGAL - Fachada Leste"),
        new Deliverable("fachada-oeste", "Fachada oeste", "FACHADAS", ViewType.Elevation, "LEGAL - Fachada Oeste"),
        new Deliverable("portas", "Quadro de portas", "QUADROS", ViewType.Schedule, OpeningSchedules.Doors), new Deliverable("janelas", "Quadro de janelas", "QUADROS", ViewType.Schedule, OpeningSchedules.Windows), new Deliverable("areas", "Q04 — áreas dos ambientes", "QUADROS", ViewType.Schedule,
            "02 PROJETO LEGAL - Q04 - Áreas dos ambientes", "Q04"),
        new Deliverable("areas-descobertas", "Áreas descobertas — fora do total útil", "QUADROS", ViewType.Schedule, AreaScheduleTotals.UncoveredName),
        new Deliverable("memoria", "Memória gráfica de áreas", "MEMORIA_AREAS", ViewType.FloorPlan, MemoryAreas.ViewName),
        new Deliverable("urbanistico", "Quadro urbanístico — preencher", "QUADROS", ViewType.DraftingView,
            "LEGAL - Quadro urbanístico / preencher")
    };

    public static string Normalize(string? value) => string.Join(" ", (value ?? "").Split(
        (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    public static bool Same(string? a, string? b) => Normalize(a).Equals(Normalize(b), StringComparison.OrdinalIgnoreCase);
    public static string Text(Element element, Guid id) => element.get_Parameter(id)?.AsString() ?? "";
    public static bool IsLegal(View view) => Same(Text(view, StageId), Stage) ||
        (view is ViewSchedule && Items.Any(item=>item.Type==ViewType.Schedule&&Same(view.Name,item.ReferenceName)));
    public static bool HasRole(View view, Deliverable item) => Same(Text(view, FunctionId), item.Role);
    public static bool Compatible(View view, Deliverable item) => view.ViewType == item.Type ||
        (item.Role == "FACHADAS" && view.ViewType == ViewType.Section);
    public static List<View> Views(Document doc) => new FilteredElementCollector(doc).OfClass(typeof(View))
        .Cast<View>().Where(v => !v.IsTemplate && v.ViewType != ViewType.DrawingSheet).ToList();
    public static View? Reference(IEnumerable<View> views, Deliverable item)
    {
        var candidates=views.ToList();
        if(item.Key=="urbanistico")
        {
            var generated=candidates.FirstOrDefault(v=>Compatible(v,item)&&Same(v.Name,item.ReferenceName+" — VEBRAM"));
            if(generated!=null)return generated;
        }
        return candidates.FirstOrDefault(v=>Compatible(v,item)&&Same(v.Name,item.ReferenceName));
    }
}
