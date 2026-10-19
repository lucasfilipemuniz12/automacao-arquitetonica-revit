using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
namespace Vebram.Revit;

public sealed record PlanAnnotationResult(int RoomDimensions,int WallDimensions,int OpeningDimensions,int ExternalLabels,List<string> Pending);

// Coordinates are Revit internal feet; distances and references come from the model.
public static class PlanAnnotations
{
 sealed record Edge(Line Line,Wall Wall);
 internal sealed record Rect(double X0,double Y0,double X1,double Y1)
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
   string name=RoomNameText.Display(raw);
   TextNote? note=null;Rect? labelRect=null;
   var compact=LegalStyles.Text(doc,2);
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
     // Several lines to try (near both edges, then a quarter of the way in), so the dimension rarely has to be skipped.
     double margin=paper*1.2;double lo=horizontal?box.Min.Y:box.Min.X,hi=horizontal?box.Max.Y:box.Max.X;
     var targets=horizontal?new[]{lo+margin,hi-margin,lo+(hi-lo)*.25,lo+(hi-lo)*.75}:new[]{hi-margin,lo+margin,lo+(hi-lo)*.75,lo+(hi-lo)*.25};
     var baseStart=p0;var baseEnd=p1;Rect? chosenBand=null;XYZ q0=p0,q1=p1;bool irregular=false;
     foreach(bool strict in new[]{true,false})
     {
      foreach(double crossTarget in targets)
      {
       double shift=horizontal?cross-crossTarget:crossTarget-cross;var offset=horizontal?new XYZ(0,-shift,0):new XYZ(shift,0,0);
       var s0=baseStart+offset;var s1=baseEnd+offset;
       var band=horizontal?new Rect(s0.X,(s0.Y+s1.Y)/2-paper*.7,s1.X,(s0.Y+s1.Y)/2+paper*.7):new Rect((s0.X+s1.X)/2-paper*.7,s0.Y,(s0.X+s1.X)/2+paper*.7,s1.Y);
       if(labelRect!=null&&labelRect.Overlaps(band,0))continue;
       var middle=(s0+s1)/2;if(!room.IsPointInRoom(new(middle.X,middle.Y,z+Implantation.Feet(1))))continue;
       // Irregular (L/U-shaped) rooms: the shifted line must still run between the same two facing walls,
       // with no other boundary wall in between; otherwise the dimension would cross a wall or measure a notch.
       if(!FacingPairAt(edges,low,high,crossTarget,horizontal,horizontal?anchor.X:anchor.Y)){irregular=true;continue;}
       if(strict&&occupied.Any(r=>r.Overlaps(band,paper*.1)))continue;
       chosenBand=band;q0=s0;q1=s1;break;
      }
      if(chosenBand!=null)break;
     }
     if(chosenBand==null){pending.Add("Ambiente "+room.Number+": cota "+(horizontal?"horizontal":"vertical")+(irregular?" pendente: ambiente pequeno ou irregular, sem faixa livre entre as mesmas duas paredes. Cote manualmente.":" pendente para preservar leitura do rótulo."));continue;}
     p0=q0;p1=q1;var dimensionBand=chosenBand;
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
  foreach(var note in new FilteredElementCollector(doc,view.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(n=>!made.Contains(n)))occupied.Add(TextBounds(note,view.Scale));
  // Wall centre lines (with half thickness) and dimension lines already drawn in this view.
  var obstacles=new List<(Line Line,double Half)>();
  foreach(var wall in new FilteredElementCollector(doc,view.Id).OfClass(typeof(Wall)).Cast<Wall>())
   if((wall.Location as LocationCurve)?.Curve is Line wallLine&&wallLine.IsBound)obstacles.Add((wallLine,wall.Width/2));
  foreach(var dimension in new FilteredElementCollector(doc,view.Id).OfClass(typeof(Dimension)).Cast<Dimension>())
  {
   if(dimension.DimensionShape!=DimensionShape.Linear||dimension.Curve is not Line dimensionLine)continue;
   if(dimension.get_BoundingBox(view) is {} db)
   {
    var a=dimensionLine.Project(db.Min)?.XYZPoint;var b=dimensionLine.Project(db.Max)?.XYZPoint;
    if(a!=null&&b!=null&&a.DistanceTo(b)>doc.Application.ShortCurveTolerance)obstacles.Add((Line.CreateBound(a,b),0));
   }
   // Native linear dimension curves are usually unbound; their text must also be reserved.
   void Reserve(XYZ point,string value)
   {double height=paper*1.5,width=Math.Max(paper*2,value.Length*paper*.65);occupied.Add(new(point.X-width/2,point.Y-height/2,point.X+width/2,point.Y+height/2));}
   if(dimension.NumberOfSegments==0)Reserve(dimension.TextPosition,dimension.ValueString??"");
   else foreach(DimensionSegment segment in dimension.Segments)Reserve(segment.TextPosition,segment.ValueString??"");
  }
  int openingLabels=0;
  foreach(var opening in new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType()
   .Where(e=>e.Category?.Id.Value is (long)BuiltInCategory.OST_Doors or (long)BuiltInCategory.OST_Windows).OrderBy(e=>e.Id.Value))
  {
   string code=opening.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString()?.Trim()??"";
   if(code.Length==0){pending.Add("Esquadria "+opening.Id.Value+": preencha a Marca para identificar na planta e no quadro.");continue;}
   // A manually placed native tag already identifies this opening; do not duplicate it.
   if(new FilteredElementCollector(doc,view.Id).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Any(t=>t.GetTaggedLocalElementIds().Contains(opening.Id)))continue;
   var box=opening.get_BoundingBox(view);if(box==null)continue;
   // Door boxes include the leaf swing; point leaders to the actual insertion/opening instead.
   var center=(opening.Location as LocationPoint)?.Point??(box.Min+box.Max)/2;center=new XYZ(center.X,center.Y,z);
   var facing=opening is FamilyInstance family? family.FacingOrientation:box.Max.X-box.Min.X>=box.Max.Y-box.Min.Y?XYZ.BasisY:XYZ.BasisX;facing=new XYZ(facing.X,facing.Y,0);if(facing.GetLength()<.001)facing=XYZ.BasisY;else facing=facing.Normalize();
   var along=new XYZ(-facing.Y,facing.X,0);TextNote? chosen=null;
   // Rings of growing distance, eight directions each. Beyond 7 mm on paper the label gets a short leader to its opening.
   // First pass also avoids walls and dimension lines; the second pass keeps the earlier behaviour so nothing becomes unplaceable.
   var directions=new[]{facing,-facing,along,-along,(facing+along).Normalize(),(facing-along).Normalize(),(-facing+along).Normalize(),(-facing-along).Normalize()};
   foreach(bool avoidLines in new[]{true})
   {
    foreach(double distance in new[]{.0035,.005,.007,.009,.011,.014,.018,.022,.028,.036})
    {
     foreach(var delta in directions)
     {
      var note=TextNote.Create(doc,view.Id,center+delta*Implantation.Feet(distance)*view.Scale,code,new TextNoteOptions(LegalStyles.Text(doc,2).Id){HorizontalAlignment=HorizontalTextAlignment.Center,VerticalAlignment=VerticalTextAlignment.Middle});doc.Regenerate();
      var rect=TextBounds(note,view.Scale);
      if(occupied.Any(r=>r.Overlaps(rect,paper*.2))||(avoidLines&&obstacles.Any(o=>TouchesLine(rect,o.Line,o.Half)))){doc.Delete(note.Id);continue;}
      if(distance>.007)
      {
       var target=center;
       if(opening is FamilyInstance hosted&&hosted.Host is Wall hostWall)
        target+=facing*((note.Coord-center).DotProduct(facing)>=0?hostWall.Width/2:-hostWall.Width/2);
       bool Clear(XYZ a,XYZ b)=>a.DistanceTo(b)<=doc.Application.ShortCurveTolerance||!occupied.Any(r=>TouchesLine(r,Line.CreateBound(a,b),paper*.1));
       XYZ? elbow=null;
       if(!Clear(note.Coord,target))
       {
        elbow=new[]{new XYZ(note.Coord.X,target.Y,z),new XYZ(target.X,note.Coord.Y,z)}.FirstOrDefault(p=>Clear(note.Coord,p)&&Clear(p,target));
        if(elbow==null){doc.Delete(note.Id);continue;}
       }
       // A label far from its opening is only acceptable with a leader; otherwise try the next position.
       try{var leader=note.AddLeader(TextNoteLeaderTypes.TNLT_STRAIGHT_R);leader.End=target;if(elbow!=null)leader.Elbow=elbow;}
       catch(Exception ex) when(ex is Autodesk.Revit.Exceptions.ApplicationException or InvalidOperationException){doc.Delete(note.Id);continue;}
      }
      occupied.Add(rect);chosen=note;break;
     }
     if(chosen!=null)break;
    }
    if(chosen!=null)break;
   }
   if(chosen==null){pending.Add("Esquadria "+code+": sem espaço para identificação mesmo com chamada; ajuste a anotação na planta.");continue;}
   made.Add(chosen);openingLabels++;
  }
  pending.Add(openingLabels+" identificações de esquadrias geradas com a Marca existente. Atualize a planta após alterar os códigos.");
  return new(roomDims,wallDims,openingDims,external,pending);
 }
 /// <summary>True when a line (thickened by half) passes through the rectangle; sampled every ~5 cm.</summary>
 internal static bool TouchesLine(Rect r,Line line,double half)
 {
  var a=line.GetEndPoint(0);var b=line.GetEndPoint(1);int steps=Math.Max(1,(int)Math.Ceiling(a.DistanceTo(b)/.05));
  for(int i=0;i<=steps;i++){var p=a+(b-a)*i/steps;if(p.X>r.X0-half&&p.X<r.X1+half&&p.Y>r.Y0-half&&p.Y<r.Y1+half)return true;}
  return false;
 }
 static Rect TextBounds(TextNote note,int scale)
 {
  var view=note.Document.GetElement(note.OwnerViewId) as View;
  var box=view==null?null:note.get_BoundingBox(view);
  return box!=null?new(box.Min.X,box.Min.Y,box.Max.X,box.Max.Y):new(note.Coord.X-note.Width*scale/2,note.Coord.Y-note.Height*scale/2,note.Coord.X+note.Width*scale/2,note.Coord.Y+note.Height*scale/2);
 }
 static bool Crosses(Line line,double cross,bool horizontal){var a=line.GetEndPoint(0);var b=line.GetEndPoint(1);double low=horizontal?Math.Min(a.Y,b.Y):Math.Min(a.X,b.X),high=horizontal?Math.Max(a.Y,b.Y):Math.Max(a.X,b.X);return cross>low+.01&&cross<high-.01;}
 /// <summary>True when, on the line at <paramref name="cross"/>, the nearest boundary walls on each side of <paramref name="along"/> are still <paramref name="low"/> and <paramref name="high"/>.</summary>
 static bool FacingPairAt(List<Edge> edges,Edge low,Edge high,double cross,bool horizontal,double along)
 {
  double Position(Edge e)=>horizontal?e.Line.GetEndPoint(0).X:e.Line.GetEndPoint(0).Y;
  var sides=edges.Where(e=>Math.Abs(horizontal?e.Line.Direction.Y:e.Line.Direction.X)>.999&&Crosses(e.Line,cross,horizontal)).ToList();
  var nearLow=sides.Where(e=>Position(e)<along).OrderBy(Position).LastOrDefault();
  var nearHigh=sides.Where(e=>Position(e)>along).OrderBy(Position).FirstOrDefault();
  // Compare positions, not segments: one wall face may be split into collinear boundary segments.
  return nearLow!=null&&nearHigh!=null&&Math.Abs(Position(nearLow)-Position(low))<1e-3&&Math.Abs(Position(nearHigh)-Position(high))<1e-3;
 }
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




