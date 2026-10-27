# -*- coding: utf-8 -*-
"""Etapa 4: escada em U, telhado 4 águas 30%, guarda-corpos do terraço, ambientes e DXF vinculado."""
import clr
from pyrevit.api import DB
from Autodesk.Revit.DB.Architecture import Stairs, StairsRun, StairsLanding, StairsRunJustification, Railing, RailingType, StairsType
from System.Collections.Generic import List
from ct03_base import *

import time as _time
class _Accept(DB.IFailuresPreprocessor):
    __namespace__ = 'ct03_stairs_%d' % int(_time.time() * 1000)  # nome único: o pythonnet não aceita registrar a mesma classe duas vezes
    def PreprocessFailures(self, fa):
        return DB.FailureProcessingResult.Continue

def plan_view(doc, lv, prefix='PRE - Layout'):
    vs = [v for v in DB.FilteredElementCollector(doc).OfClass(DB.ViewPlan) if not v.IsTemplate and v.GenLevel and v.GenLevel.Id == lv.Id]
    return next((v for v in vs if v.Name.startswith(prefix)), vs[0])

def stair(doc, I, T):
    rise = (N['terreo'] - N['inferior']) / 17.0
    scope = DB.StairsEditScope(doc, 'CT03 / Escada em U')
    sid = scope.Start(I.Id, T.Id)
    tr = DB.Transaction(doc, 'CT03 / Lances e patamar'); tr.Start()
    st = doc.GetElement(sid)
    stype = find_type(doc, StairsType, 'VEBRAM / Escada CT03 (piso 25 cm)')
    if stype is None:
        stype = find_type(doc, StairsType, 'VEBRAM / Escada de concreto (estudo)').Duplicate('VEBRAM / Escada CT03 (piso 25 cm)')
        stype.get_Parameter(DB.BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH).Set(0.25 * M)
        mk = stype.get_Parameter(DB.BuiltInParameter.ALL_MODEL_TYPE_MARK)
        if mk and not mk.IsReadOnly: mk.Set('CT03-ESC')
    st.ChangeTypeId(stype.Id)
    st.get_Parameter(DB.BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS).Set(17)
    st.get_Parameter(DB.BuiltInParameter.STAIRS_ACTUAL_TREAD_DEPTH).Set(0.25 * M)
    # lance 1 junto a x = 10,5, subindo para o fundo (y crescente); lance 2 junto a x = 7,5, voltando para a frente
    r1 = StairsRun.CreateStraightRun(doc, sid, DB.Line.CreateBound(P(9.725, 9.825, N['inferior']), P(9.725, 11.575, N['inferior'])), StairsRunJustification.Center)
    r1.ActualRunWidth = 1.40 * M
    doc.Regenerate()
    zl = N['inferior'] + r1.TopElevation / M  # TopElevation é relativa à base da escada; o caminho do lance usa cota absoluta
    r2 = StairsRun.CreateStraightRun(doc, sid, DB.Line.CreateBound(P(8.275, 11.575, zl), P(8.275, 9.575, zl)), StairsRunJustification.Center)
    r2.ActualRunWidth = 1.40 * M
    StairsLanding.CreateAutomaticLanding(doc, r1.Id, r2.Id)
    tag(st, 'stairs')
    tr.Commit()
    scope.Commit(_Accept())
    st = doc.GetElement(sid)
    return {'espelhos': st.ActualRisersNumber, 'espelho_cm': round(st.ActualRiserHeight / M * 100, 2), 'piso_cm': round(st.ActualTreadDepth / M * 100, 2),
            'base': round(st.BaseElevation / M, 3), 'topo': round(st.TopElevation / M, 3)}

