using Autodesk.Revit.DB;
namespace Vebram.Revit;

internal static class UrbanisticReference
{
 // Exact unfilled V02 fixture. A changed note or additional note means manual content: keep it.
 static readonly string[] EmptyTexts=[
  "QUADRO URBANÍSTICO - A DEFINIR",
  "Item                         Projeto       Limite local",
  "Área do terreno (m²)       ____         ____\nÁrea computável (m²)       ____         ____\nProjeção da edificação      ____         ____\nTaxa de ocupação (%)       ____         ____\nCoef. aproveitamento       ____         ____\nPermeabilidade (%)         ____         ____\nAfastamentos (m)            ____         ____",
  "Registrar município, zoneamento e critério de área.\nPreencher após consultar a legislação do terreno."
 ];
 internal static int RemoveUnfilledPlacement(Document doc)
 {
  var item=LegalCatalog.Items.First(i=>i.Key=="urbanistico");
  var views=LegalCatalog.Views(doc);
  if(!views.Any(v=>v.Name==item.ReferenceName+" — VEBRAM"))return 0;
  var reference=views.FirstOrDefault(v=>v.Name==item.ReferenceName);if(reference==null)return 0;
  var texts=new FilteredElementCollector(doc,reference.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Select(t=>LegalCatalog.Normalize(t.Text)).Order(StringComparer.Ordinal).ToList();
  if(!texts.SequenceEqual(EmptyTexts.Select(LegalCatalog.Normalize).Order(StringComparer.Ordinal)))return 0;
  int count=0;
  foreach(var vp in new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Where(v=>v.ViewId==reference.Id&&!v.Pinned).ToList())
   if(doc.GetElement(vp.SheetId) is ViewSheet sheet&&LegalSheets.IsLegalSheet(sheet))
   {
    var id=vp.Id;
    if(doc.Delete(id).Any(deleted=>deleted!=id)||doc.GetElement(reference.Id)==null)throw new InvalidOperationException("Dependências impedem retirar a referência vazia com segurança.");
    count++;
   }
  return count;
 }
}
