# -*- coding: utf-8 -*-
"""Terreno v2: sem furo passante. Escavação até sob a laje do inferior e do pátio, plataforma de aterro sob a
frente do térreo e dos acessos, taludes 1:1 até o terreno natural (fórmula do casa.json) e terreno natural no resto."""
import math
from pyrevit.api import DB
from System.Collections.Generic import List
from ct03_base import *

# regiões (x1, y1, x2, y2) nas faces externas das paredes / bordas dos pisos
FRONT = (1.425, 3.925, 10.575, 9.375)    # sob a laje do térreo (bottom −0,05) -> −0,10
LOWER = (1.4, 9.375, 10.6, 16.075)       # sob a laje do inferior (bottom −3,00) -> −3,05
PATIO = (1.4, 16.075, 10.6, 19.0)        # sob o piso do pátio (bottom −2,90) -> −2,95
ACC = [(7.5, 0.0, 10.5, 3.925), (2.8, 0.0, 4.1, 3.925)]   # sob as calçadas (bottom +0,07) -> +0,06
PLAT = 0.0                               # topo do aterro junto às paredes do térreo (base das paredes = 0,00)

def inside(x, y, r): return r[0] <= x <= r[2] and r[1] <= y <= r[3]
def dist(x, y, r):
    dx = max(r[0] - x, 0, x - r[2]); dy = max(r[1] - y, 0, y - r[3]); return math.hypot(dx, dy)

def zfinal(x, y):
    if inside(x, y, LOWER): return -3.05
    if inside(x, y, PATIO): return -2.95
    if inside(x, y, FRONT): return -0.10
    for r in ACC:
        if inside(x, y, r): return 0.06
    z = zterr(x, y)
    # aterro: talude 1:1 descendo da plataforma (frente da casa e acessos) até o natural
    d = min([dist(x, y, FRONT)] + [dist(x, y, r) for r in ACC])
    z = max(z, PLAT - d)
    # corte: talude 1:1 subindo do pátio até o natural (as paredes do inferior seguram o resto)
    z = min(z, -2.85 + dist(x, y, PATIO)) if not inside(x, y, (LOWER[0] - 0.01, LOWER[1], LOWER[2] + 0.01, LOWER[3])) else z
    return z

def run(doc):
    guard(doc)
    RN = level(doc, 'RN / Rua')
    tt = find_type(doc, DB.ToposolidType, 'Pradaria CT03 / 8 m')
    seen = {}
    def add(x, y):
        if not (0 <= x <= 15 and 0 <= y <= 25): return
        k = (round(x, 3), round(y, 3))
        if k not in seen: seen[k] = zfinal(x, y)
    i = 0
    while i <= 60:
        j = 0
        while j <= 100:
            gx, gy = i * 0.25, j * 0.25
            # longe das bordas das regiões, para a triangulação não cruzar a borda das lajes
            if all(not (r[0] - 0.12 < gx < r[2] + 0.12 and r[1] - 0.12 < gy < r[3] + 0.12) or (r[0] + 0.12 < gx < r[2] - 0.12 and r[1] + 0.12 < gy < r[3] - 0.12) for r in [FRONT, LOWER, PATIO] + ACC):
                add(gx, gy)
            j += 1
        i += 1
    # bordas das regiões: um ponto logo dentro e outro logo fora, para o terreno encostar nas faces
    for r in [FRONT, LOWER, PATIO] + ACC:
        n = int(max(r[2] - r[0], r[3] - r[1]) / 0.05)
        for k in range(n + 1):
            for (x, y, nx, ny) in [(r[0] + (r[2] - r[0]) * k / n, r[1], 0, -1), (r[0] + (r[2] - r[0]) * k / n, r[3], 0, 1),
                                   (r[0], r[1] + (r[3] - r[1]) * k / n, -1, 0), (r[2], r[1] + (r[3] - r[1]) * k / n, 1, 0)]:
                add(x, y); add(x + nx * 0.02, y + ny * 0.02)  # no próprio limite vale a cota da região; a transição fica fora da laje
    t = DB.Transaction(doc, 'CT03 / Terreno v2 (corte, aterro e taludes)'); t.Start()
    try:
        for e in existing(doc, 'site:terreno'): doc.Delete(e.Id)
        prof = List[DB.CurveLoop](); prof.Add(loop(rect(0.0, 0.0, 15.0, 25.0)))
        pts = List[DB.XYZ]()
        for (x, y), z in seen.items(): pts.Add(P(x, y, z))
        topo = DB.Toposolid.Create(doc, prof, pts, tt.Id, RN.Id); tag(topo, 'site:terreno')
        t.Commit()
    finally:
        if t.GetStatus() == DB.TransactionStatus.Started: t.RollBack()
    return {'pontos': len(seen), 'avisos': [(w.GetDescriptionText(), [e.Value for e in w.GetFailingElements()]) for w in doc.GetWarnings()]}
