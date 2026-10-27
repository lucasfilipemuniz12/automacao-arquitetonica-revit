# -*- coding: utf-8 -*-
"""Etapa 2: paredes por eixo, lajes, pisos acabados por ambiente, acessos e toposólido em declive."""
from pyrevit.api import DB
from System.Collections.Generic import List
from ct03_base import *

WALL_TYPES = {('T', 0.15): 'VEBRAM PAR-15 / cerâmico revestido', ('T', 0.10): 'VEBRAM PAR-10 / cerâmico revestido (CT03)',
              ('I', 0.15): 'VEBRAM PAR-15 / cerâmico revestido', ('I', 0.10): 'VEBRAM PAR-10 / cerâmico revestido (CT03)',
              ('I', 0.20): 'VEBRAM PAR-20 / concreto parcialmente enterrada (CT03)', ('I', 0.25): 'VEBRAM CONTENÇÃO / concreto 25 cm (CT03)'}

FINISH = {'PISO INTERNO / porcelanato': 'PISO INTERNO / porcelanato e contrapiso 5 cm',
          'PISO INTERNO / cerâmica': 'PISO INTERNO / cerâmica e contrapiso 5 cm',
          'PISO INTERNO / madeira': 'PISO INTERNO / madeira e contrapiso 5 cm',
          'PISO EXTERNO / cimentado': 'PISO EXTERNO / cimentado 5 cm',
          'PISO EXTERNO / cerâmica antiderrapante': 'PISO EXTERNO / cerâmica antiderrapante 5 cm',
          'CALÇADA / concreto': 'CALÇADA / concreto 8 cm'}

