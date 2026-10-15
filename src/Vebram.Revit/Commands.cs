using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using TextBox = System.Windows.Controls.TextBox;

namespace Vebram.Revit;

[Transaction(TransactionMode.ReadOnly)]
public sealed class DiagnoseCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
        try
        {
            var doc = data.Application.ActiveUIDocument?.Document;
            if (doc == null || doc.IsFamilyDocument) return Result.Cancelled;
            var report = Diagnostics.Inspect(doc);
            ReportWindow.Show(data.Application, report);
            return Result.Succeeded;
        }
        catch (Exception e) { message = e.Message; return Result.Failed; }
    }
}

[Transaction(TransactionMode.Manual)]
public sealed class PrepareLegalCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
        try
        {
            var doc = data.Application.ActiveUIDocument?.Document;
            if (doc == null || doc.IsFamilyDocument) return Result.Cancelled;
            var form = new PrepareWindow(data.Application);
            if (form.ShowDialog() != true) return Result.Cancelled;
            if(form.ComposeSheets)
            {
                var composition=LegalSheetComposition.Apply(doc);data.Application.ActiveUIDocument!.ActiveView=(View)doc.GetElement(new ElementId(composition.SheetId));data.Application.ActiveUIDocument.GetOpenUIViews().FirstOrDefault(v=>v.ViewId.Value==composition.SheetId)?.ZoomToFit();TaskDialog.Show("VEBRAM — composição",composition.Report);return Result.Succeeded;
            }
            if(form.Sheet is {} sheet)
            {
                var prepared=LegalSheets.Apply(doc,sheet);data.Application.ActiveUIDocument!.ActiveView=(View)doc.GetElement(new ElementId(prepared.ViewId));data.Application.ActiveUIDocument.GetOpenUIViews().FirstOrDefault(v=>v.ViewId.Value==prepared.ViewId)?.ZoomToFit();TaskDialog.Show("VEBRAM — folha",prepared.Report);return Result.Succeeded;
            }
            if(form.Context is {} context)
            {
                var prepared=LegalContext.Apply(doc,context);data.Application.ActiveUIDocument!.ActiveView=(View)doc.GetElement(new ElementId(prepared.ViewId));data.Application.ActiveUIDocument.GetOpenUIViews().FirstOrDefault(v=>v.ViewId.Value==prepared.ViewId)?.ZoomToFit();TaskDialog.Show("VEBRAM — resultado",prepared.Report);return Result.Succeeded;
            }
            if(form.Facades is {} facades)
            {
                var prepared=LegalFacades.Apply(doc,facades);data.Application.ActiveUIDocument!.ActiveView=(View)doc.GetElement(new ElementId(prepared[0].ViewId));data.Application.ActiveUIDocument.GetOpenUIViews().FirstOrDefault(v=>v.ViewId.Value==prepared[0].ViewId)?.ZoomToFit();TaskDialog.Show("VEBRAM — fachadas",string.Join("\n",prepared.Select(p=>p.Report)));return Result.Succeeded;
            }
            if(form.Section is {} section)
            {
                var prepared=LegalSections.Apply(doc,section);
                data.Application.ActiveUIDocument!.ActiveView=(View)doc.GetElement(new ElementId(prepared.ViewId));
                data.Application.ActiveUIDocument.GetOpenUIViews().FirstOrDefault(v=>v.ViewId.Value==prepared.ViewId)?.ZoomToFit();
                TaskDialog.Show("VEBRAM — corte preparado",prepared.Report);return Result.Succeeded;
            }
            if(form.Module is { } module)
            {
                var prepared=LegalModules.Apply(doc,module);
                data.Application.ActiveUIDocument!.ActiveView=(View)doc.GetElement(new ElementId(prepared.ViewId));
                if(module.Key!="areas")data.Application.ActiveUIDocument!.GetOpenUIViews().FirstOrDefault(v=>v.ViewId.Value==prepared.ViewId)?.ZoomToFit();
                TaskDialog.Show("VEBRAM — resultado",prepared.Report);return Result.Succeeded;
            }
            var plan = Implantation.Plan(doc, form.Selection!);
            var preview = new TaskDialog("VEBRAM — prévia")
            {
                MainInstruction = "Preparar a Implantação?",
                MainContent = plan.Summary,
                CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
                DefaultButton = TaskDialogResult.Cancel
            };
            if (preview.Show() != TaskDialogResult.Ok) return Result.Cancelled;
            var result = Implantation.Apply(doc, plan);
            var uidoc = data.Application.ActiveUIDocument!;
            uidoc.ActiveView = (View)doc.GetElement(new ElementId(result.ViewId));
            uidoc.GetOpenUIViews().FirstOrDefault(v => v.ViewId.Value == result.ViewId)?.ZoomToFit();
            TaskDialog.Show("VEBRAM — resultado", result.Report);
            return Result.Succeeded;
        }
        catch (Exception e) { message = e.Message; return Result.Failed; }
    }
}

