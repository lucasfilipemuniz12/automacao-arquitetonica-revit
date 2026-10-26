using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;

/// <summary>Notes the user typed: general ones (all legal sheets) and per sheet. Stored in the project, never invented.</summary>
public sealed record UserNotes(string General,Dictionary<string,string> Sheets);

internal static class SheetNotes
{
 static readonly Guid Id=new("76e1bd79-e82b-4ce1-9acb-bec5a1ae1018");
 static readonly Guid UserId=new("a1c8f5d2-0b64-4e6e-9a3b-6f0d7c1e2b55");
 internal static bool IsUnedited(Element element)
 {var schema=Schema.Lookup(Id);if(schema==null||element is not TextNote note||note.Pinned)return false;var entity=note.GetEntity(schema);return entity.IsValid()&&entity.Get<string>(schema.GetField("Fingerprint"))==DrawingOwnership.Fingerprint(note);}
 internal static void ClearUnedited(Document doc,ElementId sheetId)
 {
  var schema=Schema.Lookup(Id);if(schema==null)return;
  foreach(var note in new FilteredElementCollector(doc,sheetId).OfClass(typeof(TextNote)).Cast<TextNote>().ToList())
  {var entity=note.GetEntity(schema);if(!entity.IsValid()||note.Pinned||entity.Get<string>(schema.GetField("Fingerprint"))!=DrawingOwnership.Fingerprint(note))continue;var id=note.Id;if(doc.Delete(id).Any(d=>d!=id))throw new InvalidOperationException("Dependências nas notas impedem atualização segura.");}
 }

