using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Vebram.Revit;

public sealed class App : IExternalApplication
{
    readonly string assembly = Assembly.GetExecutingAssembly().Location;
    int buttonIndex;

    public Result OnStartup(UIControlledApplication app)
    {
        // Cada aba do VEBRAM tem seu próprio método. Futuras abas (Projeto executivo, Modelagem)
        // entram aqui como BuildExecutivo(app) e BuildModelagem(app), sem mexer nas outras.
        BuildLegal(app);
        new SectionValidation().OnStartup(app);
        return Result.Succeeded;
    }

    static void EnsureTab(UIControlledApplication app, string tab)
    {
        // Reutiliza uma aba existente com esse nome sem esconder outros erros.
        try { app.CreateRibbonTab(tab); }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
    }

    void BuildLegal(UIControlledApplication app)
    {
        const string tab = "Projeto legal";
        EnsureTab(app, tab);
        // Painéis de versões antigas ficam ocultos (nomes com número de versão).
        foreach (var old in app.GetRibbonPanels(tab).Where(p => p.Name.StartsWith("VEBRAM ·") && !p.Name.StartsWith("VEBRAM · 1") && !p.Name.StartsWith("VEBRAM · 2") && !p.Name.StartsWith("VEBRAM · Avançado")))
            old.Visible = false;
        // Ordem de uso: 1 preparar o projeto, 2 gerar os entregáveis.
        var prepare = app.CreateRibbonPanel(tab, "VEBRAM · 1 Preparar o projeto");
        Add(prepare, "Parametros", "1 Configurar\nparâmetros", typeof(ConfigureCommand),
            "Passo 1. Adiciona os três parâmetros de classificação ARQ do V02 em vistas e folhas. Rode uma vez por projeto.");
        Add(prepare, "Diagnosticar", "2 Diagnosticar\nprojeto", typeof(DiagnoseCommand),
            "Passo 2. Confere o modelo contra o template V02 e lista o que falta. Somente leitura.");
        Add(prepare, "Preferencias", "3 Preferências\nlegais", typeof(LegalPreferencesCommand),
            "Passo 3. Estilo de cota, escala e informação adicional para este projeto.");
        var deliverables = app.CreateRibbonPanel(tab, "VEBRAM · 2 Entregáveis legais");
        Add(deliverables, "Preparar", "4 Preparar\nlegal", typeof(PrepareLegalCommand),
            "Passo 4. Escolha uma planta de modelagem e gere cada entregável: implantação, situação, plantas, cortes, fachadas, quadros e memória gráfica. Organizar folhas fica aqui dentro.");
        // Uso ocasional: fora do caminho principal.
        var advanced = app.CreateRibbonPanel(tab, "VEBRAM · Avançado");
        var menu = (PulldownButton)advanced.AddItem(new PulldownButtonData("Vebram_Avancado", "Avançado"));
        menu.ToolTip = "Ferramentas de teste do template. Não alteram o seu projeto.";
        menu.LargeImage = RibbonIcons.For("Demo"); menu.Image = menu.LargeImage;
        var demo = menu.AddPushButton(new PushButtonData("Vebram_Demo_" + buttonIndex++, "Demonstração do V02", assembly, typeof(DemoCommand).FullName));
        demo.ToolTip = "Cria um RVT novo a partir do template escolhido, para validar a base.";
    }

    void Add(RibbonPanel panel, string key, string label, Type command, string tooltip)
    {
        var button = (PushButton)panel.AddItem(new PushButtonData("Vebram_" + key + "_" + buttonIndex++,
            label, assembly, command.FullName));
        button.ToolTip = tooltip;
        button.LargeImage = RibbonIcons.For(key);
        button.Image = button.LargeImage;
        button.AvailabilityClassName = typeof(ProjectAvailable).FullName;
    }
    public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
}

public sealed class ProjectAvailable : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication app, CategorySet categories) =>
        app.ActiveUIDocument?.Document is { IsFamilyDocument: false };
}