def run(doc):
    guard(doc)
    out = {}
    I = level(doc, 'Inferior'); T = level(doc, 'Térreo'); C = level(doc, 'Cobertura')
    # ---- V11 reposicionado (altura livre sob o lance 2) — hipótese registrada
    t = DB.Transaction(doc, 'CT03 / V11 reposicionado'); t.Start()
    try:
        old = [e for e in existing(doc, 'opening:V11')]
        for e in old: doc.Delete(e.Id)
        w = next(x for x in DB.FilteredElementCollector(doc).OfClass(DB.Wall) if comment(x) == PREFIX + 'wall:I:14')
        op = doc.Create.NewOpening(w, DB.XYZ(7.5 * M, -10.65 * M, I.Elevation), DB.XYZ(7.5 * M, -9.65 * M, I.Elevation + 2.10 * M)); tag(op, 'opening:V11')
        t.Commit()
    finally:
        if t.GetStatus() == DB.TransactionStatus.Started: t.RollBack()
    # ---- escada
    if not existing(doc, 'stairs'):
        out['escada'] = stair(doc, I, T)
    t = DB.Transaction(doc, 'CT03 / Telhado, guarda-corpos e ambientes'); t.Start()
    try:
        # ---- telhado 4 águas, 30%, beiral 0,60, apoiado na laje de forro (+3,15)
        rt = find_type(doc, DB.RoofType, 'VEBRAM COB / telha cerâmica sobre suporte')
        ca = DB.CurveArray()
        ring = rect(0.9, 3.325, 11.1, 13.175)
        for a, b in zip(ring, ring[1:] + ring[:1]): ca.Append(DB.Line.CreateBound(P(a[0], a[1], N['cobertura_laje']), P(b[0], b[1], N['cobertura_laje'])))
        res = doc.Create.NewFootPrintRoof(ca, C, rt, DB.ModelCurveArray())
        roof, mca = (res[0], res[1]) if isinstance(res, tuple) else (res, None)
        if mca is None:
            mca = roof.GetProfiles()[0]
        for c in mca:
            roof.set_DefinesSlope(c, True); roof.set_SlopeAngle(c, 0.30)
        tag(roof, 'roof:4aguas_30')
        doc.Regenerate(); bb = roof.get_BoundingBox(None)
        out['telhado'] = {'base': round(bb.Min.Z / M, 3), 'cumeeira': round(bb.Max.Z / M, 3)}
        # ---- guarda-corpos do terraço (1,10 m)
        rtype = find_type(doc, RailingType, '1100mm')
        path = DB.CurveLoop()
        pts = [(1.5, 12.6), (1.5, 15.95), (10.5, 15.95), (10.5, 12.6)]
        for a, b in zip(pts, pts[1:]): path.Append(DB.Line.CreateBound(P(a[0], a[1], N['terreo']), P(b[0], b[1], N['terreo'])))
        rail = Railing.Create(doc, path, rtype.Id, T.Id); tag(rail, 'railing:terraco')
        # ---- separação do terraço (aberto) para fechar o ambiente
        vT = plan_view(doc, T)
        sp = DB.SketchPlane.Create(doc, DB.Plane.CreateByNormalAndOrigin(DB.XYZ.BasisZ, DB.XYZ(0, 0, T.Elevation)))
        sep = DB.CurveArray()
        for a, b in zip(pts, pts[1:]): sep.Append(DB.Line.CreateBound(P(a[0], a[1], N['terreo']), P(b[0], b[1], N['terreo'])))
        for e in doc.Create.NewRoomBoundaryLines(sp, sep, vT): tag(e, 'separator:terraco')
        doc.Regenerate()
        # ---- ambientes
        rooms = []; nT = 101; nI = 1
        for a in CASA['ambientes']:
            if a['p'] not in ('T', 'I'): continue
            lv = T if a['p'] == 'T' else I
            cx, cy = (a['x1'] + a['x2']) / 2, (a['y1'] + a['y2']) / 2
            if a['nome'] == 'Escada': cx = 7.5 + 0.3 if a['p'] == 'T' else 9.0
            r = doc.Create.NewRoom(lv, DB.UV(cx * M, -cy * M))
            r.get_Parameter(DB.BuiltInParameter.ROOM_NAME).Set(a['nome'])
            num = '%03d' % (nT if a['p'] == 'T' else nI)
            r.get_Parameter(DB.BuiltInParameter.ROOM_NUMBER).Set(num)
            if a['p'] == 'T': nT += 1
            else: nI += 1
            tag(r, 'room:' + a['p'] + ':' + a['nome'])
            rooms.append(r)
        doc.Regenerate()
        out['ambientes'] = [(r.get_Parameter(DB.BuiltInParameter.ROOM_NUMBER).AsString(), r.get_Parameter(DB.BuiltInParameter.ROOM_NAME).AsString(), doc.GetElement(r.LevelId).Name, round(r.Area / M / M, 2)) for r in rooms]
        t.Commit()
    finally:
        if t.GetStatus() == DB.TransactionStatus.Started: t.RollBack()
    # ---- DXF vinculado (não importado), só na planta do térreo, origem com origem
    t = DB.Transaction(doc, 'CT03 / DXF vinculado'); t.Start()
    try:
        if not list(DB.FilteredElementCollector(doc).OfClass(DB.ImportInstance)):
            o = DB.DWGImportOptions(); o.Unit = DB.ImportUnit.Meter; o.Placement = DB.ImportPlacement.Origin; o.ThisViewOnly = True
            path = os.path.join(HERE, '..', 'referencia', 'Casa_Teste_03.dxf')
            res = doc.Link(os.path.normpath(path), o, plan_view(doc, T), DB.ElementId.InvalidElementId)  # pythonnet devolve (ok, id)
            out['dxf_vinculado'] = str(res)
        t.Commit()
    finally:
        if t.GetStatus() == DB.TransactionStatus.Started: t.RollBack()
    return out
