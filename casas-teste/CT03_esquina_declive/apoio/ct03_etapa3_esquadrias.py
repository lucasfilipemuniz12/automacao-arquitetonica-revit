# -*- coding: utf-8 -*-
"""Etapa 3: esquadrias hospedadas com as medidas da ficha e vãos livres recortados nas paredes."""
from pyrevit.api import DB
from ct03_base import *

def sym_by(doc, family, name):
    for s in DB.FilteredElementCollector(doc).OfClass(DB.FamilySymbol):
        if s.FamilyName == family and tname(s) == name: return s
    return None

def sized(doc, family, source, name, w, h, modular=False):
    s = sym_by(doc, family, name)
    if s is None:
        s = sym_by(doc, family, source).Duplicate(name)
        if modular:
            s.LookupParameter('Número de Módulos Básicos nM Largura').Set(int(round(w * 10)))
            s.LookupParameter('Número de Módulos Básicos nM Altura').Set(int(round(h * 10)))
            for pn in ('Ajuste de Coordenação Base AC Base', 'Distância do Nível'):
                p = s.LookupParameter(pn)
                if p and not p.IsReadOnly: p.Set(0.0)
        else:
            for bip in (DB.BuiltInParameter.DOOR_WIDTH, DB.BuiltInParameter.WINDOW_WIDTH):
                p = s.get_Parameter(bip)
                if p and not p.IsReadOnly: p.Set(w * M)
            for bip in (DB.BuiltInParameter.DOOR_HEIGHT, DB.BuiltInParameter.WINDOW_HEIGHT):
                p = s.get_Parameter(bip)
                if p and not p.IsReadOnly: p.Set(h * M)
    if not s.IsActive: s.Activate()
    return s

def host(doc, e):
    axis, val = e['parede'].split('='); v = float(val); mid = (e['de'] + e['ate']) / 2
    for i, w in enumerate(CASA['paredes']):
        if w['p'] != e['p']: continue
        if axis == 'y' and w['y1'] == w['y2'] == v and min(w['x1'], w['x2']) < mid < max(w['x1'], w['x2']): idx = i; break
        if axis == 'x' and w['x1'] == w['x2'] == v and min(w['y1'], w['y2']) < mid < max(w['y1'], w['y2']): idx = i; break
    else:
        raise Exception('Parede hospedeira não encontrada para ' + e['cod'])
    tagv = PREFIX + 'wall:%s:%02d' % (e['p'], idx)
    wall = next(x for x in DB.FilteredElementCollector(doc).OfClass(DB.Wall) if comment(x) == tagv)
    pt = P(mid, v) if axis == 'y' else P(v, mid)
    return wall, pt, axis, v

def run(doc):
    guard(doc)
    if existing(doc, 'opening:'): raise Exception('Esquadrias CT03 já existem; nada foi repetido.')
    out = []
    t = DB.Transaction(doc, 'CT03 / Esquadrias e vãos'); t.Start()
    try:
        T = level(doc, 'Térreo'); I = level(doc, 'Inferior')
        doc.Regenerate()
        for e in CASA['esquadrias']:
            wall, pt, axis, v = host(doc, e)
            lv = T if e['p'] == 'T' else I
            cod = e['cod']; tipo = e['tipo']
            if cod.startswith('V'):
                z0 = lv.Elevation; z1 = z0 + e['h'] * M
                if axis == 'y': a = DB.XYZ(e['de'] * M, -v * M, z0); b = DB.XYZ(e['ate'] * M, -v * M, z1)
                else: a = DB.XYZ(v * M, -e['ate'] * M, z0); b = DB.XYZ(v * M, -e['de'] * M, z1)
                op = doc.Create.NewOpening(wall, a, b); tag(op, 'opening:' + cod)
                out.append([cod, 'vão livre', e['l'], e['h']]); continue
            if cod == 'PG01':
                s = sized(doc, 'Porta de Correr Metálica com Divisões Horizontais', 'CT EX07 / P06', 'VEBRAM Portão metálico / 260 x 220', e['l'], e['h'], True)
            elif tipo.startswith('porta-janela'):
                s = sized(doc, 'Porta de Correr Metálica com Divisões Horizontais', 'CT EX07 / P06', 'VEBRAM Porta de correr / %d x %d' % (round(e['l'] * 100), round(e['h'] * 100)), e['l'], e['h'], True)
            elif cod.startswith('P'):
                s = sized(doc, 'M_Folha única', 'VEBRAM Madeira / 80 x 210', 'VEBRAM Madeira / %d x 210' % round(e['l'] * 100), e['l'], e['h'])
            elif 'correr' in tipo:
                s = sized(doc, 'VEBRAM Janela de correr 4 folhas', '150 x 90', '%d x %d' % (round(e['l'] * 100), round(e['h'] * 100)), e['l'], e['h'])
            else:
                s = sized(doc, 'VEBRAM Janela 2 módulos', '90 x 60', '%d x %d' % (round(e['l'] * 100), round(e['h'] * 100)), e['l'], e['h'])
            doc.Regenerate()
            inst = doc.Create.NewFamilyInstance(pt, s, wall, lv, DB.Structure.StructuralType.NonStructural)
            sill = e.get('peitoril', 0.0)
            p = inst.get_Parameter(DB.BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM)
            if p and not p.IsReadOnly: p.Set(sill * M)
            inst.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK).Set(cod); tag(inst, 'opening:' + cod)
            out.append([cod, s.FamilyName, tname(s), e['l'], e['h'], sill])
        doc.Regenerate()
        # conferência: cada esquadria precisa recortar a parede hospedeira
        bad = []
        for inst in DB.FilteredElementCollector(doc).OfClass(DB.FamilyInstance):
            c = comment(inst)
            if not c.startswith(PREFIX + 'opening:'): continue
            if inst.Host is None or inst.Id not in list(inst.Host.FindInserts(True, True, True, True)): bad.append(c)
        t.Commit()
    finally:
        if t.GetStatus() == DB.TransactionStatus.Started: t.RollBack()
    return {'esquadrias': out, 'sem_recorte': bad}
