using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Vebram.Revit;

public sealed class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication app)
    {
        var assembly = Assembly.GetExecutingAssembly().Location;
        int buttonIndex = 0;
        foreach (var tab in new[] { "Projeto legal" })
        {
            // Reutiliza uma aba existente com esse nome sem esconder outros erros.
            try { app.CreateRibbonTab(tab); }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { }
            foreach (var old in app.GetRibbonPanels(tab).Where(p => p.Name is "VEBRAM · Base" or "VEBRAM · Entregáveis" or "VEBRAM · Projeto legal" or "VEBRAM · Entregáveis legais" or "VEBRAM · Projeto legal 0.3" or "VEBRAM · Entregáveis legais 0.3" or "VEBRAM · Projeto legal 0.3.1" or "VEBRAM · Entregáveis legais 0.3.1" or "VEBRAM · Projeto legal 0.3.2" or "VEBRAM · Entregáveis legais 0.3.2" or "VEBRAM · Projeto legal 0.3.3" or "VEBRAM · Entregáveis legais 0.3.3" or "VEBRAM · Projeto legal 0.3.4" or "VEBRAM · Entregáveis legais 0.3.4" or "VEBRAM · Projeto legal 0.3.5" or "VEBRAM · Entregáveis legais 0.3.5" or "VEBRAM · Projeto legal 0.4" or "VEBRAM · Entregáveis legais 0.4" or "VEBRAM · Projeto legal 0.4.1" or "VEBRAM · Entregáveis legais 0.4.1" or "VEBRAM · Projeto legal 0.4.2" or "VEBRAM · Entregáveis legais 0.4.2"))
                old.Visible = false;
            var panel = app.CreateRibbonPanel(tab, "VEBRAM · Projeto legal 0.5");
            Add(panel, "Diagnosticar", "Diagnosticar\nprojeto", typeof(DiagnoseCommand),
                "Inventaria o modelo e compara com o contrato do template V02. Somente leitura.");
            if (tab == "Projeto legal")
            {
                Add(panel, "Demo", "Demonstração\ndo V02", typeof(DemoCommand),
                    "Cria um RVT novo a partir do template escolhido, para validar a base.");
                Add(panel, "Parametros", "Configurar\nparâmetros", typeof(ConfigureCommand),
                    "Adiciona os três parâmetros de classificação ARQ do V02 em vistas e folhas.");
                Add(panel,"Preferencias","Preferências\nlegais",typeof(LegalPreferencesCommand),"Escolha estilo de cota, escala e informação adicional para este projeto.");
            }
            if (tab == "Projeto legal")
            {
                var deliverables = app.CreateRibbonPanel(tab, "VEBRAM · Entregáveis legais 0.5");
                Add(deliverables, "Preparar", "Preparar\nlegal", typeof(PrepareLegalCommand),
                    "Escolha uma planta de modelagem e clique em um entregável. Piloto funcional: Implantação com cotas e detalhamento.");
            }
        }
        new SectionValidation().OnStartup(app);
        return Result.Succeeded;

        void Add(RibbonPanel panel, string key, string label, Type command, string tooltip)
        {
            var button = (PushButton)panel.AddItem(new PushButtonData("Vebram_" + key + "_" + buttonIndex++,
                label, assembly, command.FullName));
            button.ToolTip = tooltip;
            button.LargeImage=RibbonIcons.For(key);
            button.Image=button.LargeImage;
            if (command != typeof(DemoCommand)) button.AvailabilityClassName = typeof(ProjectAvailable).FullName;
        }
    }
    public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
}

public sealed class ProjectAvailable : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication app, CategorySet categories) =>
        app.ActiveUIDocument?.Document is { IsFamilyDocument: false };
}











