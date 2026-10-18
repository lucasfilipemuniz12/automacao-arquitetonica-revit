using System.Globalization;
using System.Text;
using Autodesk.Revit.DB;
namespace Vebram.Revit;

/// <summary>
/// Coloured area memory: a new plan view where every floor is painted by its physical class (from the floor type),
/// plus a legend with areas summed from the same floors. Model geometry is never changed.
/// Physical classes only; municipal criteria (computável, permeabilidade exigida...) are deliberately not decided here.
/// </summary>
public static class MemoryAreas
{
 public const string ViewName="02 PROJETO LEGAL - VEBRAM - Memória gráfica de áreas";
 const string ViewMarker="VEBRAM-MEMORIA";
 const string LegendMarker="VEBRAM-MEMORIA-LEGENDA";
 sealed record AreaClass(string Key,string Label,byte R,byte G,byte B);
 static readonly AreaClass Interna=new("interna","Área interna",230,160,110),Externa=new("externa","Área externa",190,190,190),
  Garagem=new("garagem","Garagem coberta",205,150,190),Deck=new("deck","Deck",150,105,60),Piscina=new("piscina","Piscina",90,170,230),
  Impermeavel=new("impermeavel","Calçada e passeio (impermeável)",120,120,130),Permeavel=new("permeavel","Gramado (classe física)",120,190,100);
 /// <summary>Short identifier shown in the legend and on the plan.</summary>
 static string ClassId(AreaClass c)=>c.Key switch{"interna"=>"A","externa"=>"E","garagem"=>"G","deck"=>"D","piscina"=>"P","impermeavel"=>"I","permeavel"=>"AP",_=>c.Key.ToUpperInvariant()};
 static string Normalize(string s)
 {
  var d=s.Normalize(NormalizationForm.FormD);var sb=new StringBuilder();
  foreach(var c in d)if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)sb.Append(char.ToUpperInvariant(c));
  return sb.ToString();
 }
 /// <summary>Returns the class of a floor type, or null with hidden=true for structure that would overlap other floors.</summary>
 static AreaClass? Classify(string typeName,out bool hidden)
 {
  var n=Normalize(typeName);hidden=false;
  if(n.Contains("RADI")||n.Contains("LAJE")){hidden=true;return null;}
  if(n.Contains("PISCINA"))return Piscina;
  if(n.Contains("DECK"))return Deck;
  if(n.Contains("INTERNO"))return Interna;
  if(n.Contains("EXTERNO"))return Externa;
  if(n.Contains("CALCADA")||n.Contains("PASSEIO"))return Impermeavel;
  if(n.Contains("GRAMADO")||n.Contains("GRAMA"))return Permeavel;
  return null;
 }
 static double SquareMeters(Element e)=>(e.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble()??0)*0.09290304;
 static string Number(double value)=>value.ToString("0.00",CultureInfo.GetCultureInfo("pt-BR"));

 public static ModuleResult Apply(Document doc,ViewPlan source,bool replaceEdited=false)
 {
  if(doc.IsReadOnly||doc.IsFamilyDocument||doc.IsWorkshared)throw new InvalidOperationException("Use um RVT local editável.");
  var warnings=new List<string>();
  var floors=new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().Where(f=>f.LevelId==source.GenLevel.Id).ToList();
  if(floors.Count==0)throw new InvalidOperationException("Não há pisos modelados neste nível.");
  var byType=floors.GroupBy(f=>doc.GetElement(f.GetTypeId())?.Name??"(sem tipo)").OrderBy(g=>g.Key).ToList();
  using var tx=new Transaction(doc,"VEBRAM — memória gráfica de áreas");tx.Start();
  try
  {
   var view=new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(v=>!v.IsTemplate&&v.Name==ViewName);
   if(view!=null&&view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION)?.AsString()!=ViewMarker&&MemoryAreaState.Read(view)==null)
    throw new InvalidOperationException("Já existe uma vista manual chamada \""+ViewName+"\". Renomeie-a antes de gerar a memória gráfica.");
   if(view==null)
   {
    // A generated memory that was renamed is no longer found by name; refuse instead of creating a duplicate view.
    var renamed=new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(v=>!v.IsTemplate&&MemoryAreaState.Read(v)!=null);
    if(renamed!=null)throw new InvalidOperationException("A memória gráfica já existe com outro nome: \""+renamed.Name+"\". Volte ao nome \""+ViewName+"\" para atualizá-la sem criar uma vista duplicada.");
   }
   var previous=view==null?null:MemoryAreaState.Read(view);
   if(previous!=null&&previous.Source!=source.UniqueId)throw new InvalidOperationException("A memória usa outra planta-fonte. Preserve a vista existente antes de mudar a fonte.");
   if(previous!=null&&!replaceEdited&&previous.Edited(doc))throw new InvalidOperationException("A legenda gerada foi editada. Confira as alterações e marque a opção de refazê-la.");
   if(view!=null&&previous==null&&!replaceEdited)throw new InvalidOperationException("Esta memória é de uma versão anterior, sem registro das edições da legenda. Confira e autorize sua atualização na configuração.");
   bool initialize=view==null||!LegalCatalog.IsLegal(view);
   if(view==null)
   {
    view=(ViewPlan)doc.GetElement(source.Duplicate(ViewDuplicateOption.Duplicate));view.Name=ViewName;
    view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION)?.Set(ViewMarker);
   }
   // A view template would override the filter colours in this generated view only.
   if(view.ViewTemplateId!=ElementId.InvalidElementId)view.ViewTemplateId=ElementId.InvalidElementId;
   foreach(var pair in new[]{(LegalCatalog.StageId,LegalCatalog.Stage),(LegalCatalog.FunctionId,"MEMORIA_AREAS")})
   {var p=view.get_Parameter(pair.Item1);if(p==null||p.IsReadOnly)throw new InvalidOperationException("Classificação ARQ indisponível na memória gráfica.");p.Set(pair.Item2);}
   if(initialize)view.Scale=LegalPreferenceStore.Read(doc).Scale;
   Ownership.Write(view,"legal-v02/memoria",source.UniqueId);
   view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION)?.Set("Memória gráfica de áreas");
   // Navigation clutter inherited from the source plan (grids, elevation/section/callout marks) is hidden in this view only.
   foreach(var builtIn in new[]{BuiltInCategory.OST_Grids,BuiltInCategory.OST_Elev,BuiltInCategory.OST_Sections,BuiltInCategory.OST_Callouts})
   {var category=doc.Settings.Categories.get_Item(builtIn);if(category!=null&&view.CanCategoryBeHidden(category.Id))view.SetCategoryHidden(category.Id,true);}
   // Previous legend (generated, marked) is rebuilt; nothing else in the view is touched.
   var oldLegend=new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType().Where(e=>e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()==LegendMarker).Select(e=>e.Id).ToList();
   if(previous!=null)oldLegend=previous.Legend.Select(s=>doc.GetElement(s.UniqueId)).Where(e=>e!=null&&e.OwnerViewId==view.Id).Select(e=>e!.Id).ToList();
   if(oldLegend.Count>0){var allowed=oldLegend.ToHashSet();
    foreach(var region in oldLegend.Select(id=>doc.GetElement(id)).OfType<FilledRegion>())
     foreach(var dependent in region.GetDependentElements(null))
      if(doc.GetElement(dependent) is Sketch or SketchPlane or CurveElement)allowed.Add(dependent);
    foreach(var sketch in oldLegend.Select(id=>doc.GetElement(id)).OfType<FilledRegion>().SelectMany(region=>region.GetDependentElements(new ElementClassFilter(typeof(Sketch)))).Select(id=>doc.GetElement(id)).OfType<Sketch>())allowed.UnionWith(sketch.GetAllElements());
    if(doc.Delete(oldLegend).Any(id=>!allowed.Contains(id)))throw new InvalidOperationException("Há elementos manuais dependentes da legenda; atualização revertida.");}
   var solid=new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>()
    .FirstOrDefault(p=>p.GetFillPattern().IsSolidFill&&p.GetFillPattern().Target==FillPatternTarget.Drafting)??throw new InvalidOperationException("Padrão de preenchimento sólido não encontrado no projeto.");
   // Per-element overrides (not filters) so a single floor can be told apart, e.g. the covered garage.
   var garageRooms=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Autodesk.Revit.DB.Architecture.Room>()
    .Where(r=>r.LevelId==source.GenLevel.Id&&r.Area>0&&Normalize(r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString()??"").Contains("GARAGEM")).ToList();
   var anyRooms=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Autodesk.Revit.DB.Architecture.Room>().Where(r=>r.LevelId==source.GenLevel.Id&&r.Area>0).ToList();
   // Restore only structural floors hidden by this generator; manual hidden floors remain hidden. Reapply the generated colour scheme.
   var previouslyHidden=(previous?.Hidden??new()).Select(id=>doc.GetElement(id)).OfType<Floor>().Where(f=>f.IsHidden(view)).Select(f=>f.Id).ToList();if(previouslyHidden.Count>0)view.UnhideElements(previouslyHidden);
   foreach(var f in floors)view.SetElementOverrides(f.Id,new OverrideGraphicSettings());
   var totals=new Dictionary<AreaClass,double>();var biggest=new Dictionary<AreaClass,(Floor Floor,double Area)>();var unclassified=new HashSet<string>();var hiddenIds=new List<ElementId>();var notes=new List<string>();
   foreach(var floor in floors)
   {
    string typeName=doc.GetElement(floor.GetTypeId())?.Name??"(sem tipo)";
    var cls=Classify(typeName,out bool hidden);
    if(hidden){hiddenIds.Add(floor.Id);continue;}
    if(cls==null){unclassified.Add(typeName);continue;}
    // A floor that mostly lies under a garage room becomes its own class (sampled on a grid over its box).
    if((cls==Externa||cls==Interna)&&garageRooms.Count>0&&floor.get_BoundingBox(null) is {} b)
    {
     int inGarage=0,inAny=0;
     for(int ix=1;ix<=6;ix++)for(int iy=1;iy<=6;iy++)
     {
      var p=new XYZ(b.Min.X+(b.Max.X-b.Min.X)*ix/7,b.Min.Y+(b.Max.Y-b.Min.Y)*iy/7,source.GenLevel.Elevation+Implantation.Feet(1));
      if(garageRooms.Any(r=>r.IsPointInRoom(p))){inGarage++;inAny++;}else if(anyRooms.Any(r=>r.IsPointInRoom(p)))inAny++;
     }
     if(inAny>0&&inGarage*2>inAny){cls=Garagem;notes.Add("Piso "+floor.Id.Value+" ("+typeName+", "+Number(SquareMeters(floor))+" m²) classificado como garagem coberta por estar sob o ambiente de garagem.");}
    }
    var color=new Color(cls.R,cls.G,cls.B);var ogs=new OverrideGraphicSettings();
    ogs.SetSurfaceForegroundPatternId(solid.Id);ogs.SetSurfaceForegroundPatternColor(color);ogs.SetSurfaceTransparency(0);
    ogs.SetCutForegroundPatternId(solid.Id);ogs.SetCutForegroundPatternColor(color);
    view.SetElementOverrides(floor.Id,ogs);
    totals[cls]=totals.GetValueOrDefault(cls)+SquareMeters(floor);
    var floorArea=SquareMeters(floor);if(!biggest.TryGetValue(cls,out var currentBiggest)||floorArea>currentBiggest.Area)biggest[cls]=(floor,floorArea);
   }
   if(hiddenIds.Count>0)view.HideElements(hiddenIds);
   if(garageRooms.Count>0&&!totals.ContainsKey(Garagem))warnings.Add("Há ambiente de garagem, mas nenhum piso foi identificado sob ele; conferir a classe da garagem.");
   warnings.AddRange(notes);
   // The terrain (toposolid) that shows between the floors is the same grass area as the "Terreno gramado" floors: same class and colour.
   string balance="";
   var terrain=new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Toposolid).WhereElementIsNotElementType().ToList();
   if(terrain.Count>0)
   {
    var grass=new Color(Permeavel.R,Permeavel.G,Permeavel.B);var terrainOverride=new OverrideGraphicSettings();
    terrainOverride.SetSurfaceForegroundPatternId(solid.Id);terrainOverride.SetSurfaceForegroundPatternColor(grass);terrainOverride.SetSurfaceTransparency(0);
    terrainOverride.SetCutForegroundPatternId(solid.Id);terrainOverride.SetCutForegroundPatternColor(grass);
    foreach(var piece in terrain)view.SetElementOverrides(piece.Id,terrainOverride);
    // Arithmetic balance only. No geometric union or permeability measurement is inferred.
    var lot=Implantation.Saved(doc);double painted=totals.Values.Sum();
    if(lot!=null)
    {
     double lotArea=lot.LotWidth*lot.LotDepth,uncovered=lotArea-painted;
     balance="Saldo aritmético lote menos pisos: "+Number(uncovered)+" m². Estimativa sem união de contornos; não é área permeável medida.";
     warnings.Add(balance+" Pisos sobrepostos, pavimentos e áreas fora do lote exigem revisão. O saldo não entra na soma das classes.");
    }
    else warnings.Add("Lote não confirmado na Implantação: o terreno foi pintado como gramado, mas sua área não entra na legenda.");
   }
   if(terrain.Count>0)warnings.Add("Terreno em verde apenas para orientação visual. Sua geometria não foi medida nem classificada como permeável.");
   if(totals.Count==0)warnings.Add("Nenhum piso deste nível tem classe reconhecida pelo nome do tipo (interno, externo, deck, piscina, calçada/passeio, gramado). A legenda fica sem áreas até os tipos serem nomeados ou classificados.");
   foreach(var name in unclassified.Order())warnings.Add("Tipo de piso sem classe: \""+name+"\" (mantido sem cor). Informe a classe para incluí-lo.");
   // Legend to the right of the building: swatch + class + area (summed from the same floors painted above).
   var boxes=floors.Select(f=>f.get_BoundingBox(null)).Where(b=>b!=null).ToList();
   double right=boxes.Max(b=>b!.Max.X),top=boxes.Max(b=>b!.Max.Y);int scale=view.Scale;double z=source.GenLevel.Elevation;
   double Paper(double meters)=>Implantation.Feet(meters)*scale;
   var x0=right+Paper(.012);double y=top;
   var regionBase=new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).Cast<FilledRegionType>().First();
   var legend=new List<Element>();
   void Mark(Element e){e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(LegendMarker);DrawingOwnership.MarkAnnotation(e);legend.Add(e);}
   Mark(TextNote.Create(doc,view.Id,new XYZ(x0,y,z),"MEMÓRIA GRÁFICA DE ÁREAS",new TextNoteOptions(LegalStyles.Text(doc,3.5,true).Id)));
   y-=Paper(.009);int items=1;double sum=0;
   foreach(var pair in totals.OrderBy(p=>p.Key.Label))
   {
    var typeName="VEBRAM Memória - "+pair.Key.Label;
    var regionType=new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).Cast<FilledRegionType>().FirstOrDefault(t=>t.Name==typeName)??(FilledRegionType)regionBase.Duplicate(typeName);
    regionType.ForegroundPatternId=solid.Id;regionType.ForegroundPatternColor=new Color(pair.Key.R,pair.Key.G,pair.Key.B);
    double w=Paper(.008),h=Paper(.005);
    var loop=CurveLoop.Create(new List<Curve>{Line.CreateBound(new XYZ(x0,y-h,z),new XYZ(x0+w,y-h,z)),Line.CreateBound(new XYZ(x0+w,y-h,z),new XYZ(x0+w,y,z)),Line.CreateBound(new XYZ(x0+w,y,z),new XYZ(x0,y,z)),Line.CreateBound(new XYZ(x0,y,z),new XYZ(x0,y-h,z))});
    Mark(FilledRegion.Create(doc,regionType.Id,view.Id,new List<CurveLoop>{loop}));
    Mark(TextNote.Create(doc,view.Id,new XYZ(x0+w+Paper(.003),y,z),Implantation.Feet(.085),ClassId(pair.Key)+" · "+pair.Key.Label+" — "+Number(pair.Value)+" m²",new TextNoteOptions(LegalStyles.Text(doc,2.5).Id)));
    doc.Regenerate();y-=Math.Max(Paper(.008),legend.Last().get_BoundingBox(view) is {} lb?lb.Max.Y-lb.Min.Y+Paper(.003):0);sum+=pair.Value;items+=2;
   }
   Mark(TextNote.Create(doc,view.Id,new XYZ(x0,y-Paper(.002),z),"Soma dos pisos classificados — "+Number(sum)+" m²",new TextNoteOptions(LegalStyles.Text(doc,2.5,true).Id)));
   Mark(TextNote.Create(doc,view.Id,new XYZ(x0,y-Paper(.010),z),Implantation.Feet(.10),"Classes por nome de tipo; soma de áreas, sem união de contornos. Critérios municipais não avaliados. Terreno verde: orientação visual, sem área medida.\n"+balance,new TextNoteOptions(LegalStyles.Text(doc,2).Id)));
   // Class letters on the plan itself (on the biggest floor of each class), same letters as in the legend.
   foreach(var pair in biggest)
    if(pair.Value.Floor.get_BoundingBox(null) is {} fb)
    {var middle=new XYZ((fb.Min.X+fb.Max.X)/2,(fb.Min.Y+fb.Max.Y)/2,z);
     Mark(TextNote.Create(doc,view.Id,middle,ClassId(pair.Key),new TextNoteOptions(LegalStyles.Text(doc,3,true).Id){HorizontalAlignment=HorizontalTextAlignment.Center,VerticalAlignment=VerticalTextAlignment.Middle}));items++;}
   doc.Regenerate();
   var bounds=floors.Select(f=>f.get_BoundingBox(view)??f.get_BoundingBox(null)).Concat(new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==view.Id&&e is TextNote or FilledRegion).Select(e=>e.get_BoundingBox(view))).Where(b=>b!=null).ToList();
   var crop=view.CropBox;var inv=crop.Transform.Inverse;var points=bounds.SelectMany(b=>new[]{inv.OfPoint(b!.Min),inv.OfPoint(b.Max)}).ToList();double margin=Paper(.006);
   crop.Min=new XYZ(points.Min(p=>p.X)-margin,points.Min(p=>p.Y)-margin,crop.Min.Z);crop.Max=new XYZ(points.Max(p=>p.X)+margin,points.Max(p=>p.Y)+margin,crop.Max.Z);view.CropBox=crop;view.CropBoxActive=true;view.CropBoxVisible=false;
   var annotationCrop=view.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);if(annotationCrop is {IsReadOnly:false})annotationCrop.Set(0);
   MemoryAreaState.Write(view,new(source.UniqueId,legend.Select(e=>new ElementStamp(e.UniqueId,MemoryAreaState.Fingerprint(e))).ToList(),hiddenIds.Select(id=>doc.GetElement(id).UniqueId).ToList()));
   if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Memória gráfica revertida pelo Revit.");
   return new(view.Id.Value,0,items+2,"Memória gráfica de áreas gerada na vista \""+ViewName+"\" (cópia da planta, sem o modelo alterado).\n"+
    string.Join("\n",totals.OrderBy(p=>p.Key.Label).Select(p=>"• "+p.Key.Label+": "+Number(p.Value)+" m²"))+"\nLaje/radier ficam ocultos nesta vista para não sobrepor os pisos.\n"+string.Join("\n",warnings));
  }
  catch{if(tx.GetStatus()==TransactionStatus.Started)tx.RollBack();throw;}
 }
}
