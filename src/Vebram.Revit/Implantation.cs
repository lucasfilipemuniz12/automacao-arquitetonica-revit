using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Vebram.Revit;

public sealed record ImplantationOptions(long SourceId, long FootprintId, double LotMinX, double LotMinY,
    double LotWidth, double LotDepth, int Scale, string Front, bool AddStreet, double SidewalkWidth,
    double StreetWidth, string StreetName, double NorthDegrees, bool Study, bool ReplaceEdited = false,
    double? DesignRoofSlope = null, string RoofFlow = "Modelo", List<RoofDrainageChoice>? Drainage = null,
    List<long>? OccupationExtraIds = null);

public sealed record ImplantationPlan(ViewPlan Source, Floor Footprint, ViewPlan? Target,
    ImplantationOptions Options, IReadOnlyList<XYZ> Contour, IReadOnlyList<Element> Roofs)
{
    public string Summary => $"Planta de origem: {Source.Name}\nNível: {Source.GenLevel.Name}\nContorno: {Footprint.Name}\n" +
        $"Lote: {Options.LotWidth:F2} × {Options.LotDepth:F2} m · escala 1:{Options.Scale}\n" +
        (Target == null ? "Criar LEGAL - Implantação." : "Completar/atualizar LEGAL - Implantação, mantendo sua posição no template.") +
        "\nCoberturas e terreno vêm do modelo. Cotas nativas em metros; contorno do térreo, afastamentos, norte, acesso e identificação.\n" +
        (Options.AddStreet ? "Passeio e via são representados com os dados informados.\n" : "") +
        "Anotações próprias serão refeitas. Anotações manuais externas ao gerador serão preservadas.";
}

public sealed record ImplantationResult(long ViewId, int Dimensions, int Annotations, string Report);

public static class Implantation
{
    internal static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    private const string ViewName = "LEGAL - Implantação";
    public static double Feet(double meters) => meters / .3048;
    public static double Meters(double feet) => feet * .3048;
    public static List<ViewPlan> ModelPlans(Document doc, bool all = false) => LegalCatalog.Views(doc).OfType<ViewPlan>()
        .Where(v => v.ViewType == ViewType.FloorPlan && v.GenLevel != null && v.GetPrimaryViewId() == ElementId.InvalidElementId &&
            (all || (!LegalCatalog.IsLegal(v) && !v.Name.StartsWith("EX - ", StringComparison.OrdinalIgnoreCase) &&
                !v.Name.StartsWith("FONTE - ", StringComparison.OrdinalIgnoreCase))))
        .OrderBy(v => v.GenLevel.Elevation).ThenBy(v => v.Name).ToList();
    public static List<Floor> Footprints(Document doc, ViewPlan source) => new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
        .Where(f => f.get_BoundingBox(null) is { } b && Math.Abs(b.Max.Z - source.GenLevel.Elevation) < Feet(.65))
        .OrderByDescending(f => f.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0).ToList();