[Transaction(TransactionMode.Manual)]
public sealed class DemoCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
        Document? demo = null;
        try
        {
            var template = new OpenFileDialog { Title = "Escolher o template ARQ_AUTOMACAO_V02", Filter = "Template Revit (*.rte)|*.rte", CheckFileExists = true };
            if (template.ShowDialog() != true) return Result.Cancelled;
            var save = new SaveFileDialog { Title = "Salvar novo RVT de demonstração", Filter = "Projeto Revit (*.rvt)|*.rvt", FileName = "VEBRAM_Demonstracao_V02.rvt", OverwritePrompt = true };
            if (save.ShowDialog() != true) return Result.Cancelled;
            if (File.Exists(save.FileName)) throw new InvalidOperationException("Escolha um nome novo. O comando de demonstração não substitui arquivos.");
            demo = data.Application.Application.NewProjectDocument(template.FileName);
            var report = Diagnostics.Inspect(demo);
            demo.SaveAs(save.FileName, new SaveAsOptions { OverwriteExistingFile = false });
            demo.Close(false); demo = null;
            data.Application.OpenAndActivateDocument(save.FileName);
            ReportWindow.Show(data.Application, report);
            return Result.Succeeded;
        }
        catch (Exception e) { message = e.Message; return Result.Failed; }
        finally { if (demo?.IsValidObject == true) demo.Close(false); }
    }
}

public static class ReportWindow
{
    public static void Show(UIApplication app, DiagnosticReport report)
    {
        var window = new Window { Title = "VEBRAM — diagnóstico somente leitura", Width = 900, Height = 650,
            MinWidth = 560, MinHeight = 400, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        new WindowInteropHelper(window).Owner = app.MainWindowHandle;
        var panel = new DockPanel { Margin = new Thickness(16) };
        window.Content = panel;
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer);
        var export = new Button { Content = "Salvar relatório", Padding = new Thickness(12, 7, 12, 7) };
        export.Click += (_, _) =>
        {
            try
            {
                var save = new SaveFileDialog { Title = "Salvar relatório do diagnóstico", Filter = "Markdown (*.md)|*.md|JSON (*.json)|*.json", FileName = "VEBRAM_Diagnostico.md" };
                if (save.ShowDialog(window) != true) return;
                File.WriteAllText(save.FileName, Path.GetExtension(save.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase)
                    ? Diagnostics.Json(report) : Diagnostics.Markdown(report));
            }
            catch (Exception e) { MessageBox.Show(window, e.Message, "Não foi possível salvar o relatório"); }
        };
        var close = new Button { Content = "Fechar", IsCancel = true, Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 7, 12, 7) };
        close.Click += (_, _) => window.Close();
        footer.Children.Add(export); footer.Children.Add(close);
        panel.Children.Add(new TextBox { Text = Diagnostics.Markdown(report), IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        window.ShowDialog();
    }
}

