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
        var project = app.CreateRibbonPanel(tab, "VEBRAM · 1 Dados do projeto");
        Add(project,"Cadastro","Cadastrar\nprojeto",typeof(RegisterProjectCommand),"Comece aqui: identifique a casa, o lote, o uso e a intervenção. Os dados ficam no RVT; o perfil municipal é escolhido na biblioteca.");
        var municipalities=app.CreateRibbonPanel(tab,"VEBRAM · 2 Municípios e regras");
        Add(municipalities,"Municipios","Biblioteca\nmunicipal",typeof(MunicipalProfilesCommand),"Cadastre perfis com município, zoneamento, uso, intervenção, parâmetros e fontes. Reutilize em outros projetos deste computador. Regras manuais, sem verificação legal automática.");
        var deliverables = app.CreateRibbonPanel(tab, "VEBRAM · 3 Preparar entregáveis");
        Add(deliverables, "Preparar", "Preparar\nentregáveis", typeof(PrepareLegalCommand),
            "Plantas, cortes, fachadas, quadros e organização de folhas. O cadastro não bloqueia a preparação das vistas.");
        var menu=(PulldownButton)deliverables.AddItem(new PulldownButtonData("Vebram_Ferramentas","Ferramentas"));
        menu.LargeImage=RibbonIcons.For("Preferencias");menu.Image=menu.LargeImage;
        void Tool(string text,Type type){var item=menu.AddPushButton(new PushButtonData("Vebram_Tool_"+buttonIndex++,text,assembly,type.FullName));item.AvailabilityClassName=typeof(ProjectAvailable).FullName;}
        Tool("Configurar parâmetros do V02",typeof(ConfigureCommand));Tool("Diagnosticar projeto",typeof(DiagnoseCommand));Tool("Preferências legais",typeof(LegalPreferencesCommand));Tool("Demonstração do V02",typeof(DemoCommand));
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











