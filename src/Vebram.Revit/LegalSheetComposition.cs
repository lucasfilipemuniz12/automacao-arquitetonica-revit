using Autodesk.Revit.DB;
namespace Vebram.Revit;

public sealed record SheetCompositionResult(long SheetId,int HiddenGuides,int RemovedEmptySheets,string Report,int RemovedDuplicateSchedules=0);

/// <summary>Explicit V02 composition command. Never runs as a side effect of preparing a view.</summary>
public static class LegalSheetComposition
{
 static readonly string[][][] Layout=
 {
  new[]{new[]{"planta-1"},new[]{"situacao","urbanistico"}},
  new[]{new[]{"cobertura","fachada-oeste"},new[]{"fachada-norte","fachada-leste"},new[]{"fachada-sul"}},
  new[]{new[]{"implantacao"},new[]{"corte-a","corte-b"}}
 };
 static readonly Dictionary<string,string> Titles=new()
 {
  ["planta-1"]="Planta legal — nível 1",["situacao"]="Situação",["urbanistico"]="Quadro urbanístico",
  ["cobertura"]="Cobertura",["implantacao"]="Implantação",["corte-a"]="Corte A",["corte-b"]="Corte B",
  ["fachada-norte"]="Fachada Norte",["fachada-sul"]="Fachada Sul",["fachada-leste"]="Fachada Leste",["fachada-oeste"]="Fachada Oeste"
 };
 public static LegalPaperRect Area(Document doc,ViewSheet sheet)
 {
  var box=new FilteredElementCollector(doc,sheet.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().FirstOrDefault()?.get_BoundingBox(sheet)??throw new InvalidOperationException("Carimbo ausente: "+sheet.SheetNumber);
  double width=Implantation.Meters(box.Max.X-box.Min.X),height=Implantation.Meters(box.Max.Y-box.Min.Y);
  if(Math.Abs(width-.841)>.025||Math.Abs(height-.594)>.025)throw new InvalidOperationException("Revisar composição atende o carimbo A1 horizontal do V02. Use Organizar folhas para outros formatos.");
  return new(box.Min.X+Implantation.Feet(.035),box.Min.Y+Implantation.Feet(.035),box.Max.X-Implantation.Feet(.230),box.Max.Y-Implantation.Feet(.035));
 }
 internal static LegalPaperRect Bounds(Viewport viewport,ViewSheet sheet)
 {var r=LegalSheets.Rect(viewport,sheet)!.Value;double p=Implantation.Feet(.002);return new(r.Left-p,r.Bottom-p,r.Right+p,r.Top+p);}
 public static SheetCompositionResult Apply(Document doc)
 {
  if(doc.IsFamilyDocument||doc.IsReadOnly||doc.IsWorkshared||doc.PathName.EndsWith(".rte",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use um RVT local editável.");
  var sheets=new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(LegalSheets.IsLegalSheet).OrderBy(s=>s.SheetNumber).ToList();
  if(!new[]{"LEG-01","LEG-02","LEG-03"}.All(n=>sheets.Any(s=>s.SheetNumber==n)))throw new InvalidOperationException("Prepare os entregáveis e as folhas LEG-01 a LEG-03 antes de revisar a composição.");
  var views=Titles.Keys.ToDictionary(k=>k,k=>LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key==k))??throw new InvalidOperationException("Entregável ausente: "+Titles[k]));
  var placed=new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().ToList();
  if(views.Any(p=>!placed.Any(v=>v.ViewId==p.Value.Id)))throw new InvalidOperationException("Coloque todos os entregáveis nas folhas antes de revisar a composição.");
  if(placed.Any(v=>views.Values.Any(x=>x.Id==v.ViewId)&&v.Pinned))throw new InvalidOperationException("Desfixe as vistas que deseja recompor.");
  var scales=views.Values.ToDictionary(v=>v.Id.Value,v=>v.Scale);int hidden=0,removed=0,duplicateSchedules=0;
  using var group=new TransactionGroup(doc,"VEBRAM — revisar composição V02");group.Start();
  try
  {
   var saved=LegalContext.Saved(doc,"urbanistico")??throw new InvalidOperationException("Prepare o quadro urbanístico antes de revisar a composição.");
   LegalContext.Apply(doc,saved with{Confirmed=true,ReplaceEdited=false});
   using(var tx=new Transaction(doc,"VEBRAM — limpar guias e centralizar entregáveis"))
   {
    tx.Start();
    foreach(var sheet in sheets)
    {
     var guideIds=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==sheet.Id&&!e.IsHidden(sheet)&&e.CanBeHidden(sheet)&&
      (e is CurveElement curve&&curve.LineStyle?.Name=="ARQ Guias de composição"||e is TextNote note&&
       (doc.GetElement(note.GetTypeId()).Name=="ARQ Guia de composição 3.0"||note.Text.StartsWith("BASE DE PROJETO LEGAL")||note.Text.StartsWith("PAR-01 cerâmico 9 cm;")||note.Text.StartsWith("00 | Folha-base - preencher")||note.Text.StartsWith("Quadros complementares: inserir")))).Select(e=>e.Id).ToList();
     if(guideIds.Count>0){sheet.HideElements(guideIds);hidden+=guideIds.Count;}
    }
    // Legacy placement allowed the same Q04 to occupy every new complementary
    // sheet. Only remove owned copies still at the generator's default position,
    // and only when an original, unowned instance survives on LEG-01.
    var q04=LegalCatalog.Reference(LegalCatalog.Views(doc),LegalCatalog.Items.First(i=>i.Key=="areas"));
    if(q04!=null)
    {
     var instances=new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Where(s=>s.ScheduleId==q04.Id).ToList();
     bool original=instances.Any(s=>doc.GetElement(s.OwnerViewId) is ViewSheet sh&&sh.SheetNumber=="LEG-01"&&!DrawingOwnership.IsAnnotation(s));
     if(original)foreach(var instance in instances.Where(s=>!s.Pinned&&DrawingOwnership.IsAnnotation(s)))
     {
      if(doc.GetElement(instance.OwnerViewId) is not ViewSheet sh||!LegalSheets.IsLegalSheet(sh))continue;
      var title=new FilteredElementCollector(doc,sh.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType().FirstOrDefault()?.get_BoundingBox(sh);var rect=LegalSheets.Rect(instance,sh);if(title==null||rect==null)continue;
      double margin=Implantation.Feet(.015),tolerance=Implantation.Feet(.002);
      if(Math.Abs(rect.Value.Left-title.Min.X-margin)>tolerance||Math.Abs(rect.Value.Top-title.Max.Y+margin)>tolerance)continue;
      var id=instance.Id;if(doc.Delete(id).Any(deleted=>deleted!=id))throw new InvalidOperationException("Dependências da tabela impedem remover duplicata com segurança.");duplicateSchedules++;
     }
    }
    // Remove the empty template placement, preserving its drafting view and all its notes.
    foreach(var viewport in placed.Where(v=>doc.GetElement(v.ViewId).Name=="LEGAL - Quadro urbanístico / preencher"))
    {
     var placeholder=doc.GetElement(viewport.ViewId);
     if(new FilteredElementCollector(doc,placeholder.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Any(n=>n.Text.Trim()=="QUADRO URBANÍSTICO - A DEFINIR"))DeleteViewport(doc,viewport);
    }
    foreach(var pair in views)
    {
     if(pair.Value.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION) is {IsReadOnly:false} title)title.Set(Titles[pair.Key]);
    }
    var byKey=new Dictionary<string,Viewport>();
    for(int i=0;i<Layout.Length;i++)
    {
     var sheet=sheets.First(s=>s.SheetNumber=="LEG-"+(i+1).ToString("00"));
     foreach(string key in Layout[i].SelectMany(r=>r))
     {
      var view=views[key];var viewport=new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().Single(v=>v.ViewId==view.Id);
      if(viewport.SheetId!=sheet.Id)
      {var type=viewport.GetTypeId();DeleteViewport(doc,viewport);doc.Regenerate();viewport=Viewport.Create(doc,sheet.Id,view.Id,XYZ.Zero);viewport.ChangeTypeId(type);DrawingOwnership.MarkAnnotation(viewport);}
      viewport.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.Set("VEBRAM-"+viewport.Id.Value);byKey.Add(key,viewport);
     }
    }
    for(int i=0;i<Layout.Length;i++){int detail=1;foreach(string key in Layout[i].SelectMany(r=>r))byKey[key].get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.Set((detail++).ToString());}
    doc.Regenerate();
    foreach(var vp in byKey.Values)CenterLabel(doc,vp);
    for(int i=0;i<Layout.Length;i++)
    {
     var sheet=sheets.First(s=>s.SheetNumber=="LEG-"+(i+1).ToString("00"));var area=Area(doc,sheet);var rows=Layout[i];double gap=Implantation.Feet(.012);
     var sizes=rows.Select(row=>row.Select(k=>Bounds(byKey[k],sheet)).ToList()).ToList();
     double totalHeight=sizes.Sum(row=>row.Max(r=>r.Top-r.Bottom))+gap*(rows.Length-1);
     if(totalHeight>area.Top-area.Bottom)throw new InvalidOperationException("A composição não cabe na altura da "+sheet.SheetNumber+" sem alterar escala.");
     double top=(area.Top+area.Bottom+totalHeight)/2;int number=1;
     for(int rowIndex=0;rowIndex<rows.Length;rowIndex++)
     {
      var row=rows[rowIndex];var dimensions=sizes[rowIndex];double contentWidth=dimensions.Sum(r=>r.Right-r.Left);double rowGap=row.Length>1?Math.Min(gap,(area.Right-area.Left-Implantation.Feet(.002)-contentWidth)/(row.Length-1)):gap;double width=contentWidth+rowGap*(row.Length-1);double rowHeight=dimensions.Max(r=>r.Top-r.Bottom);
      if(row.Length>1&&rowGap<Implantation.Feet(.006)-1e-8)throw new InvalidOperationException("Espaço entre vistas insuficiente na "+sheet.SheetNumber+" sem alterar escala.");
      if(width>area.Right-area.Left)throw new InvalidOperationException($"Composição larga na {sheet.SheetNumber}: {Implantation.Meters(width)*1000:0} mm para {Implantation.Meters(area.Right-area.Left)*1000:0} mm. Nenhuma escala foi alterada.");
      double left=(area.Left+area.Right-width)/2;
      for(int column=0;column<row.Length;column++)
      {var vp=byKey[row[column]];var rect=Bounds(vp,sheet);double itemTop=top-(rowHeight-(rect.Top-rect.Bottom))/2;ElementTransformUtils.MoveElement(doc,vp.Id,new(left-rect.Left,itemTop-rect.Top,0));vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.Set((number++).ToString());left+=rect.Right-rect.Left+rowGap;}
      top-=rowHeight+gap;
     }
     sheet.Name=i switch{0=>"Planta legal, situação e quadros",1=>"Cobertura e fachadas",_=>"Implantação e cortes"};
    }
    doc.Regenerate();
    foreach(var sheet in sheets.Where(s=>s.SheetNumber is "LEG-01" or "LEG-02" or "LEG-03"))Validate(doc,sheet);
    foreach(var sheet in sheets.Where(s=>s.SheetNumber is not ("LEG-01" or "LEG-02" or "LEG-03")&&s.Name=="Projeto legal — complementos"))
    {
     var contents=new FilteredElementCollector(doc,sheet.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==sheet.Id&&!e.IsHidden(sheet)&&e.Category?.Id.Value!=(long)BuiltInCategory.OST_TitleBlocks).ToList();
     if(contents.Count==0){doc.Delete(sheet.Id);removed++;}
    }
    if(scales.Any(p=>((View)doc.GetElement(new ElementId(p.Key))).Scale!=p.Value))throw new InvalidOperationException("Escala alterada; composição revertida.");
    if(tx.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Composição revertida pelo Revit.");
   }
   if(group.Assimilate()!=TransactionStatus.Committed)throw new InvalidOperationException("Composição revertida.");
   return new(sheets[0].Id.Value,hidden,removed,"Entregáveis compostos na LEG-01 a LEG-03. Planta em destaque; quadro compacto e Situação abaixo. Cobertura e quatro fachadas na LEG-02; Implantação e Cortes A/B na LEG-03. Duplicatas padrão do Q04 geradas: "+duplicateSchedules+". Conteúdo manual nas demais folhas é preservado. Confira dados técnicos e carimbo antes de emitir.",duplicateSchedules);
  }
  catch{if(group.GetStatus()==TransactionStatus.Started)group.RollBack();throw;}
 }
 static void DeleteViewport(Document doc,Viewport viewport)
 {var view=viewport.ViewId;var id=viewport.Id;if(doc.Delete(id).Any(deleted=>deleted!=id)||doc.GetElement(view)==null)throw new InvalidOperationException("Dependências da vista impedem reposicionamento seguro.");}
 internal static void CenterLabel(Document doc,Viewport vp)
 {
  var box=vp.GetBoxOutline();double width=box.MaximumPoint.X-box.MinimumPoint.X;
  vp.LabelLineLength=Math.Min(Implantation.Feet(.14),Math.Max(Implantation.Feet(.06),width-Implantation.Feet(.03)));vp.LabelOffset=new(Implantation.Feet(.015),-Implantation.Feet(.010),0);doc.Regenerate();
  var label=vp.GetLabelOutline();double dx=(box.MinimumPoint.X+box.MaximumPoint.X-label.MinimumPoint.X-label.MaximumPoint.X)/2;vp.LabelOffset+=new XYZ(dx,0,0);doc.Regenerate();
 }
 internal static void Validate(Document doc,ViewSheet sheet)
 {
  var area=Area(doc,sheet);var vps=new FilteredElementCollector(doc,sheet.Id).OfClass(typeof(Viewport)).Cast<Viewport>().Where(v=>!v.IsHidden(sheet)).ToList();
  var rects=vps.Select(v=>Bounds(v,sheet)).ToList();
  if(rects.Any(r=>!area.Contains(r,.001)))throw new InvalidOperationException("Vista ou título fora da margem na "+sheet.SheetNumber);
  for(int a=0;a<rects.Count;a++)for(int b=a+1;b<rects.Count;b++)if(rects[a].Intersects(rects[b],0))throw new InvalidOperationException("Sobreposição na "+sheet.SheetNumber);
 }
}
