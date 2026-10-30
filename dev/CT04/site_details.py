from pyrevit.api import DB
from pyrevit.revit.db import create
from terrain import BOUNDARY,xyz,natural,net

def apply(doc):
    if doc.Title!='Casa_Teste_04': raise Exception('Somente CT04 em modelagem')
    if any((e.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() or '').startswith('CT04:divisa') for e in DB.FilteredElementCollector(doc).OfClass(DB.Wall)): raise Exception('Divisas ja modeladas')
    level=next(l for l in DB.FilteredElementCollector(doc).OfClass(DB.Level) if l.Name=='Térreo')
    wt=next(t for t in DB.FilteredElementCollector(doc).OfClass(DB.WallType) if t.Name=='ARQ PAR-01 / cerâmico 9 + revestimentos 3+3')
    main=next(t for t in DB.FilteredElementCollector(doc).OfClass(DB.Toposolid) if t.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString().startswith('CT04:terreno pelas curvas'))
    tx=DB.Transaction(doc,'CT04: muros de divisa, arrimo e gradis nas duas frentes');tx.Start()
    try:
        ids=[]
        for i,height in [(3,1.8),(4,1.8),(0,.4),(1,.4),(2,.4)]:
            a=BOUNDARY[i];b=BOUNDARY[(i+1)%5];p=xyz(a[0],a[1],natural(*a));q=xyz(b[0],b[1],natural(*b))
            points=[p,q,q+DB.XYZ.BasisZ*(height/.3048),p+DB.XYZ.BasisZ*(height/.3048)]
            profile=net(DB.Curve,[DB.Line.CreateBound(v,points[(j+1)%4]) for j,v in enumerate(points)])
            w=DB.Wall.Create(doc,profile,wt.Id,level.Id,False)
            w.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set('CT04:divisa — altura da ficha; espessura do tipo do template')
            ids.append(w.Id.Value)
        w=create.create_wall(xyz(.9,13),xyz(10,13),wt,level=level,height=1/.3048,base_offset=0,doc=doc)
        w.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set('CT04:arrimo y=13 — altura 1,00 m; topo quintal +1,00; conferir transicao ao terreno natural')
        ids.append(w.Id.Value)
        rt=doc.GetElement(DB.ElementId(25223)).Duplicate('CT04 — gradil 1,80 m')
        rt.TopRailHeight=1.8/.3048
        p=rt.get_Parameter(DB.BuiltInParameter.ALL_MODEL_TYPE_MARK)
        if p and not p.IsReadOnly:p.Set('CT04-GRADIL')
        for i in [0,1,2]:
            a=BOUNDARY[i];b=BOUNDARY[i+1]
            path=DB.CurveLoop.Create(net(DB.Curve,[DB.Line.CreateBound(xyz(*a),xyz(*b))]))
            r=DB.Architecture.Railing.Create(doc,path,rt.Id,level.Id);r.HostId=main.Id
            r.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set('CT04:gradil nas frentes, altura total 1,80 m sobre o terreno; mureta 0,40 m')
            ids.append(r.Id.Value)
        doc.Regenerate();status=tx.Commit()
        if status!=DB.TransactionStatus.Committed:raise Exception(str(status))
        return {'ids':ids,'warnings':[w.GetDescriptionText() for w in doc.GetWarnings()]}
    except:
        if tx.GetStatus()==DB.TransactionStatus.Started:tx.RollBack()
        raise
