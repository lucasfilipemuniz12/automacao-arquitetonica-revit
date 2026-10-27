using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
namespace Vebram.Revit;

public sealed record ContextSelection(string Key,long SourceId,string Identification="",string LeftNeighbor="",string RightNeighbor="",string RearNeighbor="",double? ComputableArea=null,double? PermeableArea=null,double? MaximumOccupation=null,double? MaximumCoefficient=null,double? MinimumPermeability=null,bool Confirmed=false,bool ReplaceEdited=false,string RegistrationText="");
record ContextStamp(string Source,List<ElementStamp> Elements,ContextSelection Input);

/// <summary>Situation diagram and urbanistic worksheet using explicitly confirmed project data.</summary>
public static class LegalContext
{
 static readonly Guid SchemaId=new("63465842-6c64-46d4-b3aa-0a92c7e329e4");
 public static ContextSelection? Saved(Document doc,string key)
 {var view=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key==key));return view==null?null:Read(view)?.Input;}
 public static ModuleResult Apply(Document doc,ContextSelection input)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  if(input.Key is not ("situacao" or "urbanistico")||!input.Confirmed)throw new InvalidOperationException("Confira os dados do lote e confirme as informações.");
  var options=Implantation.Saved(doc)??throw new InvalidOperationException("Prepare a Implantação e confirme seu lote antes deste entregável.");
  if(options.SourceId!=input.SourceId)throw new InvalidOperationException("Use a mesma planta fonte da Implantação.");
  _=Implantation.Plan(doc,options); // Revalidate dimensions, source and floor against the current project.
  var values=new[]{input.ComputableArea,input.PermeableArea,input.MaximumOccupation,input.MaximumCoefficient,input.MinimumPermeability};
  if(values.Any(v=>v.HasValue&&(!double.IsFinite(v.Value)||v<0)))throw new InvalidOperationException("Áreas e índices devem ser números positivos ou zero.");
  if(new[]{input.Identification,input.LeftNeighbor,input.RightNeighbor,input.RearNeighbor}.Any(s=>s.Length>120))throw new InvalidOperationException("Use até 120 caracteres por identificação ou confrontante.");
  double area=options.LotWidth*options.LotDepth;
  if(input.PermeableArea>area||input.MaximumOccupation>100||input.MinimumPermeability>100)throw new InvalidOperationException("Confira área permeável e percentuais entre 0 e 100%.");
  var item=LegalCatalog.Items.First(i=>i.Key==input.Key);var view=LegalCatalog.Reference(LegalCatalog.Views(doc),item)??throw new InvalidOperationException("Entregável de referência ausente no V02.");
  var source=(ViewPlan)doc.GetElement(new ElementId(input.SourceId));var previous=Read(view);
  if(previous!=null&&previous.Source!=source.UniqueId)throw new InvalidOperationException("O entregável utiliza outra fonte.");
  if(previous!=null&&!input.ReplaceEdited&&previous.Elements.Any(s=>doc.GetElement(s.UniqueId) is not {} e||DrawingOwnership.Fingerprint(e)!=s.Fingerprint))throw new InvalidOperationException("Marcações editadas: confira e marque a opção de refazê-las.");
  using var tx=new Transaction(doc,"VEBRAM — "+item.Label);tx.Start();
  try
  {
   // The V02 reference may already occupy the title-block sidebar. Keep that content
   // and its placement intact; the complete generated table needs its own paper area.
   if(input.Key=="urbanistico"&&previous==null&&view.Name==item.ReferenceName)
   {
    var reference=view;view=ViewDrafting.Create(doc,reference.GetTypeId());view.Name=item.ReferenceName+" — VEBRAM";
    foreach(var parameter in new[]{LegalCatalog.StageId,LegalCatalog.FunctionId})
     if(view.get_Parameter(parameter) is {IsReadOnly:false} target)target.Set(LegalCatalog.Text(reference,parameter));
   }
   if(previous!=null){var ids=previous.Elements.Select(s=>doc.GetElement(s.UniqueId)).Where(e=>e!=null).Select(e=>e!.Id).ToList();if(ids.Count>0){var allowed=ids.ToHashSet();if(doc.Delete(ids).Any(id=>!allowed.Contains(id)))throw new InvalidOperationException("Existem elementos dependentes das marcações. Operação revertida.");}}
   var made=new List<Element>();var type=TextType(doc);var report=new List<string>();
   if(input.Key=="situacao")
   {
    if(view is ViewPlan old&&old.GenLevel.Id!=source.GenLevel.Id)
    {
     old.Name="RESERVA - "+item.ReferenceName+" - "+old.Id.Value;
     var replacement=ViewPlan.Create(doc,old.GetTypeId(),source.GenLevel.Id);replacement.Name=item.ReferenceName;
     foreach(var parameter in new[]{LegalCatalog.StageId,LegalCatalog.FunctionId})
      if(replacement.get_Parameter(parameter) is {IsReadOnly:false} target)target.Set(LegalCatalog.Text(old,parameter));
     view=replacement;
     report.Add("Situação criada no nível da planta-fonte: "+source.GenLevel.Name+". Vista anterior preservada como reserva.");
    }
    view.ViewTemplateId=ElementId.InvalidElementId;view.Scale=500;
    var annotationCrop=view.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);if(annotationCrop is {IsReadOnly:false})annotationCrop.Set(0);
    // A localization diagram intentionally excludes the physical building; it is not a surveyed map.
    foreach(Category category in doc.Settings.Categories)if(category.CategoryType==CategoryType.Model&&category.Id.Value!=(long)BuiltInCategory.OST_Lines&&view.CanCategoryBeHidden(category.Id))view.SetCategoryHidden(category.Id,true);
    var lines=doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);if(view.CanCategoryBeHidden(lines.Id))view.SetCategoryHidden(lines.Id,false);foreach(Category sub in lines.SubCategories)if(view.CanCategoryBeHidden(sub.Id))view.SetCategoryHidden(sub.Id,false);
    // Elevation and section markers belong to model navigation, not the localization diagram.
    foreach(var builtIn in new[]{BuiltInCategory.OST_Elev,BuiltInCategory.OST_Sections,BuiltInCategory.OST_RoomSeparationLines}){var category=doc.Settings.Categories.get_Item(builtIn);if(category!=null&&view.CanCategoryBeHidden(category.Id))view.SetCategoryHidden(category.Id,true);}
    double z=((ViewPlan)view).GenLevel.Elevation,x=Implantation.Feet(options.LotMinX),y=Implantation.Feet(options.LotMinY),w=Implantation.Feet(options.LotWidth),h=Implantation.Feet(options.LotDepth);
    XYZ P(double a,double b)=>new(a,b,z);
    DetailCurve L(XYZ a,XYZ b){var c=doc.Create.NewDetailCurve(view,Line.CreateBound(a,b));view.SetElementOverrides(c.Id,new OverrideGraphicSettings().SetProjectionLineColor(new Color(0,0,0)).SetProjectionLineWeight(3));made.Add(c);return c;}
    void Text(double a,double b,string value)=>made.Add(TextNote.Create(doc,view.Id,P(a,b),value,type.Id));
    var edges=new[]{L(P(x,y),P(x+w,y)),L(P(x+w,y),P(x+w,y+h)),L(P(x+w,y+h),P(x,y+h)),L(P(x,y+h),P(x,y))};
    double gap=Implantation.Feet(3);
    void Dimension(int a,int b,XYZ p,XYZ q){var refs=new ReferenceArray();refs.Append(edges[a].GeometryCurve.Reference??new Reference(edges[a]));refs.Append(edges[b].GeometryCurve.Reference??new Reference(edges[b]));doc.Regenerate();made.Add(doc.Create.NewDimension(view,Line.CreateBound(p,q),refs,LegalPreferenceStore.Dimension(doc)));}
    Dimension(3,1,P(x,y-gap),P(x+w,y-gap));Dimension(0,2,P(x-gap,y),P(x-gap,y+h));
    // Text wrapping widths are in paper units. Neighbor names are outside the highlighted lot.
    void Wrapped(double a,double b,double paperWidth,string value)=>made.Add(TextNote.Create(doc,view.Id,P(a,b),Implantation.Feet(paperWidth),value,new TextNoteOptions(type.Id)));
    // At 1:500 a 12 m lot is only 24 mm wide on paper. Keep long identification outside it.
    Wrapped(x+w*.05,y+h*.60,options.LotWidth/view.Scale*.9,"LOTE\n"+area.ToString("0.00",Implantation.Pt)+" m²");
    Wrapped(x-Implantation.Feet(35),y+h*.7,.05,"CONFRONTANTE ESQUERDO\n"+Missing(input.LeftNeighbor));
    Wrapped(x+w+Implantation.Feet(5),y+h*.7,.05,"CONFRONTANTE DIREITO\n"+Missing(input.RightNeighbor));
    Wrapped(x,y+h+Implantation.Feet(10),.065,"CONFRONTANTE AO FUNDO\n"+Missing(input.RearNeighbor));
    var side=options.Front;double offset=Implantation.Feet(options.SidewalkWidth+options.StreetWidth);
    if(options.AddStreet)
    {
     if(side=="Direita"||side=="Esquerda"){double xx=side=="Direita"?x+w+offset:x-offset;L(P(xx,y-gap),P(xx,y+h+gap));Text(xx+(side=="Direita"?gap:-Implantation.Feet(25)),y-gap-Implantation.Feet(6),"VIA: "+options.StreetName);}
     else{double yy=side=="Acima"?y+h+offset:y-offset;L(P(x-gap,yy),P(x+w+gap,yy));Text(x,yy+(side=="Acima"?Implantation.Feet(8):-Implantation.Feet(8)),"VIA: "+options.StreetName);}
    }
    double radians=options.NorthDegrees*Math.PI/180;var north=new XYZ(-Math.Sin(radians),Math.Cos(radians),0);var tail=P(x+w+Implantation.Feet(35),y+h);var tip=tail+north*Implantation.Feet(8);var right=new XYZ(north.Y,-north.X,0);L(tail,tip);L(tip,tip-north*Implantation.Feet(2)+right*Implantation.Feet(1));L(tip,tip-north*Implantation.Feet(2)-right*Implantation.Feet(1));Text(tip.X,tip.Y+Implantation.Feet(3),"N");
    Wrapped(x-Implantation.Feet(35),y-Implantation.Feet(12),.13,"LOTE: "+(string.IsNullOrWhiteSpace(input.Identification)?"identificação não informada":input.Identification));
    Wrapped(x-Implantation.Feet(35),y-Implantation.Feet(23),.13,(options.Study?"ESTUDO FICTÍCIO — ":"")+"SITUAÇÃO ESQUEMÁTICA · conferir levantamento e cadastro municipal");
    if(!string.IsNullOrWhiteSpace(input.RegistrationText))Wrapped(x-Implantation.Feet(35),y-Implantation.Feet(37),.11,ProjectDataIntegration.CompactForDrawing(input.RegistrationText));
    doc.Regenerate();var box=view.CropBox;var inverse=box.Transform.Inverse;var boxes=made.Select(e=>e.get_BoundingBox(view)).Where(b=>b!=null).ToList();var points=boxes.SelectMany(b=>new[]{inverse.OfPoint(b!.Min),inverse.OfPoint(b.Max)}).ToList();box.Min=new(points.Min(p=>p.X)-gap,points.Min(p=>p.Y)-gap,box.Min.Z);box.Max=new(points.Max(p=>p.X)+gap,points.Max(p=>p.Y)+gap,box.Max.Z);view.CropBox=box;view.CropBoxActive=true;view.CropBoxVisible=false;
    report.Add("Diagrama com lote confirmado, confrontantes informados, via e norte. Não equivale a mapa cadastral ou levantamento georreferenciado.");
   }
   else
   {
    view.ViewTemplateId=ElementId.InvalidElementId;view.Scale=1;
    var occupation=OccupationFootprint.Measure(doc,options);double projection=occupation.Total;
    UrbanisticTable.Draw(doc,view,input,options,projection,made,occupation);
    report.Add("Lajes no contorno da TO: "+string.Join("; ",occupation.Parts.Select(p=>p.Label+" = "+p.Area.ToString("0.00",Implantation.Pt)+" m²"))+". Projeção total: "+projection.ToString("0.00",Implantation.Pt)+" m²."+(occupation.Parts.Count==1?" Somente a laje principal (padrão); outras lajes podem ser marcadas na Implantação.":""));
    report.Add("TO usa o contorno base, ajustado às faces externas adjacentes; CA/TP usam somente as áreas que você informou. Limites não informados ficam pendentes. Não declara aprovação urbanística.");
   }
   doc.Regenerate();foreach(var e in made)DrawingOwnership.MarkAnnotation(e);Write(view,new(source.UniqueId,made.Select(e=>new ElementStamp(e.UniqueId,DrawingOwnership.Fingerprint(e))).ToList(),input));
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("O Revit reverteu a operação.");
   return new(view.Id.Value,made.OfType<Dimension>().Count(),made.Count,string.Join("\n",report));
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
 static string Missing(string value)=>string.IsNullOrWhiteSpace(value)?"NÃO INFORMADO":value.Trim();
 static string Number(double value)=>value.ToString("0.00",Implantation.Pt);
 static string Optional(double? value,string suffix)=>value.HasValue?Number(value.Value)+suffix:"NÃO INFORMADO";
 internal static TextNoteType TextType(Document doc)=>LegalStyles.Text(doc);
 static Schema Schema(){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s!=null)return s;var b=new SchemaBuilder(SchemaId);b.SetSchemaName("VebramLegalContext05");b.AddSimpleField("Json",typeof(string));return b.Finish();}
 static ContextStamp? Read(View view){var s=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(SchemaId);if(s==null)return null;var e=view.GetEntity(s);return e.IsValid()?JsonSerializer.Deserialize<ContextStamp>(e.Get<string>(s.GetField("Json"))):null;}
 static void Write(View view,ContextStamp state){var s=Schema();var e=new Entity(s);e.Set(s.GetField("Json"),JsonSerializer.Serialize(state));view.SetEntity(e);}
}





