using Autodesk.Revit.DB;
namespace Vebram.Revit;

// The V03 family has a number label with a literal slash, but no bindable total parameter.
// Complete it on each issued sheet; keep the family/template unchanged.
internal sealed record NumberStampProfile(XYZ Position,double Width,double Size,string Font,bool Bold,bool Italic);
internal static class SheetNumberStamp
{
 const string Marker="VEBRAM-NUMERO-TOTAL";
 internal const string HistoryMarker="VEBRAM-HISTORICO";
 /// <summary>Generated identification on the stamp (number/total or history line); never manual content.</summary>
 // Text notes have no Comments parameter: generated notes are identified by their dedicated text types.
 internal const string NumberType="VEBRAM — identificação número/total",HistoryType="VEBRAM — histórico de revisão";
 static string? TypeName(Element e)=>e is TextNote n?n.Document.GetElement(n.GetTypeId())?.Name:null;
 internal static bool Generated(Element e)=>TypeName(e) is NumberType or HistoryType||e is TextNote&&e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() is Marker or HistoryMarker;
 internal static bool IsNumber(Element e)=>TypeName(e)==NumberType||e is TextNote&&e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()==Marker;
 internal static bool IsHistory(Element e)=>TypeName(e)==HistoryType||e is TextNote&&e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()==HistoryMarker;
 internal static NumberStampProfile? Read(Document doc,FamilyInstance title)=>Read(doc,title.Symbol);
 internal static NumberStampProfile? Read(Document doc,FamilySymbol symbol)
 {
  var family=doc.EditFamily(symbol.Family);
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
  foreach(var note in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Where(IsNumber).ToList())doc.Delete(note.Id);
  var type=LegalStyles.Text(doc,profile.Size*.3048*1000,profile.Bold);
  const string name=NumberType;
  var stampType=new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault(t=>t.Name==name)??(TextNoteType)type.Duplicate(name);
  stampType.get_Parameter(BuiltInParameter.TEXT_FONT).Set(profile.Font);stampType.get_Parameter(BuiltInParameter.TEXT_STYLE_ITALIC).Set(profile.Italic?1:0);
  stampType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(0);
  var title=SheetPlanner.TitleOf(doc,sheet)!;
  var text=TextNote.Create(doc,sheet.Id,title.GetTransform().OfPoint(profile.Position),profile.Width,sheet.SheetNumber+"/"+total.ToString("00"),new TextNoteOptions(stampType.Id){HorizontalAlignment=HorizontalTextAlignment.Left,VerticalAlignment=VerticalTextAlignment.Top});
  text.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(Marker);DrawingOwnership.MarkAnnotation(text);
 }
}

/// <summary>
/// One line in the "HISTÓRICO DE REVISÕES" box of the issued LEG sheets, written only with the text the user supplies
/// (revision, description, date). Reserves get none. The title block family and the template are not changed.
/// </summary>
internal static class SheetHistory
{
 static XYZ? HeaderPosition(Document doc,FamilyInstance title)
 {
  var family=doc.EditFamily(title.Symbol.Family);
  try
  {
   var header=new FilteredElementCollector(family).OfClass(typeof(TextNote)).Cast<TextNote>().FirstOrDefault(t=>LegalCatalog.Normalize(t.Text).Contains(LegalCatalog.Normalize("HISTÓRICO DE REVIS")));
   return header?.Coord;
  }
  finally{family.Close(false);}
 }
 internal static string Apply(Document doc,string line)
 {
  if(string.IsNullOrWhiteSpace(line))throw new InvalidOperationException("Informe a linha de revisão.");
  var sheets=SheetPlanner.Sheets(doc);if(sheets.Count==0)throw new InvalidOperationException("Não há folhas LEG emitidas.");
  var title=SheetPlanner.TitleOf(doc,sheets[0])??throw new InvalidOperationException("Folha sem carimbo.");
  var header=HeaderPosition(doc,title)??throw new InvalidOperationException("Campo HISTÓRICO DE REVISÕES não encontrado no carimbo; nada foi escrito.");
  using var tx=new Transaction(doc,"VEBRAM — histórico de revisões das folhas emitidas");tx.Start();
  try
  {
   var baseType=LegalStyles.Text(doc,2);int count=0;
   var type=new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault(t=>t.Name==SheetNumberStamp.HistoryType)??(TextNoteType)baseType.Duplicate(SheetNumberStamp.HistoryType);
   foreach(var sheet in sheets)
   {
    foreach(var old in new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Where(SheetNumberStamp.IsHistory).ToList())doc.Delete(old.Id);
    // The template's "00 | Folha-base" reminder occupies the same box; hide it on issued sheets only.
    var placeholder=new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>n.OwnerViewId==sheet.Id&&!n.IsHidden(sheet)&&n.Text.StartsWith("00 | Folha-base")).Select(n=>n.Id).ToList();
    if(placeholder.Count>0)sheet.HideElements(placeholder);
    var stampTitle=SheetPlanner.TitleOf(doc,sheet);if(stampTitle==null)continue;
    var point=stampTitle.GetTransform().OfPoint(header+new XYZ(0,-Implantation.Feet(.009),0));
    var text=TextNote.Create(doc,sheet.Id,point,Implantation.Feet(.13),line.Trim(),new TextNoteOptions(type.Id){HorizontalAlignment=HorizontalTextAlignment.Left,VerticalAlignment=VerticalTextAlignment.Top});
    text.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(SheetNumberStamp.HistoryMarker);DrawingOwnership.MarkAnnotation(text);count++;
   }
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Histórico revertido pelo Revit.");
   return "Histórico preenchido em "+count+" folha(s) emitida(s): \""+line.Trim()+"\". Reservas sem histórico.";
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
}
