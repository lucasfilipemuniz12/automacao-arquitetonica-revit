using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Vebram.Revit;

// Ferramenta de desenvolvimento, inativa no uso normal. Só executa quando a sessão é
// iniciada com VEBRAM_TEST_JOB apontando para um job de teste explícito neste pacote.
// Atua exclusivamente no arquivo já copiado em PluginRevit/teste, nunca no original.
internal static class DevelopmentSession
{
    private sealed record Job(string TestProject, string Template, string OutputDirectory, bool RunChecks,
        bool ResumePreparedSources = false, string? SourceProject = null);
    private static string? JobPath;
    private static PendingSession? Pending;
    private sealed record PendingSession(UIDocument UiDoc, Job Job, string Root, List<View> OriginalViews,
        List<ViewSheet> OriginalSheets, HashSet<long> Physical, List<string> SourceMap, View FirstView);
    public static void RunActiveTest(UIApplication ui, string path)
    {
        var job = JsonSerializer.Deserialize<Job>(File.ReadAllText(path)) ?? throw new InvalidOperationException("Job inválido.");
        var root = Path.GetFullPath(job.OutputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var uidoc = ui.ActiveUIDocument ?? throw new InvalidOperationException("Abra a cópia de teste.");
        if (!uidoc.Document.PathName.Equals(job.TestProject, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFullPath(job.TestProject).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O documento ativo não é a cópia de teste autorizada.");
        Pending = PrepareTestCopy(uidoc, job, root);
        uidoc.ActiveView = Pending.FirstView;
        Complete(ui);
    }
    public static void Attach(UIControlledApplication app)
    {
        var path = Environment.GetEnvironmentVariable("VEBRAM_DEV_JOB");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "session-job.json");
        if (File.Exists(path)) { JobPath = path; app.Idling += OnIdle; }
    }
    private static void OnIdle(object? sender, IdlingEventArgs args)
    {
        if (sender is not UIApplication ui) return;
        if (Pending != null) { OnViewReady(sender, args); return; }
        var path = JobPath!;
        if (!File.Exists(path)) { ui.Idling -= OnIdle; return; }
        Job? job = null;
        try
        {
            job = JsonSerializer.Deserialize<Job>(File.ReadAllText(path)) ?? throw new InvalidOperationException("Job inválido.");
            if (job.ResumePreparedSources && ui.ActiveUIDocument?.Document.PathName.Equals(job.TestProject, StringComparison.OrdinalIgnoreCase) != true)
            { ui.Idling -= OnIdle; return; }
            var root = Path.GetFullPath(job.OutputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(job.TestProject).StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                !job.TestProject.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cópia de teste fora da pasta explicitamente autorizada.");
            File.Move(path, path + ".executado", false); // Consome o job uma única vez.
            Directory.CreateDirectory(job.OutputDirectory);
            // M0: consulta nativa a um projeto novo criado do RTE, sem salvar/modificar o RTE.
            var demo = ui.Application.NewProjectDocument(job.Template);
            try
            {
                File.WriteAllText(Path.Combine(root, "M0_template_nativo.json"), Diagnostics.Json(Diagnostics.Inspect(demo)));
                File.WriteAllText(Path.Combine(root, "M0_template_nativo.md"), Diagnostics.Markdown(Diagnostics.Inspect(demo)));
            }
            finally { demo.Close(false); }
            var uidoc = ui.ActiveUIDocument?.Document.PathName.Equals(job.TestProject, StringComparison.OrdinalIgnoreCase) == true
                ? ui.ActiveUIDocument : ui.OpenAndActivateDocument(job.TestProject);
            if (!string.IsNullOrEmpty(job.SourceProject)) RestoreWorkingSources(ui, uidoc.Document, job.SourceProject, root);
            if (job.ResumePreparedSources)
            {
                var working = LegalCatalog.Views(uidoc.Document).Where(v => v.Name.StartsWith("FONTE - ", StringComparison.Ordinal)).ToList();
                if (working.Count == 0) throw new InvalidOperationException("Não há vistas-fonte para retomar.");
                Pending = new(uidoc, job, root, LegalCatalog.Views(uidoc.Document).Except(working).ToList(),
                    new FilteredElementCollector(uidoc.Document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList(),
                    PhysicalIds(uidoc.Document), working.Select(v => v.Name).ToList(), working.First(v => v.ViewType == ViewType.FloorPlan));
            }
            else Pending = PrepareTestCopy(uidoc, job, root);
            uidoc.RequestViewChange(Pending.FirstView);
        }
        catch (Exception e)
        {
            ui.Idling -= OnIdle;
            var folder = job?.OutputDirectory ?? Path.GetDirectoryName(path)!;
            File.WriteAllText(Path.Combine(folder, "SESSAO_ERRO.txt"), e.ToString());
            TaskDialog.Show("VEBRAM — sessão de desenvolvimento", e.Message);
        }
    }

    private static HashSet<long> PhysicalIds(Document doc) => new FilteredElementCollector(doc)
        .WhereElementIsNotElementType().Where(e => !e.ViewSpecific && e.Category?.CategoryType == CategoryType.Model &&
            e is not View && e is not SketchPlane && e is not Sketch && e is not ElevationMarker)
        .Select(e => e.Id.Value).ToHashSet();

    private static void RestoreWorkingSources(UIApplication ui, Document target, string sourcePath, string root)
    {
        // Reconstitui apenas vistas de trabalho do arquivo de referência, sem copiar geometria.
        var source = ui.Application.OpenDocumentFile(sourcePath);
        var log = new List<string>();
        try
        {
            using var transaction = new Transaction(target, "VEBRAM — fontes limpas de referência");
            transaction.Start();
            var all = LegalCatalog.Views(source);
            foreach (var (key, name) in new[] { ("implantacao", "PL17 - Implantação"), ("cobertura", "PL17 - Cobertura"),
                ("corte-a", "PL17 - Corte A-A"), ("corte-b", "PL17 - Corte B-B") })
            {
                var item = LegalCatalog.Items.Single(i => i.Key == key);
                if (LegalCatalog.Views(target).Any(v => v.Name == "FONTE - " + item.ReferenceName)) continue;
                var original = all.Single(v => v.Name == name);
                View copy;
                if (original is ViewPlan plan)
                {
                    var level = (Level)target.GetElement(plan.GenLevel.UniqueId);
                    copy = ViewPlan.Create(target, original.GetTypeId(), level.Id);
                    copy.CropBox = original.CropBox;
                }
                else
                {
                    var oldBox = original.CropBox;
                    var transform = Transform.Identity;
                    transform.Origin = oldBox.Transform.Origin;
                    transform.BasisY = oldBox.Transform.BasisY;
                    transform.BasisZ = oldBox.Transform.BasisZ.Negate();
                    transform.BasisX = transform.BasisY.CrossProduct(transform.BasisZ);
                    var corners = new List<XYZ>();
                    foreach (var x in new[] { oldBox.Min.X, oldBox.Max.X })
                    foreach (var y in new[] { oldBox.Min.Y, oldBox.Max.Y })
                    foreach (var z in new[] { oldBox.Min.Z, oldBox.Max.Z })
                        corners.Add(transform.Inverse.OfPoint(oldBox.Transform.OfPoint(new XYZ(x, y, z))));
                    var box = new BoundingBoxXYZ { Transform = transform,
                        Min = new XYZ(corners.Min(p => p.X), corners.Min(p => p.Y), corners.Min(p => p.Z)),
                        Max = new XYZ(corners.Max(p => p.X), corners.Max(p => p.Y), corners.Max(p => p.Z)) };
                    copy = ViewSection.CreateSection(target, original.GetTypeId(), box);
                }
                copy.Name = "FONTE - " + item.ReferenceName;
                copy.Scale = original.Scale;
                copy.CropBoxActive = original.CropBoxActive;
                copy.CropBoxVisible = false;
                copy.get_Parameter(LegalCatalog.StageId)?.Set("09 APOIO");
                copy.get_Parameter(LegalCatalog.FunctionId)?.Set(item.Role);
                log.Add(copy.Name + " <- referência " + original.Name + "; vista reconstruída sem anotações");
            }
            if (transaction.Commit() != TransactionStatus.Committed) throw new InvalidOperationException("Fontes não reconstituídas.");
        }
        finally { source.Close(false); }
        File.WriteAllText(Path.Combine(root, "FONTES_RECONSTITUIDAS.txt"), string.Join("\n", log));
    }

    private static PendingSession PrepareTestCopy(UIDocument uidoc, Job job, string root)
    {
        var doc = uidoc.Document;
        var physical = PhysicalIds(doc);
        var originalViews = LegalCatalog.Views(doc);
        var sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
        var displayed = new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>()
            .Select(p => (View)doc.GetElement(p.ViewId)).DistinctBy(v => v.Id.Value).ToList();
        var working = new List<View>();
        var sourceMap = new List<string>();
            using (var create = new Transaction(doc, "VEBRAM — vistas-fonte limpas"))
            {
                create.Start(); ParameterSetup.Bind(doc);
                foreach (var item in LegalCatalog.Items.Where(i => i.Type is ViewType.FloorPlan or ViewType.Section))
                {
                    // O caso R18 é térreo. Não inventar nível 2 nem posição de situação geográfica.
                    if (item.Key is "planta-2" or "situacao") continue;
                    string token = item.Key switch { "planta-1" => "Planta térrea", "implantacao" => "Implantação",
                        "cobertura" => "Cobertura", "corte-a" => "Corte A-A", "corte-b" => "Corte B-B", _ => "" };
                    var candidates = displayed.Where(v => v.ViewType == item.Type && v.Name.Contains(token, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (candidates.Count != 1) { sourceMap.Add(item.Label + ": escolha manual necessária."); continue; }
                    var source = candidates[0];
                    var copy = (View)doc.GetElement(source.Duplicate(ViewDuplicateOption.Duplicate));
                    copy.Name = "FONTE - " + item.ReferenceName;
                    copy.get_Parameter(LegalCatalog.StageId)?.Set("09 APOIO");
                    copy.get_Parameter(LegalCatalog.FunctionId)?.Set(item.Role);
                    working.Add(copy); sourceMap.Add(copy.Name + " <- " + source.Name);
                }
                // Fachadas históricas são cortes. Preservar nomes físicos; orientação cardeal é escolha do usuário.
                foreach (var source in displayed.Where(v => v.Name.Contains("Fachada", StringComparison.OrdinalIgnoreCase)))
                {
                    var copy = (View)doc.GetElement(source.Duplicate(ViewDuplicateOption.Duplicate));
                    copy.Name = "FONTE - " + source.Name;
                    copy.get_Parameter(LegalCatalog.StageId)?.Set("09 APOIO");
                    copy.get_Parameter(LegalCatalog.FunctionId)?.Set("FACHADAS");
                    working.Add(copy); sourceMap.Add(copy.Name + " <- " + source.Name);
                }
                var q04 = ViewSchedule.CreateSchedule(doc, new ElementId(BuiltInCategory.OST_Rooms));
                q04.Name = "FONTE - " + LegalCatalog.Items.Single(i => i.Key == "areas").ReferenceName;
                var available = q04.Definition.GetSchedulableFields();
                foreach (var builtin in new[] { BuiltInParameter.ROOM_NUMBER, BuiltInParameter.ROOM_NAME, BuiltInParameter.ROOM_AREA })
                {
                    var field = available.FirstOrDefault(f => f.ParameterId == new ElementId(builtin));
                    if (field != null) q04.Definition.AddField(field);
                }
                working.Add(q04);
                if (create.Commit() != TransactionStatus.Committed) throw new InvalidOperationException("Criação de fontes não concluída.");
            }
        return new(uidoc, job, root, originalViews, sheets, physical, sourceMap, working.First(v => v.ViewType == ViewType.FloorPlan));
    }

    private static void OnViewReady(object? sender, IdlingEventArgs args)
    {
        if (sender is not UIApplication ui || Pending == null) return;
        var session = Pending;
        if (session.UiDoc.ActiveView.Id != session.FirstView.Id) return;
        Complete(ui);
    }

    private static void Complete(UIApplication ui)
    {
        var session = Pending!;
        ui.Idling -= OnIdle;
        Pending = null;
        var doc = session.UiDoc.Document;
        try
        {
            using (var cleanup = new Transaction(doc, "VEBRAM — retirar documentação da cópia"))
            {
                cleanup.Start();
                var remove = session.OriginalViews.Where(v => v.ViewType != ViewType.ThreeD && Regex.IsMatch(v.Name, @"^(PL(?:\d+)?\s*[- ]|LEGAL\s*-)", RegexOptions.IgnoreCase) &&
                    (v is not ViewSchedule schedule || !schedule.IsTitleblockRevisionSchedule)).Select(v => v.Id)
                    .Concat(session.OriginalSheets.Select(s => s.Id)).Where(id => doc.GetElement(id) != null).ToList();
                var metadata = session.Physical.Select(id => doc.GetElement(new ElementId(id)))
                    .Where(e => e != null).ToDictionary(e => e.Id.Value, e => new { e.Name, Type = e.GetType().Name, Category = e.Category?.Name });
                if (remove.Count > 0) doc.Delete(remove);
                var after = PhysicalIds(doc);
                if (!session.Physical.SetEquals(after))
                {
                    File.WriteAllText(Path.Combine(session.Root, "DIFERENCAS_MODELO.json"), JsonSerializer.Serialize(
                        new { Removed = session.Physical.Except(after).Select(id => new { Id = id, Metadata = metadata.GetValueOrDefault(id) }),
                            Added = after.Except(session.Physical).Select(id => new { Id = id, Type = doc.GetElement(new ElementId(id))?.GetType().Name }) }, new JsonSerializerOptions { WriteIndented = true }));
                    throw new InvalidOperationException("Limpeza afetaria elementos de modelo; operação revertida.");
                }
                if (cleanup.Commit() != TransactionStatus.Committed) throw new InvalidOperationException("Limpeza não concluída.");
            }
            var report = Diagnostics.Inspect(doc);
            File.WriteAllText(Path.Combine(session.Root, "BASE_TESTE.md"), "# Casa Terras — cópia de teste\n\nGeometria e IDs de modelo preservados. Folhas e vistas com prefixo legal PL/LEGAL removidas somente desta cópia. Vistas genéricas e dados do modelo preservados.\nVistas-fonte duplicadas sem detalhamento. Escolha das fachadas/norte exige conferência.\n\n" + string.Join("\n", session.SourceMap.Select(x => "- " + x)));
            File.WriteAllText(Path.Combine(session.Root, "BASE_TESTE_diagnostico.json"), Diagnostics.Json(report));
            if (session.Job.RunChecks) RunChecks(doc, session.Root);
            doc.Save();
            File.WriteAllText(Path.Combine(session.Root, "SESSAO_CONCLUIDA.txt"), "Cópia limpa, diagnóstico e verificações concluídos. Revit deixado aberto em " + doc.PathName);
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(session.Root, "SESSAO_ERRO_ASYNC.txt"), e.ToString());
            TaskDialog.Show("VEBRAM — sessão de desenvolvimento", e.Message);
        }
    }

    private static void RunChecks(Document doc, string root)
    {
        var views = LegalCatalog.Views(doc);
        var requests = LegalCatalog.Items.Select(i => (Item: i, Source: views.FirstOrDefault(v => LegalCatalog.Compatible(v, i) &&
            LegalCatalog.Same(v.Name, "FONTE - " + i.ReferenceName))))
            .Where(x => x.Source != null).Select(x => new Request(x.Item, x.Source!.Id.Value)).ToList();
        var physical = PhysicalIds(doc);
        var modified = doc.IsModified;
        var beforeDiagnostic = new FilteredElementCollector(doc).GetElementCount();
        _ = Diagnostics.Inspect(doc);
        if (modified != doc.IsModified || beforeDiagnostic != new FilteredElementCollector(doc).GetElementCount())
            throw new InvalidOperationException("Diagnóstico alterou o estado observado.");
        using var test = new TransactionGroup(doc, "VEBRAM — verificação reversível");
        test.Start();
        try
        {
            var first = LegalPreparation.Plan(doc, requests);
            var firstResult = LegalPreparation.Apply(doc, first);
            int count = LegalCatalog.Views(doc).Count;
            var second = LegalPreparation.Plan(doc, requests);
            if (second.Any(x => x.Existing == null)) throw new InvalidOperationException("Segunda execução propôs duplicatas.");
            var secondResult = LegalPreparation.Apply(doc, second);
            if (count != LegalCatalog.Views(doc).Count || !physical.SetEquals(PhysicalIds(doc)))
                throw new InvalidOperationException("Verificação de repetição/geometria falhou.");
            File.WriteAllText(Path.Combine(root, "VERIFICACAO_NATIVA.txt"),
                "PASS: diagnóstico sem alteração de contagem/IsModified.\nPASS: segunda execução sem duplicatas.\nPASS: IDs dos elementos de modelo preservados.\n" + firstResult + "\n" + secondResult + "\nGerações revertidas para deixar a cópia sem o legal aplicado.");
        }
        finally { if (test.GetStatus() == TransactionStatus.Started) test.RollBack(); }
    }
}