 // ---- user notes (stored on the Project Information element) ----
 static Schema UserSchema()
 {var schema=Schema.Lookup(UserId);if(schema!=null)return schema;var builder=new SchemaBuilder(UserId);builder.SetSchemaName("VebramUserNotes021");builder.AddSimpleField("Payload",typeof(string));return builder.Finish();}
 internal static UserNotes ReadUser(Document doc)
 {
  var schema=Schema.Lookup(UserId);if(schema==null)return new("",new());
  var entity=doc.ProjectInformation.GetEntity(schema);if(!entity.IsValid())return new("",new());
  try{return JsonSerializer.Deserialize<UserNotes>(entity.Get<string>(schema.GetField("Payload")))??new("",new());}catch(JsonException){return new("",new());}
 }
 /// <summary>Must run inside a transaction.</summary>
 internal static void SaveUser(Document doc,UserNotes notes)
 {var schema=UserSchema();var entity=new Entity(schema);entity.Set(schema.GetField("Payload"),JsonSerializer.Serialize(notes));doc.ProjectInformation.SetEntity(entity);}
 static IEnumerable<string> Lines(string text)=>(text??"").Replace("\r","").Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0);

 /// <summary>Automatic lines (from what is on the sheet) plus the user's own lines.</summary>
 internal static List<string> Compose(Document doc,string sheetNumber,IEnumerable<string> labels)
 {
  var labelList=labels.ToList();
  var notes=new List<string>{"Escalas indicadas nos títulos das vistas.","Conferir cotas e informações antes da emissão."};
  if(labelList.Any(l=>l is "Implantação" or "Situação" or "Quadro urbanístico"))notes.Add("Lote, confrontantes e índices: dados informados no projeto.");
  if(labelList.Any(l=>l.StartsWith("Corte")||l.StartsWith("Fachada")))notes.Add("Alturas e níveis: conferir referências indicadas nas vistas.");
  var user=ReadUser(doc);notes.AddRange(Lines(user.General));
  if(user.Sheets.TryGetValue(sheetNumber,out var own))notes.AddRange(Lines(own));
  if(Implantation.Saved(doc)?.Study==true)notes.Add("ESTUDO FICTÍCIO — NÃO PROTOCOLAR.");
  return notes;
 }
 static bool Place(Document doc,ViewSheet sheet,PlannerZone zone,List<string> notes,List<string> warnings)
 {
  double inset=Implantation.Feet(.003);var text=TextNote.Create(doc,sheet.Id,new XYZ(zone.Rect.Left+inset,zone.Rect.Top-inset,0),zone.Rect.Right-zone.Rect.Left-2*inset,string.Join("\n",notes),new TextNoteOptions(LegalStyles.Text(doc,2).Id));doc.Regenerate();var rect=LegalSheets.Rect(text,sheet);
  if(rect==null||!zone.Rect.Contains(rect.Value,1e-6)){doc.Delete(text.Id);warnings.Add(sheet.SheetNumber+": as notas não couberam no campo lateral; reduza o texto.");return false;}
  var schema=Schema.Lookup(Id);if(schema==null){var builder=new SchemaBuilder(Id);builder.SetSchemaName("VebramSheetNotes018");builder.AddSimpleField("Fingerprint",typeof(string));schema=builder.Finish();}DrawingOwnership.MarkAnnotation(text);var entity=new Entity(schema);entity.Set(schema.GetField("Fingerprint"),DrawingOwnership.Fingerprint(text));text.SetEntity(entity);return true;
 }
 internal static void Draw(Document doc,ViewSheet sheet,PlannerPage page,List<string> warnings)
 {
  var zone=page.Zones?.FirstOrDefault(z=>z.Key=="notas");if(zone==null)return;
  if(page.Obstacles.Any(o=>o.Intersects(zone.Rect,0))){warnings.Add(sheet.SheetNumber+": notas existentes preservadas; notas automáticas não inseridas.");return;}
  var notes=Compose(doc,sheet.SheetNumber,page.Drawings.Select(d=>d.Label));
  if(Place(doc,sheet,zone,notes,warnings))page.Notes?.AddRange(notes);
 }

 /// <summary>Redraws the generated notes of every legal sheet with the current user notes. Notes edited by hand are kept.</summary>
 internal static string Refresh(Document doc)
 {
  var sheets=SheetPlanner.Sheets(doc);var items=SheetPlanner.Items(doc);var warnings=new List<string>();int drawn=0;
  var reference=sheets.FirstOrDefault(s=>SheetPlanner.TitleOf(doc,s)!=null)??throw new InvalidOperationException("É necessária uma folha LEG com carimbo.");
  var profile=SheetZones.Read(doc,SheetPlanner.TitleOf(doc,reference)!);
  using var tx=new Transaction(doc,"VEBRAM — notas das folhas");tx.Start();
  try
  {
   foreach(var sheet in sheets)
   {
    if(SheetPlanner.TitleOf(doc,sheet)==null)continue;
    var zone=SheetZones.Map(doc,sheet,profile).Zones.FirstOrDefault(z=>z.Key=="notas");if(zone==null){warnings.Add(sheet.SheetNumber+": campo de notas não reconhecido no carimbo.");continue;}
    ClearUnedited(doc,sheet.Id);
    var obstacles=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==sheet.Id&&!e.IsHidden(sheet)&&!IsUnedited(e)&&e.Category?.Id.Value!=(long)BuiltInCategory.OST_TitleBlocks).Select(e=>LegalSheets.Rect(e,sheet)).Where(r=>r!=null).Select(r=>r!.Value).ToList();
    if(obstacles.Any(o=>o.Intersects(zone.Rect,0))){warnings.Add(sheet.SheetNumber+": já há conteúdo no campo de notas; mantido como está.");continue;}
    var labels=items.Where(i=>i.CurrentSheetId==sheet.Id.Value).Select(i=>i.Label);
    if(Place(doc,sheet,zone,Compose(doc,sheet.SheetNumber,labels),warnings))drawn++;
   }
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Notas revertidas pelo Revit.");
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
  return drawn+" folha(s) com notas atualizadas."+(warnings.Count>0?"\n"+string.Join("\n",warnings):"");
 }
}
