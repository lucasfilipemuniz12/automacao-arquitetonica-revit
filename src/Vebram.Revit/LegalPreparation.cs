using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Vebram.Revit;

public sealed record Request(Deliverable Item, long SourceId);
public sealed record Operation(Deliverable Item, View Source, View? Existing, string TargetName, ViewDuplicateOption Option)
{
    public string Description => Existing != null ? "Reutilizar: " + Existing.Name :
        "Criar: " + TargetName + "\n    Fonte: " + Source.Name + " · escala 1:" + Source.Scale +
        (Option == ViewDuplicateOption.WithDetailing ? " · com detalhamento" : " · sem detalhamento");
}

public static class Ownership
{
    private static readonly Guid Id = new("d2d3d7ee-ec81-445f-8d1d-bcf71a7ae432");
    public static (string Key, string Source)? Read(View view)
    {
        var schema = Schema.Lookup(Id);
        if (schema == null) return null;
        var entity = view.GetEntity(schema);
        return entity.IsValid() ? (entity.Get<string>(schema.GetField("Key")), entity.Get<string>(schema.GetField("Source"))) : null;
    }
    public static void Write(View view, string key, string source)
    {
        var schema = Schema.Lookup(Id);
        if (schema == null)
        {
            var builder = new SchemaBuilder(Id);
            builder.SetSchemaName("VebramLegalDeliverableV1");
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField("Key", typeof(string));
            builder.AddSimpleField("Source", typeof(string));
            schema = builder.Finish();
        }
        var entity = new Entity(schema);
        entity.Set(schema.GetField("Key"), key);
        entity.Set(schema.GetField("Source"), source);
        view.SetEntity(entity);
    }
}

public static class LegalPreparation
{
    public static List<Operation> Plan(Document doc, IReadOnlyList<Request> requests)
    {
        if (requests.Count == 0) throw new InvalidOperationException("Selecione pelo menos um entregável.");
        if (doc.IsFamilyDocument || doc.IsReadOnly || doc.IsWorkshared ||
            doc.PathName.EndsWith(".rte", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use um RVT local editável de demonstração. Templates RTE, famílias e modelos compartilhados não são suportados nesta versão.");
        var all = LegalCatalog.Views(doc);
        var plan = new List<Operation>();
        foreach (var request in requests)
        {
            var item = request.Item;
            var source = doc.GetElement(new ElementId(request.SourceId)) as View;
            if (source == null || source.IsTemplate || !LegalCatalog.Compatible(source, item))
                throw new InvalidOperationException(item.Label + ": escolha uma vista-fonte compatível.");
            if (source.GetPrimaryViewId() != ElementId.InvalidElementId)
                throw new InvalidOperationException(item.Label + ": escolha uma vista independente, não dependente.");
            var key = "legal-v02/" + item.Key;
            var owned = all.Where(v => Ownership.Read(v)?.Key == key).ToList();
            if (owned.Count > 1)
                throw new InvalidOperationException(item.Label + ": há mais de uma vista com a mesma identidade VEBRAM. Corrija o conflito antes de continuar.");
            var existing = owned.SingleOrDefault();
            if (existing != null)
            {
                var stamp = Ownership.Read(existing)!.Value;
                if (existing.Id != source.Id && stamp.Source != source.UniqueId)
                    throw new InvalidOperationException(item.Label + ": a fonte difere da geração anterior. Revisar a migração em vez de gerar uma segunda cópia.");
                if (!LegalCatalog.Compatible(existing, item))
                    throw new InvalidOperationException(item.Label + ": a vista registrada mudou de tipo.");
            }
            if (existing == null && ((LegalCatalog.IsLegal(source) && LegalCatalog.HasRole(source, item)) ||
                (item.Type == ViewType.Schedule && LegalCatalog.Normalize(source.Name).StartsWith(LegalCatalog.Stage + " - ", StringComparison.OrdinalIgnoreCase))))
                existing = source;
            string targetName = "LEGAL - " + item.Label + " - VEBRAM";
            var option = source.CanViewBeDuplicated(ViewDuplicateOption.WithDetailing) && item.Type != ViewType.Schedule
                ? ViewDuplicateOption.WithDetailing : ViewDuplicateOption.Duplicate;
            if (existing == null)
            {
                if (!source.CanViewBeDuplicated(option)) throw new InvalidOperationException(item.Label + ": a vista não permite duplicação.");
                if (all.Any(v => LegalCatalog.Same(v.Name, targetName)))
                    throw new InvalidOperationException("Nome ocupado por vista não registrada: " + targetName);
                if (item.Type != ViewType.Schedule)
                    foreach (var guid in new[] { LegalCatalog.StageId, LegalCatalog.FunctionId })
                        if (source.get_Parameter(guid) == null)
                            throw new InvalidOperationException(item.Label + ": binding ARQ ausente. Execute o diagnóstico.");
            }
            plan.Add(new(item, source, existing, targetName, option));
        }
        return plan;
    }

    public static string Apply(Document doc, IReadOnlyList<Operation> plan)
    {
        var create = plan.Where(x => x.Existing == null).ToList();
        if (create.Count == 0) return $"{plan.Count} entregáveis reutilizados. Nenhum elemento criado.";
        var failures = new RollbackOnFailure();
        using var transaction = new Transaction(doc, "VEBRAM — preparar legal");
        transaction.Start();
        transaction.SetFailureHandlingOptions(transaction.GetFailureHandlingOptions()
            .SetFailuresPreprocessor(failures).SetClearAfterRollback(true));
        try
        {
            foreach (var op in create)
            {
                var copy = (View)doc.GetElement(op.Source.Duplicate(op.Option));
                copy.Name = op.TargetName;
                // Preset, escala, nível e recorte da fonte são preservados.
                SetText(copy, LegalCatalog.StageId, LegalCatalog.Stage, op.Item.Type != ViewType.Schedule);
                SetText(copy, LegalCatalog.FunctionId, op.Item.Role, op.Item.Type != ViewType.Schedule);
                Ownership.Write(copy, "legal-v02/" + op.Item.Key, op.Source.UniqueId);
            }
            doc.Regenerate();
            if (transaction.Commit() != TransactionStatus.Committed)
                throw new InvalidOperationException("Operação revertida. " + string.Join("\n", failures.Messages));
            return $"{create.Count} entregáveis criados; {plan.Count - create.Count} reutilizados.\nConfira recortes, direção dos cortes, norte, conteúdo e escala. Salve o RVT quando concluir a revisão.";
        }
        catch
        {
            if (transaction.GetStatus() == TransactionStatus.Started) transaction.RollBack();
            throw;
        }
    }
    private static void SetText(View view, Guid id, string value, bool required)
    {
        var p = view.get_Parameter(id);
        if (p == null && !required) return;
        if (p == null || p.IsReadOnly || p.StorageType != StorageType.String || !p.Set(value))
            throw new InvalidOperationException(view.Name + ": etapa/função não pode ser gravada. O preset pode estar controlando o campo.");
    }
    private sealed class RollbackOnFailure : IFailuresPreprocessor
    {
        public List<string> Messages { get; } = [];
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            var failures = accessor.GetFailureMessages();
            Messages.AddRange(failures.Select(f => f.GetDescriptionText()));
            // Sem esconder alertas: registra e reverte a operação inteira nesta primeira versão.
            return failures.Count > 0 ? FailureProcessingResult.ProceedWithRollBack : FailureProcessingResult.Continue;
        }
    }
}
