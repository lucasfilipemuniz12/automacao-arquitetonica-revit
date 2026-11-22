using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using ComboBox=System.Windows.Controls.ComboBox;

namespace Vebram.Revit;

public sealed partial class PrepareWindow
{
    void ChooseFloorModules(string key)
    {
        bool memory=key=="memoria";
        var allViews=LegalCatalog.Views(doc);
        var modelPlans=Implantation.ModelPlans(doc,true).Where(v=>!LegalCatalog.IsLegal(v)).ToList();
        if(Source is { } chosen&&!LegalCatalog.IsLegal(chosen)&&!modelPlans.Contains(chosen))modelPlans.Add(chosen);
        var floorLevels=new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().Select(f=>f.LevelId).ToHashSet();
        var groups=modelPlans.GroupBy(v=>v.GenLevel.Id).OrderBy(g=>g.First().GenLevel.Elevation).ToList();
        var rows=new List<(Level Level,FrameworkElement Row,ComboBox Source,ComboBox? Floor)>();
        var title=memory?"Memória gráfica de áreas":"Planta legal";
        var dialog=new Window{Owner=this,Title=title,Width=650,MaxHeight=SystemParameters.WorkArea.Height-60,
            SizeToContent=SizeToContent.Height,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var body=new StackPanel{Margin=new Thickness(24)};
        dialog.Content=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        body.Children.Add(new TextBlock{Text=title,FontSize=22});
        var selector=new ComboBox{MinHeight=32};
        body.Children.Add(Label("Pavimentos a preparar"));body.Children.Add(selector);
        var scaleChoices=new[]{new SourceChoice(0,"Usar o padrão do projeto"),new SourceChoice(50,"1:50"),
            new SourceChoice(100,"1:100"),new SourceChoice(200,"1:200")};
        var scales=new ComboBox{ItemsSource=scaleChoices,SelectedIndex=0,MinHeight=32};
        body.Children.Add(Label("Escala das vistas geradas"));body.Children.Add(scales);
        foreach(var group in groups)
        {
            var level=group.First().GenLevel;
            var reference=memory?null:LegalModules.PlanReference(doc,level.Id);
            if(memory?!floorLevels.Contains(level.Id):reference==null)continue;
            var candidates=group.Where(v=>memory||Implantation.Footprints(doc,v).Count>0).ToList();
            if(candidates.Count==0)continue;
            var row=new StackPanel();
            row.Children.Add(Label("Planta de modelagem · "+level.Name));
            var sourcesForLevel=new ComboBox{ItemsSource=candidates.Select(v=>new SourceChoice(v.Id.Value,v.Name)).ToList(),MinHeight=32};
            row.Children.Add(sourcesForLevel);
            ComboBox? floor=null;
            if(!memory)
            {
                floor=new ComboBox{MinHeight=32};
                row.Children.Add(Label("Piso base · "+level.Name));row.Children.Add(floor);
                void ListFloors()
                {
                    if(sourcesForLevel.SelectedItem is not SourceChoice source||
                       doc.GetElement(new ElementId(source.Id)) is not ViewPlan view)return;
                    floor.ItemsSource=Implantation.Footprints(doc,view).Select(f=>new SourceChoice(f.Id.Value,f.Name)).ToList();
                    floor.SelectedIndex=0;
                }
                sourcesForLevel.SelectionChanged+=(_,_)=>ListFloors();
            }
            string? prior=memory?allViews.OfType<ViewPlan>().Where(v=>v.GenLevel?.Id==level.Id)
                .Select(v=>MemoryAreaState.Read(v)?.Source).FirstOrDefault(s=>s!=null):LegalModules.PreviousPlanSource(reference);
            var preferred=candidates.Find(v=>v.UniqueId==prior)??candidates.Find(v=>v.Id==Source?.Id)??
                candidates.Find(v=>v.Name.StartsWith("PRE - ",StringComparison.OrdinalIgnoreCase))??candidates[0];
            sourcesForLevel.SelectedItem=sourcesForLevel.Items.Cast<SourceChoice>().First(c=>c.Id==preferred.Id.Value);
            rows.Add((level,row,sourcesForLevel,floor));body.Children.Add(row);
        }
        if(rows.Count==0)
        {
            MessageBox.Show(this,memory?"Nenhum pavimento com planta de modelagem e piso.":"Nenhum pavimento com planta legal de referência e piso base.");
            return;
        }
        var levels=rows.Select(r=>new SourceChoice(r.Level.Id.Value,r.Level.Name)).ToList();
        if(levels.Count>1)levels.Add(new SourceChoice(0,"Todos os pavimentos listados"));
        bool Includes(Level level)=>selector.SelectedItem is SourceChoice choice&&(choice.Id==0||choice.Id==level.Id.Value);
        selector.ItemsSource=levels;
        selector.SelectionChanged+=(_,_)=>{foreach(var row in rows)row.Row.Visibility=Includes(row.Level)?System.Windows.Visibility.Visible:System.Windows.Visibility.Collapsed;};
        selector.SelectedItem=levels.FirstOrDefault(c=>c.Id==Source?.GenLevel.Id.Value)??levels[0];
        var edited=new CheckBox{Content="Refazer marcas do gerador que editei manualmente",Margin=new Thickness(0,12,0,8)};
        body.Children.Add(edited);
        if(memory)body.Children.Add(new TextBlock{Text="A memória usa as classes dos pisos de cada pavimento. Prepare a Implantação para incluir o lote na memória do térreo.",TextWrapping=TextWrapping.Wrap});
        var apply=new Button{Content="Preparar vistas",Padding=new Thickness(15,8,15,8),Margin=new Thickness(0,12,0,0),HorizontalAlignment=HorizontalAlignment.Right};
        body.Children.Add(apply);
        List<ModuleSelection>? requests=null;
        apply.Click+=(_,_)=>
        {
            var wanted=rows.Where(r=>Includes(r.Level)).ToList();
            if(wanted.Count==0)return;
            if(wanted.Any(r=>r.Source.SelectedItem is not SourceChoice||!memory&&r.Floor?.SelectedItem is not SourceChoice))
            {
                MessageBox.Show(dialog,"Escolha a planta de modelagem e o piso base de cada pavimento.");
                return;
            }
            int scale=(int)((SourceChoice)scales.SelectedItem).Id;
            requests=wanted.Select(r=>new ModuleSelection(key,((SourceChoice)r.Source.SelectedItem).Id,
                (r.Floor?.SelectedItem as SourceChoice)?.Id??0,edited.IsChecked==true,scale==0?null:scale)).ToList();
            dialog.DialogResult=true;
        };
        LegalGuide.Attach(dialog,body,key);
        if(dialog.ShowDialog()==true){FloorModules=requests;DialogResult=true;}
    }
}
