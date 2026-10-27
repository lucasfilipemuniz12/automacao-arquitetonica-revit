# -*- coding: utf-8 -*-
"""Base comum da modelagem da Casa Teste 03 (CPython / pyRevit agent).
Coordenadas do casa.json: x oeste->leste, y frente->fundos. No Revit: X = x, Y = -y (norte para cima),
origem no canto da Rua das Paineiras com o lote 06 (igual ao DXF)."""
import json, os
from pyrevit.api import DB
from System.Collections.Generic import List

HERE = os.path.dirname(os.path.abspath(__file__))
CASA = json.load(open(os.path.join(HERE, '..', 'ficha', 'casa.json'), encoding='utf-8'))
M = 1 / 0.3048
PREFIX = 'CT03:'
N = CASA['niveis']

def P(x, y, z=0.0):
    return DB.XYZ(x * M, -y * M, z * M)

def zterr(x, y):
    return -0.18 * y - 0.025 * x + 0.003 * (x - 7.5) ** 2

def tag(e, value):
    p = e.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)
    if p and not p.IsReadOnly:
        p.Set(PREFIX + value)
    return e

def comment(e):
    p = e.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)
    return (p.AsString() or '') if p else ''

def tname(t):
    p = t.get_Parameter(DB.BuiltInParameter.SYMBOL_NAME_PARAM)
    return p.AsString() if p else ''

def find_type(doc, cls, name):
    for t in DB.FilteredElementCollector(doc).OfClass(cls):
        if tname(t) == name:
            return t
    return None

def level(doc, name):
    return next(l for l in DB.FilteredElementCollector(doc).OfClass(DB.Level) if l.Name == name)

def loop(pts2d, z=0.0):
    lp = DB.CurveLoop()
    for a, b in zip(pts2d, pts2d[1:] + pts2d[:1]):
        lp.Append(DB.Line.CreateBound(P(a[0], a[1], z), P(b[0], b[1], z)))
    return lp

def rect(x1, y1, x2, y2):
    # counter-clockwise in Revit coordinates (Y = -y)
    return [(x1, y2), (x2, y2), (x2, y1), (x1, y1)]

def loops(*rings):
    l = List[DB.CurveLoop]()
    for r in rings:
        l.Add(loop(r))
    return l

def existing(doc, prefix):
    return [e for e in DB.FilteredElementCollector(doc).WhereElementIsNotElementType() if comment(e).startswith(PREFIX + prefix)]

def guard(doc):
    if doc.IsFamilyDocument or doc.PathName.lower().endswith('.rte'):
        raise Exception('Documento ativo é template/família. Modelar só no Casa_Teste_03.rvt.')
    p = doc.PathName.lower().replace('/', '\\')
    if not (p.endswith('casa_teste_03.rvt') or p.endswith('casa_teste_03_esquina_declive\\projeto2.rvt')):
        raise Exception('Documento ativo não é Casa_Teste_03.rvt: ' + doc.PathName)
