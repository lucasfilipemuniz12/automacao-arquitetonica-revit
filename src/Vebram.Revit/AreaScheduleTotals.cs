using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>
/// Q04 (rooms and their areas) gets a total per floor and a grand total, using Revit's own schedule totals.
/// Rule: sum of the room areas measured by Revit on each level (useful internal area of the rooms). It is not the
/// built/projection area and no municipal criterion is applied. Rows, columns and their headings are kept.
/// </summary>
internal static class AreaScheduleTotals
{
 internal const string GrandTitle="TOTAL DAS ÁREAS ÚTEIS — sem áreas descobertas";
 internal const string UncoveredName="02 PROJETO LEGAL - Q04 - Áreas descobertas";
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
   var terraces=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Autodesk.Revit.DB.Architecture.Room>().Where(r=>r.Area>0&&(r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString()??"").Contains("Terraço",StringComparison.OrdinalIgnoreCase)).ToList();
   var overhead=new FilteredElementCollector(doc).OfClass(typeof(HostObject)).Cast<HostObject>().Where(h=>h is Floor or Ceiling or RoofBase).Select(h=>h.get_BoundingBox(null)).Where(b=>b!=null).ToList();
   bool IsOpen(Autodesk.Revit.DB.Architecture.Room r)=>r.Location is LocationPoint l&&!overhead.Any(b=>b!.Min.Z>l.Point.Z+Implantation.Feet(.5)&&l.Point.X>b.Min.X&&l.Point.X<b.Max.X&&l.Point.Y>b.Min.Y&&l.Point.Y<b.Max.Y);
   string token=terraces.Count>0&&terraces.All(IsOpen)?"Terraço":"DESCOBERTO";
   // Two live native tables: never change room names/areas or introduce a legal area coefficient.
   // The explicit DESCOBERTO name is the project's classification, independent of floor material.
   var name=Find(BuiltInParameter.ROOM_NAME)??definition.AddField(definition.GetSchedulableFields().First(f=>f.ParameterId.Value==(long)BuiltInParameter.ROOM_NAME));
   for(int i=definition.GetFilterCount()-1;i>=0;i--)if(definition.GetFilter(i).FieldId==name.FieldId&&definition.GetFilter(i).IsStringValue&&definition.GetFilter(i).GetStringValue()==token)definition.RemoveFilter(i);
   var outside=new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().FirstOrDefault(s=>s.Name==UncoveredName);
   if(outside==null){outside=(ViewSchedule)doc.GetElement(schedule.Duplicate(ViewDuplicateOption.Duplicate));outside.Name=UncoveredName;}
   var od=outside.Definition;
   ScheduleField? on=null;for(int i=0;i<od.GetFieldCount();i++)if(od.GetField(i).ParameterId.Value==(long)BuiltInParameter.ROOM_NAME)on=od.GetField(i);
   if(on==null)throw new InvalidOperationException("Quadro de áreas descobertas sem nome do ambiente.");
   for(int i=od.GetFilterCount()-1;i>=0;i--)if(od.GetFilter(i).FieldId==on.FieldId&&od.GetFilter(i).IsStringValue&&od.GetFilter(i).GetStringValue()==token)od.RemoveFilter(i);
   od.AddFilter(new ScheduleFilter(on.FieldId,ScheduleFilterType.Contains,token));
   od.ShowGrandTotal=true;od.ShowGrandTotalTitle=true;od.ShowGrandTotalCount=false;od.GrandTotalTitle="ÁREAS DESCOBERTAS — fora do total útil";
   var header=outside.GetTableData().GetSectionData(SectionType.Header);if(header.NumberOfRows>0&&header.NumberOfColumns>0)header.SetCellText(header.FirstRowNumber,header.FirstColumnNumber,"Q04 — ÁREAS DESCOBERTAS · fora do total útil");
   definition.AddFilter(new ScheduleFilter(name.FieldId,ScheduleFilterType.NotContains,token));
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Totais do Q04 revertidos pelo Revit.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
  return "Q04: áreas descobertas apresentadas em quadro separado, fora dos totais úteis. A classificação usa DESCOBERTO no nome ou Terraço sem laje, forro ou telhado acima do ponto do ambiente. Áreas nativas preservadas; cálculo da taxa de ocupação não alterado.";
 }
}
