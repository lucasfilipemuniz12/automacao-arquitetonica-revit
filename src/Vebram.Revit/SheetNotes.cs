using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;
internal static class SheetNotes
{
 static readonly Guid Id=new("76e1bd79-e82b-4ce1-9acb-bec5a1ae1018");
 internal static bool IsUnedited(Element element)
 {var schema=Schema.Lookup(Id);if(schema==null||element is not TextNote note||note.Pinned)return false;var entity=note.GetEntity(schema);return entity.IsValid()&&entity.Get<string>(schema.GetField("Fingerprint"))==DrawingOwnership.Fingerprint(note);}
 internal static void ClearUnedited(Document doc,ElementId sheetId)
 {
  var schema=Schema.Lookup(Id);if(schema==null)return;
  foreach(var note in new FilteredElementCollector(doc,sheetId).OfClass(typeof(TextNote)).Cast<TextNote>().ToList())
  {var entity=note.GetEntity(schema);if(!entity.IsValid()||note.Pinned||entity.Get<string>(schema.GetField("Fingerprint"))!=DrawingOwnership.Fingerprint(note))continue;var id=note.Id;if(doc.Delete(id).Any(d=>d!=id))throw new InvalidOperationException("Dependências nas notas impedem atualização segura.");}
 }
 internal static void Draw(Document doc,ViewSheet sheet,PlannerPage page,List<string> warnings)
 {
  var zone=page.Zones?.FirstOrDefault(z=>z.Key=="notas");if(zone==null)return;
  if(page.Obstacles.Any(o=>o.Intersects(zone.Rect,0))){warnings.Add(sheet.SheetNumber+": notas existentes preservadas; notas automáticas não inseridas.");return;}
  var notes=new List<string>{"Escalas indicadas nos títulos das vistas.","Conferir cotas e informações antes da emissão."};
  if(page.Drawings.Any(d=>d.Label is "Implantação" or "Situação" or "Quadro urbanístico"))notes.Add("Lote, confrontantes e índices: dados informados no projeto.");
  if(page.Drawings.Any(d=>d.Label.StartsWith("Corte")||d.Label.StartsWith("Fachada")))notes.Add("Alturas e níveis: conferir referências indicadas nas vistas.");
  if(Implantation.Saved(doc)?.Study==true)notes.Add("ESTUDO FICTÍCIO — NÃO PROTOCOLAR.");
  double inset=Implantation.Feet(.003);var text=TextNote.Create(doc,sheet.Id,new XYZ(zone.Rect.Left+inset,zone.Rect.Top-inset,0),zone.Rect.Right-zone.Rect.Left-2*inset,string.Join("\n",notes),new TextNoteOptions(LegalStyles.Text(doc,2).Id));doc.Regenerate();var rect=LegalSheets.Rect(text,sheet);
  if(rect==null||!zone.Rect.Contains(rect.Value,1e-6)){doc.Delete(text.Id);warnings.Add(sheet.SheetNumber+": notas não couberam no campo lateral.");return;}
  var schema=Schema.Lookup(Id);if(schema==null){var builder=new SchemaBuilder(Id);builder.SetSchemaName("VebramSheetNotes018");builder.AddSimpleField("Fingerprint",typeof(string));schema=builder.Finish();}DrawingOwnership.MarkAnnotation(text);var entity=new Entity(schema);entity.Set(schema.GetField("Fingerprint"),DrawingOwnership.Fingerprint(text));text.SetEntity(entity);page.Notes?.AddRange(notes);
 }
}