    public static ImplantationOptions? Saved(Document doc) => LegalCatalog.Views(doc).FirstOrDefault(v => v.Name == ViewName) is { } view
        ? DrawingOwnership.Read(view)?.Options : null;
    public static (double X, double Y, double Width, double Depth)? SuggestedLot(Document doc)
    {
        // Apenas sugestão de envelope do terreno: o formulário exige confirmação do lote.
        var terrain = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Toposolid).WhereElementIsNotElementType()
            .Select(e => e.get_BoundingBox(null)).Where(b => b != null).OrderByDescending(b => (b.Max.X-b.Min.X)*(b.Max.Y-b.Min.Y)).FirstOrDefault();
        return terrain == null ? null : (Meters(terrain.Min.X), Meters(terrain.Min.Y), Meters(terrain.Max.X-terrain.Min.X), Meters(terrain.Max.Y-terrain.Min.Y));
    }
    public static ImplantationPlan Plan(Document doc, ImplantationOptions options)
    {
        if (doc.IsFamilyDocument || doc.IsReadOnly || doc.IsWorkshared || doc.PathName.EndsWith(".rte", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use um projeto RVT local editável.");
        var source = doc.GetElement(new ElementId(options.SourceId)) as ViewPlan;
        if (source == null || source.IsTemplate || source.ViewType != ViewType.FloorPlan || source.GenLevel == null ||
            source.GetPrimaryViewId() != ElementId.InvalidElementId)
            throw new InvalidOperationException("Escolha uma planta de modelagem válida.");
        if (doc.GetElement(new ElementId(options.FootprintId)) is not Floor floor || !Footprints(doc, source).Any(f => f.Id == floor.Id))
            throw new InvalidOperationException("Escolha o piso que define o contorno do térreo no nível selecionado.");
        if ((options.OccupationExtraIds ?? new()).Any(id => !Footprints(doc, source).Any(f => f.Id.Value == id)))
            throw new InvalidOperationException("Uma das lajes adicionais da taxa de ocupação não pertence ao nível escolhido. Refaça a seleção.");
        var numbers = new[] { options.LotMinX, options.LotMinY, options.LotWidth, options.LotDepth, options.NorthDegrees,
            options.SidewalkWidth, options.StreetWidth };
        if (numbers.Any(n => !double.IsFinite(n)) || options.LotWidth <= 0 || options.LotDepth <= 0 ||
            options.Scale is not (50 or 100 or 200) || !new[] { "Direita", "Esquerda", "Acima", "Abaixo" }.Contains(options.Front) ||
            (options.AddStreet && (options.SidewalkWidth <= 0 || options.StreetWidth <= 0 || string.IsNullOrWhiteSpace(options.StreetName))))
            throw new InvalidOperationException("Confira dimensões, escala e dados do lote/via.");
        if(options.DesignRoofSlope is { } design && (!double.IsFinite(design)||design<0||design>100))
            throw new InvalidOperationException("Confira o caimento de projeto informado (0 a 100%).");
        if(!new[]{"Modelo","Direita","Esquerda","Acima","Abaixo"}.Contains(options.RoofFlow))
            throw new InvalidOperationException("Escolha um sentido válido para a seta de caimento.");
        // OccupationFootprint keeps its explicitly selected slabs; setbacks use the whole building.
        var frame=BuildingFrame.Read(doc,source,floor);
        var building=frame.Members.Where(e=>e is Wall or Floor or Autodesk.Revit.DB.Architecture.Railing)
            .Select(e=>e.get_BoundingBox(null)).Where(b=>b!=null).ToList();
        double bx0=building.Min(b=>b!.Min.X),bx1=building.Max(b=>b!.Max.X),by0=building.Min(b=>b!.Min.Y),by1=building.Max(b=>b!.Max.Y),bz=source.GenLevel.Elevation;
        IReadOnlyList<XYZ> contour=[new(bx0,by0,bz),new(bx1,by0,bz),new(bx1,by1,bz),new(bx0,by1,bz)];
        var left = Feet(options.LotMinX); var right = left + Feet(options.LotWidth);
        var bottom = Feet(options.LotMinY); var top = bottom + Feet(options.LotDepth);
        if (contour.Any(p => p.X < left - .001 || p.X > right + .001 || p.Y < bottom - .001 || p.Y > top + .001))
            throw new InvalidOperationException("O contorno selecionado ultrapassa o lote informado. Confira o piso e a posição do lote.");
        var targets = LegalCatalog.Views(doc).OfType<ViewPlan>().Where(v => v.Name == ViewName).ToList();
        if (targets.Count > 1) throw new InvalidOperationException("Há mais de uma Implantação com o nome do V02.");
        var target = targets.SingleOrDefault();
        if (target != null && target.Id == source.Id)
            throw new InvalidOperationException("Escolha uma planta de modelagem diferente da Implantação.");
        if (target != null)
        {
            var state = DrawingOwnership.Read(target);
            if (state != null && !options.ReplaceEdited && DrawingOwnership.WasEdited(doc, state))
                throw new InvalidOperationException("Uma marcação gerada foi editada ou removida manualmente. Para refazê-la, marque a opção de substituir marcações editadas.");
            if (state == null && new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .Any(e => e.OwnerViewId == target.Id && (e is TextNote || e is Dimension || e is DetailCurve || e is FilledRegion)))
                throw new InvalidOperationException("A Implantação existente contém detalhamento manual não registrado. Preserve essa vista ou renomeie-a antes de criar a nova.");
        }
        var roofs = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType()
            .Where(e => e.get_BoundingBox(null) is { } b && b.Max.Z > source.GenLevel.Elevation &&
                b.Max.X >= left && b.Min.X <= right && b.Max.Y >= bottom && b.Min.Y <= top).ToList();
        if (roofs.Count == 0) throw new InvalidOperationException("Não há cobertura modelada sobre o lote. Modele/associe a cobertura antes de preparar esta Implantação.");
        return new(source, floor, target, options, contour, roofs);
    }

    public static ImplantationResult Apply(Document doc, ImplantationPlan plan)
    {
        using var transaction = new Transaction(doc, "VEBRAM — completar Implantação");
        transaction.Start();
        var problems = new Failures();
        transaction.SetFailureHandlingOptions(transaction.GetFailureHandlingOptions().SetFailuresPreprocessor(problems).SetClearAfterRollback(true));
        try
        {
            var view = plan.Target;
            if(view!=null&&view.GenLevel.Id!=plan.Source.GenLevel.Id)
            {
                // A plan's level is immutable. Preserve the old view and all placements as a reserve.
                var old=view;old.Name="RESERVA - "+ViewName+" - "+old.Id.Value;
                view=ViewPlan.Create(doc,old.GetTypeId(),plan.Source.GenLevel.Id);view.Name=ViewName;
                view.ViewTemplateId=old.ViewTemplateId;
            }
            if(view==null){view=ViewPlan.Create(doc,plan.Source.GetTypeId(),plan.Source.GenLevel.Id);view.Name=ViewName;}
            var previous = DrawingOwnership.Read(view);
            if (previous != null)
            {
                var owned = previous.Elements.Select(stamp => doc.GetElement(stamp.UniqueId))
                    .Where(e => e != null && e.OwnerViewId == view.Id && DrawingOwnership.IsAnnotation(e)).Select(e => e!.Id).ToList();
                if (owned.Count > 0){var allowed=owned.ToHashSet();if(doc.Delete(owned).Any(id=>!allowed.Contains(id)))throw new InvalidOperationException("Há elementos dependentes das marcações da Implantação. Atualização revertida para preservá-los.");}
            }
            SetClassification(view);
            PreparePreset(doc, view, plan);
            var generated = new List<Element>();
            var z = plan.Source.GenLevel.Elevation;
            var o = plan.Options;
            double x0=Feet(o.LotMinX), y0=Feet(o.LotMinY), x1=x0+Feet(o.LotWidth), y1=y0+Feet(o.LotDepth);
            var dimensionType = LegalPreferenceStore.Dimension(doc);
            var textType = TextStyle(doc, false); var boldType = TextStyle(doc, true);
            var helpers = new Drawing(doc, view, generated, dimensionType, textType, boldType, z);
            var lot = helpers.Polygon([new(x0,y0,z),new(x1,y0,z),new(x1,y1,z),new(x0,y1,z)], 4);
            var contour = helpers.Polygon(plan.Contour, 2, Dashed(doc));
            double fx0=plan.Contour.Min(p=>p.X), fx1=plan.Contour.Max(p=>p.X), fy0=plan.Contour.Min(p=>p.Y), fy1=plan.Contour.Max(p=>p.Y);
            doc.Regenerate();
            DetailCurve Vertical(double x) => contour.Where(c => Math.Abs(c.GeometryCurve.GetEndPoint(0).X-x)<.001 &&
                Math.Abs(c.GeometryCurve.GetEndPoint(1).X-x)<.001).OrderByDescending(c=>c.GeometryCurve.Length).First();
            DetailCurve Horizontal(double y) => contour.Where(c => Math.Abs(c.GeometryCurve.GetEndPoint(0).Y-y)<.001 &&
                Math.Abs(c.GeometryCurve.GetEndPoint(1).Y-y)<.001).OrderByDescending(c=>c.GeometryCurve.Length).First();
            helpers.Dimension([lot[3],Vertical(fx0),Vertical(fx1),lot[1]], new(x0,y0-Feet(.95),z), new(x1,y0-Feet(.95),z));
            helpers.Dimension([lot[3],lot[1]], new(x0,y0-Feet(1.95),z), new(x1,y0-Feet(1.95),z));
            helpers.Dimension([lot[0],Horizontal(fy0),Horizontal(fy1),lot[2]], new(x0-Feet(.95),y0,z), new(x0-Feet(.95),y1,z));
            helpers.Dimension([lot[0],lot[2]], new(x0-Feet(1.95),y0,z), new(x0-Feet(1.95),y1,z));

            double contextX0=x0, contextX1=x1, contextY0=y0, contextY1=y1;
            if (o.AddStreet)
            {
                var w=Feet(o.SidewalkWidth); var road=Feet(o.StreetWidth);
                var widths=new[]{w,road,w}; var offset=0.0;
                var bands=new List<List<DetailCurve>>();
                for(int i=0;i<3;i++)
                {
                    double a=x0,b=y0,c=x1,d=y1;
                    if(o.Front=="Direita"){a=x1+offset;c=a+widths[i];}
                    if(o.Front=="Esquerda"){c=x0-offset;a=c-widths[i];}
                    if(o.Front=="Acima"){b=y1+offset;d=b+widths[i];}
                    if(o.Front=="Abaixo"){d=y0-offset;b=d-widths[i];}
                    bands.Add(helpers.Polygon([new(a,b,z),new(c,b,z),new(c,d,z),new(a,d,z)],1));
                    contextX0=Math.Min(contextX0,a);contextX1=Math.Max(contextX1,c);contextY0=Math.Min(contextY0,b);contextY1=Math.Max(contextY1,d);
                    if(i==1) helpers.Text(new((a+c)/2,(b+d)/2,z),o.StreetName,true,true,o.Front is "Direita" or "Esquerda"?Math.PI/2:0);
                    offset+=widths[i];
                }
                doc.Regenerate();
                if(o.Front=="Direita") helpers.Dimension([lot[1],bands[0][1],bands[1][1],bands[2][1]],new(x1,y0-Feet(.95),z),new(contextX1,y0-Feet(.95),z));
                else if(o.Front=="Esquerda") helpers.Dimension([bands[2][3],bands[1][3],bands[0][3],lot[3]],new(contextX0,y0-Feet(.95),z),new(x0,y0-Feet(.95),z));
                else if(o.Front=="Acima") helpers.Dimension([lot[2],bands[0][2],bands[1][2],bands[2][2]],new(x0-Feet(.95),y1,z),new(x0-Feet(.95),contextY1,z));
                else helpers.Dimension([bands[2][0],bands[1][0],bands[0][0],lot[0]],new(x0-Feet(.95),contextY0,z),new(x0-Feet(.95),y0,z));
                var frontCenter=o.Front switch {"Direita"=>new XYZ(x1,(y0+y1)/2,z),"Esquerda"=>new XYZ(x0,(y0+y1)/2,z),
                    "Acima"=>new XYZ((x0+x1)/2,y1,z),_=>new XYZ((x0+x1)/2,y0,z)};
                helpers.Text(frontCenter+new XYZ(Feet(.4),Feet(.5),0),"FRENTE / ACESSO",false,false);
            }
            var northStart=new XYZ(x0+Feet(1.8),contextY1+Feet(1.6),z);
            double angle=o.NorthDegrees*Math.PI/180;
            var northDir=new XYZ(-Math.Sin(angle),Math.Cos(angle),0);
            helpers.Arrow(northStart,northStart+northDir*Feet(1.3));
            helpers.Text(northStart+northDir*Feet(1.3)+new XYZ(0,Feet(.35),0),"N",true,true);
            helpers.Text(northStart+new XYZ(Feet(1.1),Feet(.1),0),o.Study?"Norte convencional":"Norte informado",false,false);

            RoofDrainage.Annotate(doc,view,RoofWaters.Read(doc,plan.Roofs),o,generated);
            foreach(var item in new[]{("piscina","PISCINA"),("deck","DECK"),("grama","ÁREA AJARDINADA"),("garagem","GARAGEM")})
            {
                var element=new FilteredElementCollector(doc).WhereElementIsNotElementType()
                    .Where(e=>!e.ViewSpecific && (e is Floor || e is DirectShape) && (e.Name.Contains(item.Item1,StringComparison.OrdinalIgnoreCase) ||
                        (doc.GetElement(e.GetTypeId())?.Name.Contains(item.Item1,StringComparison.OrdinalIgnoreCase)??false)))
                    .Where(e=>e.get_BoundingBox(null) is { } b && b.Max.X>=x0 && b.Min.X<=x1 && b.Max.Y>=y0 && b.Min.Y<=y1)
                    .OrderByDescending(e=>{var b=e.get_BoundingBox(null)!;return(b.Max.X-b.Min.X)*(b.Max.Y-b.Min.Y);}).FirstOrDefault();
                if(element==null)continue;
                var b=element.get_BoundingBox(null)!;
                var label=new XYZ((b.Min.X+b.Max.X)/2,(b.Min.Y+b.Max.Y)/2+Feet(.15),z);
                helpers.Text(label,item.Item2,false,true,item.Item1=="piscina" && b.Max.Y-b.Min.Y>b.Max.X-b.Min.X?Math.PI/2:0);
            }
            var notes="IMPLANTAÇÃO\nCotas em "+LegalPreferenceStore.UnitLabel(doc,dimensionType)+"\n"+
                "Linha contínua: limite do lote informado\nLinha tracejada: envelope da edificação completa\n"+
                "Cotas parciais: afastamentos desse contorno\nFrente: "+o.Front.ToLower(Pt)+" do desenho\n"+
                (o.Study?"ESTUDO FICTÍCIO — NÃO PROTOCOLAR":"Dados do lote/norte informados pelo autor");
            var registrationNotes=ProjectDataIntegration.CompactForDrawing(ProjectDataIntegration.Notes(ProjectRegistrationStore.Read(doc)));if(registrationNotes.Length>0)notes+="\n"+registrationNotes;
            var extra=LegalPreferenceStore.Read(doc).AdditionalNote;if(!string.IsNullOrWhiteSpace(extra))notes+="\n"+extra;
            doc.Regenerate();
            var annotationBottom=generated.Select(e=>e.get_BoundingBox(view)).Where(b=>b!=null).Select(b=>b!.Min.Y).DefaultIfEmpty(contextY0).Min();
            double notesY=Math.Min(contextY0-Feet(3.0),annotationBottom-Feet(.004)*view.Scale);
            helpers.Text(new(x0,notesY,z),notes,false,false,0,Feet(25));
            // Recorte inclui desenho, cadeias, seta de norte e a coluna de chamadas.
            var box=view.CropBox; var inverse=box.Transform.Inverse;
            var corners=new[]{inverse.OfPoint(new XYZ(contextX0-Feet(3.6),contextY0-Feet(6.2),z)),
                inverse.OfPoint(new XYZ(contextX1+Feet(2.2),contextY1+Feet(4.2),z))};
            box.Min=new(corners.Min(p=>p.X),corners.Min(p=>p.Y),box.Min.Z);
            box.Max=new(corners.Max(p=>p.X),corners.Max(p=>p.Y),box.Max.Z);
            view.CropBox=box;view.CropBoxActive=true;view.CropBoxVisible=false;
            var annotationCrop=view.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);
            if(annotationCrop is {IsReadOnly:false})annotationCrop.Set(0);
            doc.Regenerate();
            DrawingOwnership.Write(view,new(o,plan.Source.UniqueId,generated.Select(e=>new ElementStamp(e.UniqueId,DrawingOwnership.Fingerprint(e))).ToList()));
            foreach(var e in generated)DrawingOwnership.MarkAnnotation(e);
            var dimensions=generated.OfType<Dimension>().Count();
            if(transaction.Commit()!=TransactionStatus.Committed)throw new InvalidOperationException("Implantação revertida: "+string.Join("\n",problems.Messages));
            return new(view.Id.Value,dimensions,generated.Count,$"{view.Name} preparada em 1:{o.Scale}.\n{dimensions} cotas nativas; {generated.Count} elementos de anotação.\n"+
                "A vista está disponível para colocar na folha. A planta de modelagem foi preservada.\n"+
                "Para refletir alterações do modelo, execute Implantação novamente. Confira o lote informado, norte e chamadas antes de emitir.");
        }
        catch { if(transaction.GetStatus()==TransactionStatus.Started)transaction.RollBack();throw; }
    }

    public static IReadOnlyList<XYZ> GetContour(Document doc,Floor floor)=>ExternalContour(doc,floor,FloorContour(doc,floor));
    private static IReadOnlyList<XYZ> FloorContour(Document doc, Floor floor)
    {
        if(doc.GetElement(floor.SketchId) is not Sketch sketch)throw new InvalidOperationException("O piso selecionado não fornece um contorno nativo.");
        var loops=sketch.Profile.Cast<CurveArray>().Select(a=>a.Cast<Curve>().ToList()).ToList();
        if(loops.Count==0 || loops.Any(a=>a.Count<3 || a.Any(c=>c.Length<doc.Application.ShortCurveTolerance)))
            throw new InvalidOperationException("O piso precisa de um contorno fechado, com pelo menos três trechos válidos.");
        var loop=loops.OrderByDescending(a=>Math.Abs(a.Select((c,i)=>{var p=c.GetEndPoint(0);var q=a[(i+1)%a.Count].GetEndPoint(0);return p.X*q.Y-q.X*p.Y;}).Sum())).First();
        if(loop.Any(c=>c is not Line || (Math.Abs(c.GetEndPoint(1).X-c.GetEndPoint(0).X)>.001 && Math.Abs(c.GetEndPoint(1).Y-c.GetEndPoint(0).Y)>.001)))
            throw new InvalidOperationException("Este piloto aceita lote retangular e contorno ortogonal. Curvas/diagonais precisam do próximo perfil geométrico.");
        return loop.Select(c=>c.GetEndPoint(0)).ToList();
    }
    private static IEnumerable<PlanarFace> Faces(Element element)
    {
        var geometry=element.get_Geometry(new Options{DetailLevel=ViewDetailLevel.Fine});
        if(geometry==null)yield break;
        foreach(var f in FacesOf(geometry))yield return f;
    }
    private static IReadOnlyList<XYZ> ExternalContour(Document doc,Floor floor,IReadOnlyList<XYZ> raw)
    {
        // Piso como referência; faces externas de paredes adjacentes para os afastamentos.
        var points=raw.Where((p,i)=>Math.Abs((p-raw[(i+raw.Count-1)%raw.Count]).Normalize()
            .CrossProduct((raw[(i+1)%raw.Count]-p).Normalize()).Z)>.001).ToList();
        if(points.Count<4)
            throw new InvalidOperationException("Não foi possível reconhecer um contorno ortogonal fechado nesse piso.");
        double area=points.Select((p,i)=>p.X*points[(i+1)%points.Count].Y-p.Y*points[(i+1)%points.Count].X).Sum();
        var walls=new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>()
            .Where(w=>w.get_BoundingBox(null) is { } b && b.Min.Z<floor.get_BoundingBox(null)!.Max.Z+Feet(.3) &&
                b.Max.Z>floor.get_BoundingBox(null)!.Max.Z+Feet(1) && w.Location is LocationCurve {Curve:Autodesk.Revit.DB.Line}).ToList();
        var planes=new List<(XYZ Normal,double D)>();
        for(int i=0;i<points.Count;i++)
        {
            var a=points[i];var b=points[(i+1)%points.Count];var dir=(b-a).Normalize();
            var normal=(area>0?new XYZ(dir.Y,-dir.X,0):new XYZ(-dir.Y,dir.X,0)).Normalize();
            var coordinates=new List<double>();
            foreach(var wall in walls)
            {
                var axis=(Autodesk.Revit.DB.Line)((LocationCurve)wall.Location).Curve;
                if(Math.Abs(axis.Direction.DotProduct(normal))>.01||Math.Abs((axis.GetEndPoint(0)-a).DotProduct(normal))>Feet(.3))continue;
                var t0=(axis.GetEndPoint(0)-a).DotProduct(dir);var t1=(axis.GetEndPoint(1)-a).DotProduct(dir);
                if(Math.Min(a.DistanceTo(b),Math.Max(t0,t1))-Math.Max(0,Math.Min(t0,t1))<Feet(.2))continue;
                foreach(var shell in new[]{ShellLayerType.Exterior,ShellLayerType.Interior})
                    foreach(var reference in HostObjectUtils.GetSideFaces(wall,shell))
                        if(wall.GetGeometryObjectFromReference(reference) is PlanarFace face && face.FaceNormal.DotProduct(normal)>.99)
                            coordinates.Add(face.Origin.DotProduct(normal));
            }
            planes.Add((normal,coordinates.Count>0?coordinates.Max():a.DotProduct(normal)));
        }
        var result=new List<XYZ>();
        for(int i=0;i<planes.Count;i++)
        {
            var p=planes[(i+planes.Count-1)%planes.Count];var q=planes[i];
            var determinant=p.Normal.X*q.Normal.Y-q.Normal.X*p.Normal.Y;
            if(Math.Abs(determinant)<1e-8)
                throw new InvalidOperationException("Há trechos colineares ou sobrepostos no contorno. Revise o piso base.");
            result.Add(new XYZ((p.D*q.Normal.Y-q.D*p.Normal.Y)/determinant,
                (p.Normal.X*q.D-q.Normal.X*p.D)/determinant,points[i].Z));
        }
        return result;
    }
    private static IEnumerable<PlanarFace> FacesOf(GeometryElement geometry)
    {
        foreach(var g in geometry)
        {
            if(g is Solid s)foreach(var f in s.Faces.OfType<PlanarFace>())yield return f;
            else if(g is GeometryInstance instance)foreach(var f in FacesOf(instance.GetInstanceGeometry()))yield return f;
        }
    }
    private static void SetClassification(View view)
    {
        foreach(var pair in new[]{(LegalCatalog.StageId,LegalCatalog.Stage),(LegalCatalog.FunctionId,"IMPLANTACAO")})
        {
            var p=view.get_Parameter(pair.Item1);
            if(p!=null&&LegalCatalog.Same(p.AsString(),pair.Item2))continue;
            if(p==null||p.IsReadOnly)throw new InvalidOperationException("Binding de etapa/função do V02 ausente ou bloqueado.");
            p.Set(pair.Item2);
        }
    }
    private static void PreparePreset(Document doc, ViewPlan view, ImplantationPlan plan)
    {
        var name="ARQ VT - IMPLANTACAO - VEBRAM";
        var preset=new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v=>v.IsTemplate&&v.Name==name);
        if(preset==null){preset=view.CreateViewTemplate();preset.Name=name;}
        var excluded=preset.GetNonControlledTemplateParameterIds().ToHashSet();
        var controlled=preset.GetTemplateParameterIds();
        foreach(var guid in new[]{LegalCatalog.StageId,LegalCatalog.FunctionId})
            if(SharedParameterElement.Lookup(doc,guid) is { } element && controlled.Contains(element.Id))excluded.Add(element.Id);
        foreach(var id in controlled)
            if(new[]{"PLAN_VIEW_RANGE","VIEW_SCALE","VIEWER_CROP_REGION","VIEWER_CROP_REGION_VISIBLE","VIEWER_ANNOTATION_CROP_ACTIVE","VIEW_PHASE","PLAN_VIEW_NORTH"}
                .Contains(((BuiltInParameter)id.Value).ToString()))excluded.Add(id);
        preset.SetNonControlledTemplateParameterIds(excluded.ToList());
        preset.DisplayStyle=DisplayStyle.HLR;
        foreach(var key in new[]{"OST_Sections","OST_Elev","OST_Levels","OST_Grids","OST_VolumeOfInterest","OST_CLines"})
            if(Enum.TryParse<BuiltInCategory>(key,out var category)&&preset.CanCategoryBeHidden(new ElementId(category)))preset.SetCategoryHidden(new ElementId(category),true);
        view.ViewTemplateId=preset.Id;view.Scale=plan.Options.Scale;
        var phase=view.get_Parameter(BuiltInParameter.VIEW_PHASE);
        if(phase is {IsReadOnly:false})phase.Set(plan.Source.get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId());
        var north=view.get_Parameter(BuiltInParameter.PLAN_VIEW_NORTH);if(north is {IsReadOnly:false})north.Set(0);
        var boxes=new FilteredElementCollector(doc).WhereElementIsNotElementType().Where(e=>!e.ViewSpecific&&(e is HostObject||e is DirectShape))
            .Select(e=>e.get_BoundingBox(null)).Where(b=>b!=null).ToList();
        double baseZ=plan.Source.GenLevel.Elevation;
        var cut=boxes.Max(b=>b.Max.Z)-baseZ+Feet(.7);var bottom=Math.Min(Feet(-.5),boxes.Min(b=>b.Min.Z)-baseZ-Feet(.2));
        using var range=view.GetViewRange();
        foreach(var plane in new[]{PlanViewPlane.TopClipPlane,PlanViewPlane.CutPlane,PlanViewPlane.BottomClipPlane,PlanViewPlane.ViewDepthPlane})range.SetLevelId(plane,plan.Source.GenLevel.Id);
        range.SetOffset(PlanViewPlane.TopClipPlane,cut+Feet(1));range.SetOffset(PlanViewPlane.CutPlane,cut);
        range.SetOffset(PlanViewPlane.BottomClipPlane,bottom);range.SetOffset(PlanViewPlane.ViewDepthPlane,bottom-Feet(.2));
        view.SetViewRange(range);
    }
    private static DimensionType DimensionStyle(Document doc)=>LegalPreferenceStore.Dimension(doc);
    private static TextNoteType TextStyle(Document doc,bool bold)=>LegalStyles.Text(doc,2.5,bold);
    private static ElementId Dashed(Document doc)
    {
        var name="ARQ - Implantação 4-2 mm - VEBRAM";
        var pattern=new FilteredElementCollector(doc).OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>().FirstOrDefault(p=>p.Name==name);
        if(pattern!=null)return pattern.Id;
        var line=new LinePattern(name);line.SetSegments(new List<LinePatternSegment>{new(LinePatternSegmentType.Dash,Feet(.004)),new(LinePatternSegmentType.Space,Feet(.002))});
        return LinePatternElement.Create(doc,line).Id;
    }
    private sealed class Drawing(Document doc,ViewPlan view,List<Element> created,DimensionType dimensionType,TextNoteType textType,TextNoteType boldType,double z)
    {
        public DetailCurve Line(XYZ a,XYZ b,int weight=1,ElementId? pattern=null)
        {
            var line=doc.Create.NewDetailCurve(view,Autodesk.Revit.DB.Line.CreateBound(new(a.X,a.Y,z),new(b.X,b.Y,z)));
            var graphics=new OverrideGraphicSettings().SetProjectionLineWeight(weight).SetProjectionLineColor(new Color(0,0,0));
            if(pattern!=null)graphics.SetProjectionLinePatternId(pattern);
            view.SetElementOverrides(line.Id,graphics);created.Add(line);return line;
        }
        public List<DetailCurve> Polygon(IReadOnlyList<XYZ> points,int weight,ElementId? pattern=null) => points.Select((p,i)=>Line(p,points[(i+1)%points.Count],weight,pattern)).ToList();
        public void Dimension(IReadOnlyList<DetailCurve> curves,XYZ a,XYZ b)
        {
            var refs=new ReferenceArray();foreach(var c in curves)refs.Append(c.GeometryCurve.Reference??new Reference(c));
            var dimension=doc.Create.NewDimension(view,Autodesk.Revit.DB.Line.CreateBound(a,b),refs,dimensionType);created.Add(dimension);
        }
        public void Text(XYZ point,string text,bool bold=false,bool centered=false,double rotation=0,double? width=null)
        {
            var options=new TextNoteOptions((bold?boldType:textType).Id){HorizontalAlignment=centered?HorizontalTextAlignment.Center:HorizontalTextAlignment.Left,Rotation=rotation};
            // TextNote.Width usa unidades do papel; Coord usa coordenadas do modelo.
            var note=width.HasValue?TextNote.Create(doc,view.Id,point,width.Value/view.Scale,text,options):TextNote.Create(doc,view.Id,point,text,options);created.Add(note);
        }
        public void Arrow(XYZ from,XYZ to)
        {
            Line(from,to,2);var direction=(to-from).Normalize();var perpendicular=new XYZ(-direction.Y,direction.X,0);
            var tip=Feet(.2)*view.Scale/100;
            Line(to,to-direction*tip+perpendicular*tip/2,2);Line(to,to-direction*tip-perpendicular*tip/2,2);
        }
        public void Callout(XYZ target,XYZ label,string text)
        {
            var elbow=new XYZ(label.X-Feet(.6),label.Y-Feet(.2),z);
            Line(target,elbow);Line(elbow,new XYZ(label.X-Feet(.1),elbow.Y,z));Text(label,text);
        }
    }
    private sealed class Failures : IFailuresPreprocessor
    {
        public readonly List<string> Messages=[];
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            foreach(var f in accessor.GetFailureMessages())Messages.Add(f.GetDescriptionText());
            return Messages.Count>0?FailureProcessingResult.ProceedWithRollBack:FailureProcessingResult.Continue;
        }
    }
}

