using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
namespace Vebram.Revit;

public sealed record PlanAnnotationResult(int RoomDimensions,int WallDimensions,int OpeningDimensions,int ExternalLabels,List<string> Pending);

// Coordinates are Revit internal feet; distances and references come from the model.
public static class PlanAnnotations
{
 sealed record Edge(Line Line,Wall Wall);
 sealed record Rect(double X0,double Y0,double X1,double Y1)
 {
  public bool Overlaps(Rect b,double gap)=>X0<b.X1+gap&&X1>b.X0-gap&&Y0<b.Y1+gap&&Y1>b.Y0-gap;
 }
 public static PlanAnnotationResult Apply(Document doc,ViewPlan view,ViewPlan source,List<Element> made,TextNoteType textType,DimensionType dimType,LegalPreferences prefs)
 {
  var pending=new List<string>();int roomDims=0,wallDims=0,openingDims=0,external=0;
  var occupied=new List<Rect>();double z=view.GenLevel.Elevation;double paper=Implantation.Feet(.0025)*view.Scale;
  var rooms=new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfType<Room>().Where(r=>r.LevelId==source.GenLevel.Id&&r.Area>0).OrderByDescending(r=>r.Area).ThenBy(r=>r.Id.Value).ToList();
  var roomBounds=rooms.Select(r=>r.get_BoundingBox(null)).Where(b=>b!=null).ToList();
  double calloutX=roomBounds.Count>0?roomBounds.Max(b=>b!.Max.X)+Implantation.Feet(2):0;
  double calloutY=roomBounds.Count>0?roomBounds.Max(b=>b!.Max.Y):0;
  var walls=new Dictionary<long,Wall>();
  foreach(var room in rooms)
  {
   using var boundary=new SpatialElementBoundaryOptions{SpatialElementBoundaryLocation=SpatialElementBoundaryLocation.Finish};
   var loops=room.GetBoundarySegments(boundary);var edges=new List<Edge>();
   if(loops!=null)foreach(var loop in loops)foreach(var segment in loop)
    if(segment.GetCurve() is Line line&&doc.GetElement(segment.ElementId) is Wall wall){edges.Add(new(line,wall));walls[wall.Id.Value]=wall;}
   var box=room.get_BoundingBox(null);if(box==null)continue;
   var candidates=new List<XYZ>();
   // Search the interior rather than relying on a room insertion point near a wall.
   for(int ix=1;ix<=15;ix++)for(int iy=1;iy<=15;iy++)
   {var p=new XYZ(box.Min.X+(box.Max.X-box.Min.X)*ix/16,box.Min.Y+(box.Max.Y-box.Min.Y)*iy/16,z);if(room.IsPointInRoom(new(p.X,p.Y,z+Implantation.Feet(1))))candidates.Add(p);}
   if(candidates.Count==0){pending.Add("Ambiente "+room.Number+": não foi encontrada posição interna para identificar/cotar.");continue;}
   var mid=new XYZ((box.Min.X+box.Max.X)/2,(box.Min.Y+box.Max.Y)/2,z);
   var anchor=candidates.OrderBy(p=>p.DistanceTo(mid)).First();
   var raw=room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString()??"Ambiente";
   // Room.Name appends the number; ROOM_NAME contains only the user-authored name.
   string name=raw.Trim();var number=room.Number?.Trim();if(!string.IsNullOrEmpty(number)&&name.EndsWith(" "+number,StringComparison.Ordinal))name=name[..^(number.Length+1)].TrimEnd();
   TextNote? note=null;Rect? labelRect=null;
   const string compactName="ARQ - Ambientes 2 mm - VEBRAM";
   var compact=new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault(t=>t.Name==compactName);
   if(compact==null){compact=(TextNoteType)textType.Duplicate(compactName);compact.get_Parameter(BuiltInParameter.TEXT_SIZE).Set(Implantation.Feet(.002));}
   foreach(var format in new[]{(22,textType),(14,textType),(10,textType),(7,textType),(14,compact),(10,compact),(7,compact)})
   {
    string label=Wrap(name,format.Item1)+"\n"+(room.Area*.09290304).ToString("0.00",Implantation.Pt)+"m²";
    note=TextNote.Create(doc,view.Id,anchor,label,new TextNoteOptions(format.Item2.Id){HorizontalAlignment=HorizontalTextAlignment.Center,VerticalAlignment=VerticalTextAlignment.Middle});
    doc.Regenerate();var original=TextBounds(note,view.Scale);
    if(original!=null)foreach(var point in candidates.OrderBy(p=>p.DistanceTo(anchor)))
    {
     var delta=point-anchor;var rect=new Rect(original.X0+delta.X,original.Y0+delta.Y,original.X1+delta.X,original.Y1+delta.Y);
     if(occupied.Any(b=>b.Overlaps(rect,paper*.12))||!Inside(room,rect,z+Implantation.Feet(1)))continue;
     ElementTransformUtils.MoveElement(doc,note.Id,delta);occupied.Add(rect);labelRect=rect;break;
    }
    if(labelRect!=null)break;
    doc.Delete(note.Id);note=null;
   }
   if(note==null)
   {
    // Keep the room identified inside; report an unresolved fit instead of drawing external leaders.
    note=TextNote.Create(doc,view.Id,anchor,Wrap(name,10)+"\n"+(room.Area*.09290304).ToString("0.00",Implantation.Pt)+"m²",new TextNoteOptions(textType.Id){HorizontalAlignment=HorizontalTextAlignment.Center,VerticalAlignment=VerticalTextAlignment.Middle});
    pending.Add("Ambiente "+room.Number+": conferir encaixe do rótulo interno; nenhuma chamada externa foi criada.");
    doc.Regenerate();var rb=TextBounds(note,view.Scale);labelRect=rb;occupied.Add(labelRect);
   }
   made.Add(note);
   if(prefs.InternalDimensions)
   {
    foreach(bool horizontal in new[]{true,false})
    {
     double cross=horizontal?anchor.Y:anchor.X;
     var sides=edges.Where(e=>Math.Abs(horizontal?e.Line.Direction.Y:e.Line.Direction.X)>.999&&Crosses(e.Line,cross,horizontal)).OrderBy(e=>horizontal?e.Line.GetEndPoint(0).X:e.Line.GetEndPoint(0).Y).ToList();
     var low=sides.LastOrDefault(e=>(horizontal?e.Line.GetEndPoint(0).X:e.Line.GetEndPoint(0).Y)<(horizontal?anchor.X:anchor.Y));
     var high=sides.FirstOrDefault(e=>(horizontal?e.Line.GetEndPoint(0).X:e.Line.GetEndPoint(0).Y)>(horizontal?anchor.X:anchor.Y));
     if(low==null||high==null){pending.Add("Ambiente "+room.Number+": limite "+(horizontal?"horizontal":"vertical")+" não é um par de paredes ortogonais.");continue;}
     var a=Face(doc,low);var b=Face(doc,high);if(a==null||b==null){pending.Add("Ambiente "+room.Number+": referência de acabamento não disponível.");continue;}
     double start=horizontal?low.Line.GetEndPoint(0).X:low.Line.GetEndPoint(0).Y;
     double end=horizontal?high.Line.GetEndPoint(0).X:high.Line.GetEndPoint(0).Y;
     var p0=horizontal?new XYZ(start,cross,z):new XYZ(cross,start,z);var p1=horizontal?new XYZ(end,cross,z):new XYZ(cross,end,z);
     // Offset dimension text toward one side of the room to leave its central label clear.
     double margin=paper*1.2;double crossTarget=horizontal?box.Min.Y+margin:box.Max.X-margin; double shift=horizontal?cross-crossTarget:crossTarget-cross;
     var offset=horizontal?new XYZ(0,-shift,0):new XYZ(shift,0,0);p0+=offset;p1+=offset;
     var dimensionBand=horizontal?new Rect(p0.X,(p0.Y+p1.Y)/2-paper*.7,p1.X,(p0.Y+p1.Y)/2+paper*.7):new Rect((p0.X+p1.X)/2-paper*.7,p0.Y,(p0.X+p1.X)/2+paper*.7,p1.Y);
     if(labelRect!=null&&labelRect.Overlaps(dimensionBand,0)){pending.Add("Ambiente "+room.Number+": cota "+(horizontal?"horizontal":"vertical")+" pendente para preservar leitura do rótulo.");continue;}
     if(TryDimension(doc,view,dimType,new[]{a,b},p0,p1,made,pending,"Ambiente "+room.Number))
     {roomDims++;var center=(p0+p1)/2;occupied.Add(new(center.X-paper*2,center.Y-paper,center.X+paper*2,center.Y+paper));}
    }
   }

  }
  foreach(var wall in walls.Values.OrderBy(w=>w.Id.Value))
  {
   if(wall.Location is not LocationCurve {Curve:Line axis})continue;
   var direction=new XYZ(axis.Direction.X,axis.Direction.Y,0).Normalize();var normal=new XYZ(-direction.Y,direction.X,0);var midpoint=(axis.GetEndPoint(0)+axis.GetEndPoint(1))/2;midpoint=new XYZ(midpoint.X,midpoint.Y,z);
   if(prefs.WallThickness)
   {
    var ext=Side(wall,ShellLayerType.Exterior);var interior=Side(wall,ShellLayerType.Interior);
    if(ext!=null&&interior!=null&&TryDimension(doc,view,dimType,new[]{ext,interior},midpoint-normal*wall.Width,midpoint+normal*wall.Width,made,pending,"Espessura da parede "+wall.Id.Value))wallDims++;
   }
   if(prefs.OpeningWidths)
   {
    foreach(var opening in wall.FindInserts(true,false,false,false).Select(id=>doc.GetElement(id)).OfType<FamilyInstance>())
    {
     if(opening.Category?.Id.Value is not ((long)BuiltInCategory.OST_Doors) and not ((long)BuiltInCategory.OST_Windows))continue;
     var a=opening.GetReferences(FamilyInstanceReferenceType.Left).FirstOrDefault();var b=opening.GetReferences(FamilyInstanceReferenceType.Right).FirstOrDefault();
     if(a==null||b==null){pending.Add("Esquadria "+opening.Id.Value+": família sem referências esquerda/direita utilizáveis.");continue;}
     var point=(opening.Location as LocationPoint)?.Point??midpoint;point=new XYZ(point.X,point.Y,z)+normal*Implantation.Feet(.5);
     if(TryDimension(doc,view,dimType,new[]{a,b},point-direction,point+direction,made,pending,"Largura da esquadria "+opening.Id.Value))openingDims++;
    }
   }
  }
  return new(roomDims,wallDims,openingDims,external,pending);
 }
 static Rect TextBounds(TextNote note,int scale)=>new(note.Coord.X-note.Width*scale/2,note.Coord.Y-note.Height*scale/2,note.Coord.X+note.Width*scale/2,note.Coord.Y+note.Height*scale/2);
 static bool Crosses(Line line,double cross,bool horizontal){var a=line.GetEndPoint(0);var b=line.GetEndPoint(1);double low=horizontal?Math.Min(a.Y,b.Y):Math.Min(a.X,b.X),high=horizontal?Math.Max(a.Y,b.Y):Math.Max(a.X,b.X);return cross>low+.01&&cross<high-.01;}
 static bool Inside(Room room,Rect b,double z)=>new[]{new XYZ(b.X0,b.Y0,z),new XYZ(b.X0,b.Y1,z),new XYZ(b.X1,b.Y0,z),new XYZ(b.X1,b.Y1,z),new XYZ((b.X0+b.X1)/2,(b.Y0+b.Y1)/2,z)}.All(room.IsPointInRoom);
 static Reference? Side(Wall wall,ShellLayerType side){try{return HostObjectUtils.GetSideFaces(wall,side).FirstOrDefault();}catch(Autodesk.Revit.Exceptions.ArgumentException){return null;}}
 static Reference? Face(Document doc,Edge edge)
 {
  var point=(edge.Line.GetEndPoint(0)+edge.Line.GetEndPoint(1))/2;
  foreach(var side in new[]{ShellLayerType.Exterior,ShellLayerType.Interior})
  {
   var reference=Side(edge.Wall,side);if(reference==null||edge.Wall.GetGeometryObjectFromReference(reference) is not PlanarFace face)continue;
   if(Math.Abs(face.FaceNormal.Z)>.001||Math.Abs((point-face.Origin).DotProduct(face.FaceNormal))>Implantation.Feet(.015))continue;
   return reference;
  }
  return null;
 }
 static bool TryDimension(Document doc,View view,DimensionType type,IEnumerable<Reference> references,XYZ a,XYZ b,List<Element> made,List<string> pending,string label)
 {
  using var sub=new SubTransaction(doc);sub.Start();
  try
  {
   var refs=new ReferenceArray();foreach(var r in references)refs.Append(r);doc.Regenerate();
   var dimension=doc.Create.NewDimension(view,Line.CreateBound(a,b),refs,type);doc.Regenerate();
   if(dimension.Value is not double value||value<=doc.Application.ShortCurveTolerance)throw new InvalidOperationException("Referências não produziram uma distância válida.");
   sub.Commit();made.Add(dimension);return true;
  }
  catch(Autodesk.Revit.Exceptions.ArgumentException ex){sub.RollBack();pending.Add(label+": "+ex.Message);return false;}
  catch(InvalidOperationException ex){sub.RollBack();pending.Add(label+": "+ex.Message);return false;}
 }
 static string Wrap(string value,int limit)
 {var lines=new List<string>();string line="";foreach(var word in value.Split(' ',StringSplitOptions.RemoveEmptyEntries)){if(line.Length>0&&line.Length+word.Length+1>limit){lines.Add(line);line="";}line+=(line.Length==0?"":" ")+word;}if(line.Length>0)lines.Add(line);return string.Join("\n",lines);}
}




