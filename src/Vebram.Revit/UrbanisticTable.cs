using Autodesk.Revit.DB;
namespace Vebram.Revit;
internal static class UrbanisticTable
{
 internal static void Draw(Document doc,View view,ContextSelection input,ImplantationOptions options,double projection,List<Element> made)
 {
  var manual=new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType().Where(e=>e.OwnerViewId==view.Id&&e is TextNote or CurveElement).Select(e=>e.get_BoundingBox(view)).Where(b=>b!=null).ToList();
  double y=manual.Count>0?manual.Min(b=>b!.Min.Y)-Implantation.Feet(.02):0,padding=Implantation.Feet(.002),lot=options.LotWidth*options.LotDepth;
  string N(double n)=>n.ToString("0.00",Implantation.Pt);
  string Area(double? n)=>n.HasValue?N(n.Value)+" m²":"Não informado";
  bool firstRow=true;
  void Row(string[] values,double[] widths,bool bold=false)
  {
   double x=0;var notes=new List<TextNote>();var edges=new List<double>{0};
   for(int i=0;i<values.Length;i++){double w=Implantation.Feet(widths[i]);var note=TextNote.Create(doc,view.Id,new(x+padding,y-padding,0),w-2*padding,values[i],new TextNoteOptions(LegalStyles.Text(doc,2.5,bold).Id));notes.Add(note);made.Add(note);x+=w;edges.Add(x);}
   doc.Regenerate();double height=Math.Max(Implantation.Feet(.009),notes.Max(n=>n.Height)+2*padding);
   void L(XYZ a,XYZ b)=>made.Add(doc.Create.NewDetailCurve(view,Line.CreateBound(a,b)));
   if(firstRow){L(new(0,y,0),new(x,y,0));firstRow=false;}L(new(0,y-height,0),new(x,y-height,0));foreach(var edge in edges)L(new(edge,y,0),new(edge,y-height,0));y-=height;
  }
  Row(["QUADRO URBANÍSTICO"],[.18],true);
  Row(["Lote",string.IsNullOrWhiteSpace(input.Identification)?"Não informado":input.Identification],[.05,.13]);
  Row(["Área do lote",Area(lot)],[.09,.09]);Row(["Projeção do contorno base",Area(projection)],[.09,.09]);
  Row(["Área computável informada",Area(input.ComputableArea)],[.09,.09]);Row(["Área permeável informada",Area(input.PermeableArea)],[.09,.09]);
  double[] columns=[.025,.042,.042,.071];Row(["Índice","Permitido","Projeto","Situação"],columns,true);
  void Index(string label,double? allowed,double? project,bool maximum,bool percent)
  {string suffix=percent?"%":"";string status=project==null?"Área não informada":allowed==null?"Limite não informado":(maximum?project<=allowed+1e-9:project>=allowed-1e-9)?"Atende (informado)":"Não atende (informado)";Row([label,allowed.HasValue?(maximum?"Máx. ":"Mín. ")+N(allowed.Value)+suffix:"Não informado",project.HasValue?N(project.Value)+suffix:"Pendente",status],columns);}
  Index("TO",input.MaximumOccupation,LegalMetrics.Ratio(projection,lot,true),true,true);
  Index("CA",input.MaximumCoefficient,input.ComputableArea.HasValue?LegalMetrics.Ratio(input.ComputableArea.Value,lot,false):null,true,false);
  Index("TP",input.MinimumPermeability,input.PermeableArea.HasValue?LegalMetrics.Ratio(input.PermeableArea.Value,lot,true):null,false,true);
  Row(["TO: projeção / lote · CA: computável / lote · TP: permeável / lote. Conferir parcelas e critérios municipais."],[.18]);
  Row([options.Study?"ESTUDO FICTÍCIO — REVISAR":"Comparação com limites informados — revisar"],[.18]);
 }
}