public sealed record ElementStamp(string UniqueId,string Fingerprint);
public sealed record DrawingState(ImplantationOptions Options,string Source,List<ElementStamp> Elements);
public static class DrawingOwnership
{
    private static readonly Guid Id=new("375b67ec-9533-4ee9-a343-c936a98f1bd9");
    private static Schema Schema()
    {
        var schema=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(Id);if(schema!=null)return schema;
        var b=new SchemaBuilder(Id);b.SetSchemaName("VebramImplantacaoV02");b.AddSimpleField("Payload",typeof(string));return b.Finish();
    }
    public static DrawingState? Read(View view)
    {
        var schema=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(Id);if(schema==null)return null;
        var e=view.GetEntity(schema);if(!e.IsValid())return null;
        var value=e.Get<string>(schema.GetField("Payload"));return value=="annotation"?null:JsonSerializer.Deserialize<DrawingState>(value);
    }
    public static void Write(View view,DrawingState state)
    {var schema=Schema();var entity=new Entity(schema);entity.Set(schema.GetField("Payload"),JsonSerializer.Serialize(state));view.SetEntity(entity);}
    public static void MarkAnnotation(Element element)
    {var schema=Schema();var entity=new Entity(schema);entity.Set(schema.GetField("Payload"),"annotation");element.SetEntity(entity);}
    public static bool IsAnnotation(Element element)
    {var schema=Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(Id);return schema!=null&&element.GetEntity(schema).IsValid()&&element.GetEntity(schema).Get<string>(schema.GetField("Payload"))=="annotation";}
    public static bool WasEdited(Document doc,DrawingState state) => state.Elements.Any(stamp=>doc.GetElement(stamp.UniqueId) is not { } e||!IsAnnotation(e)||Fingerprint(e)!=stamp.Fingerprint);
    public static string Fingerprint(Element element)
    {
        var text=element.GetType().Name+"/"+element.GetTypeId().Value;
        if(element is TextNote note){text+="/"+note.Text+"/"+Point(note.Coord)+"/"+note.Width.ToString("R",CultureInfo.InvariantCulture);foreach(var leader in note.GetLeaders())text+="/"+Point(leader.End)+"/"+Point(leader.Elbow);}
        if(element is CurveElement curve)text+="/"+Point(curve.GeometryCurve.GetEndPoint(0))+"/"+Point(curve.GeometryCurve.GetEndPoint(1));
        if(element is Dimension dimension)
        {
            text+="/"+(dimension.Curve is Autodesk.Revit.DB.Line line?Point(line.Origin)+Point(line.Direction):dimension.Curve?.ToString());
            if(dimension.NumberOfSegments>0)
                foreach(DimensionSegment segment in dimension.Segments)text+="/"+segment.ValueOverride+"/"+segment.Prefix+"/"+segment.Suffix+"/"+Point(segment.TextPosition);
            else text+="/"+dimension.ValueOverride+"/"+dimension.Prefix+"/"+dimension.Suffix+"/"+Point(dimension.TextPosition);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
    private static string Point(XYZ p)=>string.Join(",",new[]{p.X,p.Y,p.Z}.Select(n=>Math.Round(n,8).ToString("R",CultureInfo.InvariantCulture)));
}

