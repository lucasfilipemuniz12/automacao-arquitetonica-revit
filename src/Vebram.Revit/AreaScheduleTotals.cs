using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>
/// Q04 (rooms and their areas) gets a total per floor and a grand total, using Revit's own schedule totals.
/// Rule: sum of the room areas measured by Revit on each level (useful internal area of the rooms). It is not the
/// built/projection area and no municipal criterion is applied. Rows, columns and their headings are kept.
/// </summary>
internal static class AreaScheduleTotals
{
 internal const string GrandTitle="TOTAL GERAL — soma das áreas dos ambientes";
 internal static string Apply(Document doc,ViewSchedule schedule)
 {
  var definition=schedule.Definition;
  ScheduleField? Find(BuiltInParameter p){for(int i=0;i<definition.GetFieldCount();i++){var f=definition.GetField(i);if(f.ParameterId.Value==(long)p)return f;}return null;}
  var area=Find(BuiltInParameter.ROOM_AREA)??throw new InvalidOperationException("Q04 sem a coluna de área; totais não aplicados.");
  var number=Find(BuiltInParameter.ROOM_NUMBER);
  using var tx=new Transaction(doc,"VEBRAM — Q04 totais por pavimento");tx.Start();
  try
  {
   var level=Find(BuiltInParameter.ROOM_LEVEL_ID);
   if(level==null)
   {
    var available=definition.GetSchedulableFields().FirstOrDefault(f=>f.ParameterId.Value==(long)BuiltInParameter.ROOM_LEVEL_ID)??throw new InvalidOperationException("Campo Nível indisponível no Q04.");
    level=definition.AddField(available);
   }
   level.IsHidden=true; // grouping column only; the floor name appears in each group's total row
   area.DisplayType=ScheduleFieldDisplayType.Totals;
   definition.ClearSortGroupFields();
   definition.AddSortGroupField(new ScheduleSortGroupField(level.FieldId,ScheduleSortOrder.Ascending){ShowHeader=false,ShowFooter=true,ShowFooterTitle=true,ShowFooterCount=false,ShowBlankLine=false});
   if(number!=null)definition.AddSortGroupField(new ScheduleSortGroupField(number.FieldId,ScheduleSortOrder.Ascending));
   definition.ShowGrandTotal=true;definition.ShowGrandTotalTitle=true;definition.ShowGrandTotalCount=false;definition.GrandTotalTitle=GrandTitle;
   definition.IsItemized=true;
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Totais do Q04 revertidos pelo Revit.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
  return "Q04 agrupado por pavimento, com o total de cada pavimento e o total geral no final. Regra: soma das áreas dos ambientes medidas pelo Revit (área útil); não é área construída nem projeção, e nenhum critério municipal foi aplicado.";
 }
}
