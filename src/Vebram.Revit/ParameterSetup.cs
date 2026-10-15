using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace Vebram.Revit;

public static class ParameterSetup
{
    // Deve ser chamado dentro de uma transação. Não grava valores cadastrais ou municipais.
    public static void Bind(Document doc)
    {
        string profile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "profiles", "ARQ_Parametros_Compartilhados.txt");
        string previous = doc.Application.SharedParametersFilename;
        try
        {
            doc.Application.SharedParametersFilename = profile;
            var file = doc.Application.OpenSharedParameterFile() ?? throw new InvalidOperationException("Arquivo de parâmetros do V02 indisponível.");
            var definitions = file.Groups.Cast<DefinitionGroup>().SelectMany(g => g.Definitions.Cast<ExternalDefinition>()).ToList();
            foreach (var (guid, name, forViews) in new[] { (LegalCatalog.FunctionId, "ARQ Funcao", true),
                (LegalCatalog.StageId, "ARQ Etapa", true), (LegalCatalog.StatusId, "ARQ Status", false) })
            {
                if (SharedParameterElement.Lookup(doc, guid) != null) continue;
                var namesake = new FilteredElementCollector(doc).OfClass(typeof(ParameterElement)).Cast<ParameterElement>()
                    .FirstOrDefault(p => p.Name == name);
                if (namesake != null) throw new InvalidOperationException(name + ": existe um parâmetro homônimo com identidade diferente. Revisar o conflito.");
                var definition = definitions.Single(d => d.GUID == guid);
                var categories = doc.Application.Create.NewCategorySet();
                categories.Insert(Category.GetCategory(doc, BuiltInCategory.OST_Sheets));
                if (forViews) categories.Insert(Category.GetCategory(doc, BuiltInCategory.OST_Views));
                if (!doc.ParameterBindings.Insert(definition, doc.Application.Create.NewInstanceBinding(categories), GroupTypeId.IdentityData))
                    throw new InvalidOperationException("Não foi possível vincular " + name);
            }
        }
        finally { doc.Application.SharedParametersFilename = previous; }
    }
}

[Transaction(TransactionMode.Manual)]
public sealed class ConfigureCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
        var doc = data.Application.ActiveUIDocument?.Document;
        if (doc == null || doc.IsFamilyDocument || doc.IsReadOnly || doc.IsWorkshared || doc.PathName.EndsWith(".rte", StringComparison.OrdinalIgnoreCase))
        { message = "Use uma cópia RVT local editável."; return Result.Failed; }
        var confirmation = new TaskDialog("VEBRAM — configurar parâmetros")
        {
            MainInstruction = "Adicionar os parâmetros de classificação do V02?",
            MainContent = "ARQ Etapa e ARQ Funcao em vistas/folhas; ARQ Status em folhas. Identidades existentes são preservadas.",
            CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
            DefaultButton = TaskDialogResult.Cancel
        };
        if (confirmation.Show() != TaskDialogResult.Ok) return Result.Cancelled;
        using var transaction = new Transaction(doc, "VEBRAM — parâmetros da base");
        try
        {
            transaction.Start(); ParameterSetup.Bind(doc);
            if (transaction.Commit() != TransactionStatus.Committed) throw new InvalidOperationException("Configuração não concluída.");
            TaskDialog.Show("VEBRAM", "Parâmetros configurados. Execute Diagnosticar projeto para conferir bindings e entregáveis.");
            return Result.Succeeded;
        }
        catch (Exception e) { if (transaction.GetStatus() == TransactionStatus.Started) transaction.RollBack(); message = e.Message; return Result.Failed; }
    }
}
