# -*- coding: utf-8 -*-
"""Etapa 1: níveis e tipos (duplicados dos tipos VEBRAM do template; nenhum tipo original é alterado)."""
from pyrevit.api import DB
from ct03_base import *

def set_total_width(wt, target_m):
    cs = wt.GetCompoundStructure()
    core = cs.GetFirstCoreLayerIndex()
    extra = target_m * M - cs.GetWidth()
    cs.SetLayerWidth(core, cs.GetLayerWidth(core) + extra)
    wt.SetCompoundStructure(cs)

def dup(doc, cls, source, name):
    t = find_type(doc, cls, name)
    if t:
        return t, False
    src = find_type(doc, cls, source)
    if src is None:
        raise Exception('Tipo de origem ausente no template: ' + source)
    return src.Duplicate(name), True

def run(doc):
    guard(doc)
    out = {}
    t = DB.Transaction(doc, 'CT03 / Níveis e tipos'); t.Start()
    try:
        lv = sorted(DB.FilteredElementCollector(doc).OfClass(DB.Level), key=lambda l: l.Elevation)
        if [l.Name for l in lv][:3] == ['Nível 1', 'Nível 2', 'Nível 3']:
            # Nível 1 = Inferior, Nível 2 = Térreo, Nível 3 = Cobertura (as vistas do template continuam ligadas a eles)
            for l, (name, z) in zip(lv, [('Inferior', N['inferior']), ('Térreo', N['terreo']), ('Cobertura', N['cobertura_laje'])]):
                l.Elevation = z * M; l.Name = name
        if not any(l.Name == 'RN / Rua' for l in DB.FilteredElementCollector(doc).OfClass(DB.Level)):
            rn = DB.Level.Create(doc, 0.0); rn.Name = 'RN / Rua'
        out['niveis'] = [(l.Name, round(l.Elevation / M, 3)) for l in sorted(DB.FilteredElementCollector(doc).OfClass(DB.Level), key=lambda l: l.Elevation)]
        made = []
        w10, c = dup(doc, DB.WallType, 'VEBRAM PAR-15 / cerâmico revestido', 'VEBRAM PAR-10 / cerâmico revestido (CT03)')
        if c: set_total_width(w10, 0.10); made.append(tname(w10))
        w20, c = dup(doc, DB.WallType, 'Retenção - Concreto de 300 mm', 'VEBRAM PAR-20 / concreto parcialmente enterrada (CT03)')
        if c: set_total_width(w20, 0.20); made.append(tname(w20))
        w25, c = dup(doc, DB.WallType, 'Retenção - Concreto de 300 mm', 'VEBRAM CONTENÇÃO / concreto 25 cm (CT03)')
        if c: set_total_width(w25, 0.25); made.append(tname(w25))
        f1, c = dup(doc, DB.FloorType, 'PISO INTERNO / cerâmica e contrapiso 5 cm', 'PISO INTERNO / porcelanato e contrapiso 5 cm')
        if c: made.append(tname(f1))
        f2, c = dup(doc, DB.FloorType, 'PISO EXTERNO / cimentado 5 cm', 'PISO EXTERNO / cerâmica antiderrapante 5 cm')
        if c: made.append(tname(f2))
        out['tipos_criados'] = made
        out['larguras'] = {tname(x): round(x.Width / M, 3) for x in (w10, w20, w25)}
        t.Commit()
    finally:
        if t.GetStatus() == DB.TransactionStatus.Started: t.RollBack()
    return out