def floor(doc, rings, typename, lv, top, code):
    ft = find_type(doc, DB.FloorType, typename)
    if ft is None: raise Exception('Tipo de piso ausente: ' + typename)
    f = DB.Floor.Create(doc, loops(*rings), ft.Id, lv.Id)
    f.get_Parameter(DB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set((top - lv.Elevation / M) * M)
    tag(f, 'floor:' + code)
    return f

def run(doc):
    guard(doc)
    if existing(doc, 'wall:'): raise Exception('Paredes CT03 já existem; nada foi repetido.')
    out = {}
    t = DB.Transaction(doc, 'CT03 / Paredes, lajes, pisos e terreno'); t.Start()
    try:
        T = level(doc, 'Térreo'); I = level(doc, 'Inferior'); C = level(doc, 'Cobertura'); RN = level(doc, 'RN / Rua')
        # ---- paredes (eixo = linha de locação central)
        walls = []
        for i, w in enumerate(CASA['paredes']):
            wt = find_type(doc, DB.WallType, WALL_TYPES[(w['p'], w['esp'])])
            line = DB.Line.CreateBound(P(w['x1'], w['y1']), P(w['x2'], w['y2']))
            if w['p'] == 'T':
                base, top, lv = 0.00, 3.05, T          # da laje do térreo à face inferior da laje de forro (10 cm)
            else:
                base, top, lv = -3.00, -0.05, I        # da laje do inferior à face inferior da laje do térreo (15 cm)
            wall = DB.Wall.Create(doc, line, wt.Id, lv.Id, (top - base) * M, (base - lv.Elevation / M) * M, False, False)
            tag(wall, 'wall:%s:%02d' % (w['p'], i)); walls.append(wall)
        doc.Regenerate()
        # ---- lajes estruturais (estudo)
        stair_hole = rect(7.575, 9.575, 10.425, 12.425)
        def slab(ring, holes, typ, lv, top, code):
            ft = find_type(doc, DB.FloorType, typ)
            l = List[DB.CurveLoop](); l.Add(loop(ring))
            for h in holes: l.Add(loop(list(reversed(h))))
            f = DB.Floor.Create(doc, l, ft.Id, lv.Id)
            f.get_Parameter(DB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set((top - lv.Elevation / M) * M)
            tag(f, 'slab:' + code); return f
        slab(rect(1.425, 3.925, 10.575, 12.5), [stair_hole], 'VEBRAM LAJE / concreto 15 cm', T, 0.10, 'terreo_principal')
        slab(rect(1.425, 12.5, 10.575, 16.075), [], 'VEBRAM LAJE / concreto 15 cm', T, 0.10, 'terraco')
        slab(rect(1.4, 9.375, 10.6, 16.075), [], 'VEBRAM LAJE / concreto 10 cm', I, -2.90, 'inferior_sobre_solo')
        slab(rect(1.425, 3.925, 10.575, 12.575), [], 'VEBRAM LAJE / concreto 10 cm', C, 3.15, 'forro_cobertura')
        # ---- pisos acabados por ambiente (retângulo do eixo menos meia parede)
        half = {}
        for w in CASA['paredes']:
            key = (w['p'], 'y', w['y1']) if w['y1'] == w['y2'] else (w['p'], 'x', w['x1'])
            half[key] = max(half.get(key, 0), w['esp'] / 2)
        def inset(p, a):
            x1 = a['x1'] + half.get((p, 'x', a['x1']), 0); x2 = a['x2'] - half.get((p, 'x', a['x2']), 0)
            y1 = a['y1'] + half.get((p, 'y', a['y1']), 0); y2 = a['y2'] - half.get((p, 'y', a['y2']), 0)
            return x1, y1, x2, y2
        finishes = []
        for a in CASA['ambientes']:
            if a['p'] not in ('T', 'I') or a.get('escada'): continue
            x1, y1, x2, y2 = inset(a['p'], a)
            lv = T if a['p'] == 'T' else I; top = N['terreo'] if a['p'] == 'T' else N['inferior']
            if a['nome'] == 'Terraço': x1, x2, y2 = 1.425, 10.575, 16.075
            if 'box' in a:
                b = a['box']; bx2, by2 = b['x2'], b['y2']
                ring = [(x1, by2), (bx2, by2), (bx2, y1), (x2, y1), (x2, y2), (x1, y2)]
                ring = [(x1, y2), (x2, y2), (x2, y1), (bx2, y1), (bx2, by2), (x1, by2)]
                floor(doc, [ring], FINISH[a['piso']], lv, top, 'acabamento:' + a['nome'])
                floor(doc, [rect(x1, y1, bx2, by2)], b['piso'], lv, top - 0.01, 'box')
            else:
                floor(doc, [rect(x1, y1, x2, y2)], FINISH[a['piso']], lv, top, 'acabamento:' + a['nome'])
            finishes.append(a['nome'])
        # ---- áreas externas
        floor(doc, [rect(7.5, 0.0, 10.5, 3.925)], 'CALÇADA / concreto 8 cm', RN, 0.15, 'acesso_garagem')
        floor(doc, [rect(2.8, 0.0, 4.1, 3.925)], 'CALÇADA / concreto 8 cm', RN, 0.15, 'acesso_social')
        floor(doc, [rect(1.4, 16.075, 10.6, 19.0)], 'PISO EXTERNO / cimentado 5 cm', RN, -2.85, 'patio_inferior')
        floor(doc, [rect(0.0, -2.5, 15.0, 0.0)], 'CALÇADA / concreto 8 cm', RN, 0.0, 'passeio_paineiras')
        # ---- toposólido: terreno natural pela fórmula; aterro sob o térreo e sob os acessos; vazio no corte do inferior + pátio
        topo_type = next(x for x in DB.FilteredElementCollector(doc).OfClass(DB.ToposolidType))
        for x in DB.FilteredElementCollector(doc).OfClass(DB.ToposolidType):
            if 'Pradaria' in tname(x) or 'Grama' in tname(x): topo_type = x
        hole = rect(1.4, 9.375, 10.6, 19.0)
        prof = List[DB.CurveLoop](); prof.Add(loop(rect(0.0, 0.0, 15.0, 25.0))); prof.Add(loop(list(reversed(hole))))
        def inside(x, y, r, pad=0.0): return r[0] - pad <= x <= r[2] + pad and r[1] - pad <= y <= r[3] + pad
        fills = [((1.4, 3.9, 10.6, 9.375), -0.10), ((7.5, 0.0, 10.5, 3.925), 0.05), ((2.8, 0.0, 4.1, 3.925), 0.05)]
        pts = List[DB.XYZ]()
        n = 0
        for i in range(0, 31):
            for j in range(0, 51):
                x, y = i * 0.5, j * 0.5
                if 1.4 < x < 10.6 and 9.375 < y < 19.0: continue
                z = zterr(x, y)
                for r, zf in fills:
                    if inside(x, y, r): z = zf
                pts.Add(P(x, y, z)); n += 1
        # Tipo mais espesso: o terreno cai 4,7 m e o tipo de 1,2 m não comporta (erro 'muito fino').
        deep = find_type(doc, DB.ToposolidType, 'Pradaria CT03 / 8 m')
        if deep is None:
            deep = topo_type.Duplicate('Pradaria CT03 / 8 m'); cs = deep.GetCompoundStructure()
            core = cs.LayerCount - 1  # camada estrutural/variável inferior
            cs.SetLayerWidth(core, cs.GetLayerWidth(core) + 8.0 * M - cs.GetWidth()); deep.SetCompoundStructure(cs)
        topo_type = deep
        topo = DB.Toposolid.Create(doc, prof, pts, topo_type.Id, RN.Id)
        tag(topo, 'site:terreno')
        out.update({'paredes': len(walls), 'acabamentos': finishes, 'pontos_terreno': n, 'tipo_terreno': tname(topo_type)})
        t.Commit()
    finally:
        if t.GetStatus() == DB.TransactionStatus.Started: t.RollBack()
    return out
