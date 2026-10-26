using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
namespace Vebram.Revit;
public sealed record RoomNameCorrection(string UniqueId,string Number,string Before,string After);
public static class RoomNameReview
{
 // Requires an explicit source document. No guessing from the current suffix.
 public static List<RoomNameCorrection> Preview(Document target,Document reference)
 {
  var originals=new FilteredElementCollector(reference).OfClass(typeof(SpatialElement)).OfType<Room>().GroupBy(r=>r.Number).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.Single().get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString());
  var result=new List<RoomNameCorrection>();
  foreach(var room in new FilteredElementCollector(target).OfClass(typeof(SpatialElement)).OfType<Room>())
  {
   string before=room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString()??"";
   if(!originals.TryGetValue(room.Number,out var original)||string.IsNullOrWhiteSpace(original))continue;
   string after=RoomNameText.RestoreFromReference(before,room.Number,original);if(after!=before)result.Add(new(room.UniqueId,room.Number,before,after));
  }
  return result;
 }
 public static void Apply(Document target,IReadOnlyList<RoomNameCorrection> corrections)
 {
  using var tx=new Transaction(target,"VEBRAM — restaurar nomes conferidos na origem");tx.Start();
  foreach(var correction in corrections)
  {var room=target.GetElement(correction.UniqueId) as Room??throw new InvalidOperationException("Ambiente ausente.");var name=room.get_Parameter(BuiltInParameter.ROOM_NAME);if(room.Number!=correction.Number||name.IsReadOnly||name.AsString()!=correction.Before)throw new InvalidOperationException("Nome editado após a conferência; operação revertida.");name.Set(correction.After);}
  if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Nomes revertidos pelo Revit.");
 }
}
