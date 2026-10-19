using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;
internal sealed record MemoryAreaState(string Source,List<ElementStamp> Legend,List<string> Hidden)
{
 static readonly Guid Id=new("d260a251-754c-40c7-a67c-e4824e600d25");
 internal static MemoryAreaState? Read(View view)
 {var s=Schema.Lookup(Id);if(s==null)return null;var e=view.GetEntity(s);return e.IsValid()?JsonSerializer.Deserialize<MemoryAreaState>(e.Get<string>(s.GetField("Payload"))):null;}
 internal static string Fingerprint(Element element)=>DrawingOwnership.Fingerprint(element)+(element is FilledRegion region?string.Join(";",region.GetBoundaries().SelectMany(b=>b).Select(c=>c.GetEndPoint(0).ToString()+c.GetEndPoint(1).ToString())):"");
 internal static void Write(View view,MemoryAreaState state)
 {var s=Schema.Lookup(Id);if(s==null){var b=new SchemaBuilder(Id);b.SetSchemaName("VebramMemoryAreas025");b.AddSimpleField("Payload",typeof(string));s=b.Finish();}var e=new Entity(s);e.Set(s.GetField("Payload"),JsonSerializer.Serialize(state));view.SetEntity(e);}
 internal bool Edited(Document doc)=>Legend.Any(stamp=>doc.GetElement(stamp.UniqueId) is not {} e||e.Pinned||Fingerprint(e)!=stamp.Fingerprint);
}
