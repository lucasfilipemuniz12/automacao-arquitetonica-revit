namespace Vebram.Revit;

// User-authored ROOM_NAME is authoritative; numeric words are not safe to erase.
public static class RoomNameText
{
 public static string Display(string? storedName)=>string.IsNullOrWhiteSpace(storedName)?"Ambiente":storedName.Trim();
 public static bool NeedsReview(string? storedName,string? number)=>!string.IsNullOrWhiteSpace(number)&&Display(storedName).EndsWith(" "+number.Trim(),StringComparison.Ordinal);
 public static string RestoreFromReference(string current,string? number,string original)
 {
  if(string.IsNullOrWhiteSpace(number)||string.IsNullOrWhiteSpace(original))return current;
  string suffix=" "+number.Trim();var candidate=current;int count=0;
  while(candidate.EndsWith(suffix,StringComparison.Ordinal)&&candidate.Length>=original.Length+suffix.Length){candidate=candidate[..^suffix.Length];count++;}
  return count>0&&candidate==original?original:current;
 }
}
