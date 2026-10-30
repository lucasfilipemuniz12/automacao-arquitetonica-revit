using Autodesk.Revit.DB;
namespace Vebram.Revit;

// The V03 family has a number label with a literal slash, but no bindable total parameter.
// Complete it on each issued sheet; keep the family/template unchanged.
internal sealed record NumberStampProfile(XYZ Position,double Width,double Size,string Font,bool Bold,bool Italic);
internal static class SheetNumberStamp
{
 const string Marker="VEBRAM-NUMERO-TOTAL";
 internal static bool Generated(Element e)=>e is TextNote&&e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()==Marker;
 internal static NumberStampProfile? Read(Document doc,FamilyInstance title)
 {
  var family=doc.EditFamily(title.Symbol.Family);
  try
  {
   var label=new FilteredElementCollector(family).OfClass(typeof(TextElement)).Cast<TextElement>().FirstOrDefault(e=>e is not TextNote&&e.Parameters.Cast<Parameter>().Any(p=>p.StorageType==StorageType.String&&p.AsString()?.Trim() is "01/" or "1/"));
   if(label==null)return null;var t=family.GetElement(label.GetTypeId());
   return new(label.Coord,label.Width,t.get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble(),t.get_Parameter(BuiltInParameter.TEXT_FONT).AsString(),t.get_Parameter(BuiltInParameter.TEXT_STYLE_BOLD).AsInteger()==1,t.get_Parameter(BuiltInParameter.TEXT_STYLE_ITALIC).AsInteger()==1);
  }
  finally{family.Close(false);}
 }
 internal static void Draw(Document doc,ViewSheet sheet,NumberStampProfile profile,int total)
 {
  foreach(var note in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Where(Generated).ToList())doc.Delete(note.Id);
  var type=LegalStyles.Text(doc,profile.Size*.3048*1000,profile.Bold);
  const string name="VEBRAM — identificação número/total";
  var stampType=new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault(t=>t.Name==name)??(TextNoteType)type.Duplicate(name);
  stampType.get_Parameter(BuiltInParameter.TEXT_FONT).Set(profile.Font);stampType.get_Parameter(BuiltInParameter.TEXT_STYLE_ITALIC).Set(profile.Italic?1:0);
  stampType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(0);
  var title=SheetPlanner.TitleOf(doc,sheet)!;
  var text=TextNote.Create(doc,sheet.Id,title.GetTransform().OfPoint(profile.Position),profile.Width,sheet.SheetNumber+"/"+total.ToString("00"),new TextNoteOptions(stampType.Id){HorizontalAlignment=HorizontalTextAlignment.Left,VerticalAlignment=VerticalTextAlignment.Top});
  text.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(Marker);DrawingOwnership.MarkAnnotation(text);
 }
}
