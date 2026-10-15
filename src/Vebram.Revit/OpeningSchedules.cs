using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>Native, phase-aware opening schedules. Instance marks are never rewritten.</summary>
public static class OpeningSchedules
{
 public const string Doors="02 PROJETO LEGAL - VEBRAM - Portas";
 public const string Windows="02 PROJETO LEGAL - VEBRAM - Janelas";
 public static ModuleResult Apply(Document doc,ViewPlan source)
 {
  if(doc.IsReadOnly||doc.IsFamilyDocument||doc.IsWorkshared)throw new InvalidOperationException("Use um RVT local editável.");
  var warnings=new List<string>();
  using var tx=new Transaction(doc,"VEBRAM — quadros de esquadrias");tx.Start();
  var doors=Ensure(doc,source,BuiltInCategory.OST_Doors,Doors,false);
  var windows=Ensure(doc,source,BuiltInCategory.OST_Windows,Windows,true);
  doc.Regenerate();
  foreach(var schedule in new[]{doors,windows})
  {
   var instances=new FilteredElementCollector(doc,schedule.Id).WhereElementIsNotElementType().Where(e=>e.Category?.Id.Value is (long)BuiltInCategory.OST_Doors or (long)BuiltInCategory.OST_Windows).ToList();
   string Mark(Element e)=>e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString()?.Trim()??"";
   int blank=instances.Count(e=>Mark(e).Length==0);
   if(blank>0)warnings.Add(schedule.Name+": "+blank+" esquadria(s) sem código; preencha a Marca nas propriedades do elemento.");
   var fields=schedule.Definition.GetFieldOrder().Select(id=>schedule.Definition.GetField(id)).Where(f=>!f.IsHidden).ToList();
   int Column(BuiltInParameter id)=>fields.FindIndex(f=>f.ParameterId.Value==(long)id);
   int markColumn=Column(BuiltInParameter.ALL_MODEL_MARK),widthColumn=Column(BuiltInParameter.DOOR_WIDTH),heightColumn=Column(BuiltInParameter.DOOR_HEIGHT);
   var body=schedule.GetTableData().GetSectionData(SectionType.Body);var missing=new HashSet<string>();
   if(markColumn>=0&&widthColumn>=0&&heightColumn>=0&&body.NumberOfColumns==fields.Count)
   for(int row=body.FirstRowNumber;row<=body.LastRowNumber;row++)
   {
    string code=schedule.GetCellText(SectionType.Body,row,body.FirstColumnNumber+markColumn).Trim();
    if(instances.Any(e=>Mark(e)==code)&&
     (string.IsNullOrWhiteSpace(schedule.GetCellText(SectionType.Body,row,body.FirstColumnNumber+widthColumn))||string.IsNullOrWhiteSpace(schedule.GetCellText(SectionType.Body,row,body.FirstColumnNumber+heightColumn))))missing.Add(code.Length>0?code:"(sem código)");
   }
   else warnings.Add(schedule.Name+": estrutura personalizada; confira as medidas diretamente na tabela.");
   if(missing.Count>0)warnings.Add(schedule.Name+": largura/altura vazia no quadro: "+string.Join(", ",missing.Order())+". Confira os parâmetros das famílias; nenhuma medida foi deduzida da caixa envolvente.");
   foreach(var group in instances.Where(e=>Mark(e).Length>0).GroupBy(Mark).Where(g=>g.Count()>1))
    warnings.Add(schedule.Name+": código "+group.Key+" repetido em "+group.Count()+" elementos; confira se é intencional.");
  }
  tx.Commit();
  return new(doors.Id.Value,0,2,"Quadros nativos de portas e janelas preparados. Cada linha representa uma instância; medidas em metros. Incluem o projeto na fase da planta de origem, em todos os níveis.\nAs tabelas acompanham o modelo e estão disponíveis em Organizar folhas, preferencialmente nas laterais da folha de plantas.\nMarcas existentes preservadas. Abertura/material não são deduzidos do nome da família.\n"+string.Join("\n",warnings));
 }
 static ViewSchedule Ensure(Document doc,ViewPlan source,BuiltInCategory category,string name,bool window)
 {
  var existing=new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().FirstOrDefault(s=>s.Name==name);
  if(existing!=null)
  {
   if(!DrawingOwnership.IsAnnotation(existing))throw new InvalidOperationException("Já existe uma tabela manual chamada "+name+". Renomeie-a antes de gerar os quadros.");
   // Reuse native schedules without resetting the user's formatting, filters or columns.
   return existing;
  }
  var view=ViewSchedule.CreateSchedule(doc,new ElementId(category));view.Name=name;
  var phase=source.get_Parameter(BuiltInParameter.VIEW_PHASE)?.AsElementId();
  if(phase!=null)view.get_Parameter(BuiltInParameter.VIEW_PHASE)?.Set(phase);
  var phaseFilter=source.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER)?.AsElementId();
  if(phaseFilter!=null)view.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER)?.Set(phaseFilter);
  var definition=view.Definition;definition.IsItemized=true;definition.ShowTitle=true;definition.ShowHeaders=true;
  ScheduleField Add(BuiltInParameter parameter,string heading,double mm,bool hidden=false)
  {
   var available=definition.GetSchedulableFields().FirstOrDefault(f=>f.ParameterId.Value==(long)parameter)
    ??throw new InvalidOperationException("A categoria não disponibiliza o campo "+heading+".");
   var field=definition.AddField(available);field.ColumnHeading=heading;field.IsHidden=hidden;field.GridColumnWidth=mm/304.8;return field;
  }
  var mark=Add(BuiltInParameter.ALL_MODEL_MARK,"CÓDIGO",22);
  Add(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,"FAMÍLIA / TIPO",72,true);
  foreach(var spec in new[]{(window?BuiltInParameter.WINDOW_WIDTH:BuiltInParameter.DOOR_WIDTH,"L (m)"),(window?BuiltInParameter.WINDOW_HEIGHT:BuiltInParameter.DOOR_HEIGHT,"H (m)")})
  {
   var field=Add(spec.Item1,spec.Item2,25);field.SetFormatOptions(new FormatOptions(UnitTypeId.Meters){UseDefault=false,Accuracy=.01,SuppressTrailingZeros=false});
  }
  if(window){var sill=Add(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM,"PEITORIL (m)",32);sill.SetFormatOptions(new FormatOptions(UnitTypeId.Meters){UseDefault=false,Accuracy=.01,SuppressTrailingZeros=false});}
  var count=definition.AddField(ScheduleFieldType.Count);count.ColumnHeading="QTD";count.GridColumnWidth=14/304.8;
  definition.AddSortGroupField(new ScheduleSortGroupField(mark.FieldId));
  DrawingOwnership.MarkAnnotation(view);return view;
 }
}
