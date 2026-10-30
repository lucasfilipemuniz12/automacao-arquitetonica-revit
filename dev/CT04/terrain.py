from pyrevit.api import DB
from pyrevit.revit.db import create
from System.Collections.Generic import List
def net(t,items):
    result=List[t]()
    for item in items: result.Add(item)
    return result

BOUNDARY=[(0,0),(12,0),(14.5,2.5),(15.2,26),(-1,24.5)]
def xyz(x,y,z=0): return DB.XYZ(x/.3048,y/.3048,z/.3048)
def natural(x,y): return 100+.16*y-.06*x+.002*(x-7)**2-100.4
def loop(points): return create.create_curve_loop([(x/.3048,y/.3048) for x,y in points])
def rect(x,y,xx,yy): return loop([(x,y),(xx,y),(xx,yy),(x,yy)])
def inside(x,y):
    yes=False
    for i,a in enumerate(BOUNDARY):
        b=BOUNDARY[(i+1)%len(BOUNDARY)]
        if (a[1]>y)!=(b[1]>y) and x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0]: yes=not yes
    return yes
def curves(path):
    lines=open(path,encoding='cp1252').read().splitlines()
    data=[]; entity=''; layer=''; z=0.; points=[]; x=None
    def flush():
        if entity=='LWPOLYLINE' and layer.startswith('CURVAS'):
            data.extend((p[0],p[1],z) for i,p in enumerate(points) if i%4==0 or i==len(points)-1)
    for i in range(0,len(lines)-1,2):
        code,value=lines[i].strip(),lines[i+1].strip()
        if code=='0': flush();entity=value;layer='';z=0.;points=[];x=None
        elif code=='8': layer=value
        elif code=='38': z=float(value)
        elif code=='10': x=float(value)
        elif code=='20' and x is not None: points.append((x,float(value)));x=None
    flush();return data
def apply(doc,path):
    if doc.Title!='Casa_Teste_04': raise Exception('Somente CT04 em modelagem')
    if DB.FilteredElementCollector(doc).OfClass(DB.Toposolid).GetElementCount(): raise Exception('Terreno ja existe; nao duplicar')
    ground=next(l for l in DB.FilteredElementCollector(doc).OfClass(DB.Level) if l.Name=='Térreo')
    topo_type=doc.GetElement(DB.ElementId(319146)) # observed template type Pradaria - 1200 mm
    points=[]
    for i,a in enumerate(BOUNDARY):
        b=BOUNDARY[(i+1)%len(BOUNDARY)]
        for j in range(13):
            x=a[0]+(b[0]-a[0])*j/12.;y=a[1]+(b[1]-a[1])*j/12.
            points.append(xyz(x,y,natural(x,y)))
    for x,y,xx,yy in [(1.5,3,9.5,13),(9.6,3,12.6,8.5)]:
        points.extend(xyz(a,b,natural(a,b)) for a,b in [(x,y),(xx,y),(xx,yy),(x,yy)])
    cp=[(x,y,z) for x,y,z in curves(path) if inside(x,y) and not(1.5<=x<=9.5 and 3<=y<=13)]
    points.extend(xyz(x,y,z-100.4) for x,y,z in cp)
    points=list({(round(p.X,6),round(p.Y,6)):p for p in points}.values())
    tx=DB.Transaction(doc,'CT04: terreno pelas curvas, corte e aterro ate as lajes');tx.Start()
    try:
        topo_type=topo_type.Duplicate('CT04 terreno - espessura de modelagem')
        cs=topo_type.GetCompoundStructure();idx=cs.VariableLayerIndex;cs.SetLayerWidth(idx,cs.GetLayerWidth(idx)+max(0.,-min(p.Z for p in points)));topo_type.SetCompoundStructure(cs)
        main=DB.Toposolid.Create(doc,net(DB.CurveLoop,[loop(BOUNDARY),rect(1.5,3,9.5,13)]),net(DB.XYZ,points),topo_type.Id,ground.Id)
        main.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set('CT04:terreno pelas curvas DXF; grade natural; corte/aterro sob os edificios em solidos separados')
        ids=[main.Id.Value]
        for x,y,xx,yy,z in [(1.5,3,9.5,11,-.22),(1.5,11,9.5,13,-.07)]:
            t=DB.Toposolid.Create(doc,net(DB.CurveLoop,[rect(x,y,xx,yy)]),net(DB.XYZ,[xyz(x,y,z),xyz(xx,y,z),xyz(xx,yy,z),xyz(x,yy,z)]),topo_type.Id,ground.Id)
            t.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set('CT04:corte/aterro encostado na face inferior do piso/laje')
            ids.append(t.Id.Value)
        doc.Regenerate()
        for floor in DB.FilteredElementCollector(doc).OfClass(DB.Floor):
            if main.CanBeExcavatedBy(floor.Id): main.ExcavateBy(floor.Id)
        joins=[]
        for i in ids[1:]:
            sub=DB.SubTransaction(doc);sub.Start()
            try:
                DB.JoinGeometryUtils.JoinGeometry(doc,main,doc.GetElement(DB.ElementId(i)))
                sub.Commit();joins.append(i)
            except: sub.RollBack()
        doc.Regenerate()
        status=tx.Commit()
        if status!=DB.TransactionStatus.Committed: raise Exception(str(status))
        return {'toposolids':ids,'curve_points':len(cp),'joins':joins,'bounds':[{'id':i,'min_z':doc.GetElement(DB.ElementId(i)).get_BoundingBox(None).Min.Z*.3048,'max_z':doc.GetElement(DB.ElementId(i)).get_BoundingBox(None).Max.Z*.3048} for i in ids],'warnings':[w.GetDescriptionText() for w in doc.GetWarnings()]}
    except:
        if tx.GetStatus()==DB.TransactionStatus.Started: tx.RollBack()
        raise
