# -*- coding: utf-8 -*-
"""Roteiro de teste do plugin VEBRAM (versão da main) na CÓPIA Casa_Teste_03_teste_plugin.rvt.
Cada passo devolve 'ok' / 'aviso' / 'falhou' com a mensagem do plugin. Nada do código do plugin é alterado."""
import clr, os, traceback
clr.AddReference(r"C:\Users\lucas\Desktop\ARQ TESTE\claude-pendencias-0527\current\Vebram.Revit.dll")
from Vebram.Revit import (ProjectRegistration, ProjectRegistrationStore, MunicipalProfile, MunicipalLibrary, StampData,
    ImplantationOptions, Implantation, ContextSelection, LegalContext, ModuleSelection, LegalModules,
    SectionSelection, LegalSections, FacadeSelection, LegalFacades, SheetPlanner)
from System import Int64, String
from System.Collections.Generic import List, Dictionary
from pyrevit.api import DB
from ct03_base import CASA, comment

C = CASA['cadastro']; L = CASA['lote']['confrontantes']; PM = CASA['perfil_municipal_teste']

def ids(doc):
    plans = {v.Name: v.Id.Value for v in DB.FilteredElementCollector(doc).OfClass(DB.ViewPlan) if not v.IsTemplate}
    floors = {comment(f): f.Id.Value for f in DB.FilteredElementCollector(doc).OfClass(DB.Floor)}
    return plans['PRE - Layout / Nível 2'], plans['PRE - Layout / Nível 1'], floors['CT03:slab:terreo_principal'], floors['CT03:slab:terraco'], floors['CT03:slab:inferior_sobre_solo']

def step(out, name, fn):
    try:
        r = fn()
        txt = r if isinstance(r, str) else (getattr(r, 'Report', None) or str(r))
        low = txt.lower()
        out[name] = ('aviso' if any(w in low for w in ['conferir', 'pendente', 'não ', 'atenção', 'a definir', 'sem classe']) else 'ok', txt[:900])
    except Exception as e:
        out[name] = ('falhou', str(e).split('\r\n   at ')[0][:600])

def options(doc, extras=None):
    src, _, slab, _, _ = ids(doc)
    ex = None
    if extras:
        ex = List[Int64](); [ex.Add(Int64(i)) for i in extras]
    return ImplantationOptions(Int64(src), Int64(slab), 0.0, -25.0, 15.0, 25.0, 100, 'Acima', True, 2.5, 9.0, 'Rua das Paineiras',
                               0.0, False, True, None, 'Modelo', None, ex)

def cadastro(doc, out):
    rules = Dictionary[String, String]()
    for k, v in [('to', '50'), ('ca', '1,0'), ('tp', '20'), ('frente', '4,0'), ('lateral', '1,5'), ('fundos', '1,5')]: rules[k] = v
    def prof():
        p = MunicipalProfile('', PM['titulo'], C['cidade'], C['uf'], C['zoneamento'], C['uso'], C['intervencao'], rules,
                             'Frente secundária (esquina): 2,0 m. Até 2 pavimentos.', PM['fonte'], '26/10/2026', 0)
        return MunicipalLibrary.Save(p)
    holder = {}
    step(out, '01 Perfil municipal de teste', lambda: (holder.__setitem__('p', prof()), 'perfil salvo: ' + holder['p'].Title)[1])
    def reg():
        d = ProjectRegistration(C['projeto'], C['codigo'], C['cliente'], C['endereco'] + ' / ' + C['bairro'] + ' / CEP ' + C['cep'],
                                C['lote'] + ' / inscrição ' + C['inscricao_cadastral'], C['cidade'], C['uf'], C['zoneamento'], C['uso'],
                                C['intervencao'], '2', C['observacao'] + ' Pavimentos: térreo + inferior em meio-nível.', holder.get('p'))
        ProjectRegistrationStore.Save(doc, d); r = ProjectRegistrationStore.Read(doc)
        return 'cadastro: ' + r.Name + ' | ' + r.Address + ' | ' + r.Lot + ' | zona ' + r.Zone
    step(out, '02 Cadastro do projeto', reg)
    def stamp():
        v = Dictionary[String, String]()
        for k, val in [('nome', C['projeto']), ('numero', C['codigo']), ('cliente', C['cliente']), ('endereco', C['endereco'] + ' — ' + C['bairro'] + ' — ' + C['cidade'] + '/' + C['uf']),
                       ('autor', C['autor'] + ' — ' + C['registro_profissional']), ('emissao', C['data_emissao']), ('projetado', C['projetado_por']),
                       ('desenhado', C['desenhado_por']), ('verificado', C['verificado_por']), ('aprovado', C['aprovado_por'])]: v[k] = val
        return StampData.Apply(doc, v)
    step(out, '03 Carimbo', stamp)

def implantacao(doc, out, extras, label):
    step(out, label + ' Implantação', lambda: Implantation.Apply(doc, Implantation.Plan(doc, options(doc, extras))).Report)
    src = ids(doc)[0]
    reg = ProjectRegistrationStore.Read(doc)
    rt = C['endereco'] + ' — ' + C['bairro'] + ' — ' + C['cidade'] + '/' + C['uf']
    step(out, label + ' Situação', lambda: LegalContext.Apply(doc, ContextSelection('situacao', Int64(src), C['lote'], L['esquerdo_oeste'], L['direito_leste'], L['fundos_sul'],
            None, None, None, None, None, True, True, rt)).Report)
    step(out, label + ' Quadro urbanístico', lambda: LegalContext.Apply(doc, ContextSelection('urbanistico', Int64(src), C['lote'], L['esquerdo_oeste'], L['direito_leste'], L['fundos_sul'],
            None, None, float(PM['taxa_ocupacao_max_pct']), float(PM['coeficiente_aproveitamento_max']), float(PM['taxa_permeabilidade_min_pct']), True, True, rt)).Report)

def desenhos(doc, out):
    srcT, srcI, slabT, _, slabI = ids(doc)
    step(out, '20 Planta legal térreo', lambda: LegalModules.Apply(doc, ModuleSelection('planta', Int64(srcT), Int64(slabT), True)).Report)
    step(out, '21 Planta legal inferior', lambda: LegalModules.Apply(doc, ModuleSelection('planta', Int64(srcI), Int64(slabI), True)).Report)
    step(out, '22 Cobertura', lambda: LegalModules.Apply(doc, ModuleSelection('cobertura', Int64(srcT), Int64(slabT), True)).Report)
    step(out, '23 Memória térreo', lambda: LegalModules.Apply(doc, ModuleSelection('memoria', Int64(srcT), Int64(0), True)).Report)
    step(out, '24 Memória inferior', lambda: LegalModules.Apply(doc, ModuleSelection('memoria', Int64(srcI), Int64(0), True)).Report)
    step(out, '25 Q04 áreas', lambda: LegalModules.Apply(doc, ModuleSelection('areas', Int64(srcT), Int64(0), True)).Report)
    step(out, '26 Quadros de esquadrias', lambda: LegalModules.Apply(doc, ModuleSelection('esquadrias', Int64(srcT), Int64(0), True)).Report)
    # Corte A longitudinal (ao longo de Y, posição em X ≈ x = 3,00 → (3,0−1,425)/9,15 ≈ 17%); Corte B transversal a 85% da laje do térreo (y ≈ 11,2), cortando também o inferior
    step(out, '27 Corte A', lambda: LegalSections.Apply(doc, SectionSelection('corte-a', Int64(srcT), Int64(slabT), False, 17.0, False, 15.0, True)).Report)
    step(out, '28 Corte B', lambda: LegalSections.Apply(doc, SectionSelection('corte-b', Int64(srcT), Int64(slabT), True, 85.0, False, 15.0, True)).Report)
    def fac():
        rs = LegalFacades.Apply(doc, FacadeSelection(Int64(srcT), Int64(slabT), 3.0, 0.0, True))
        return '\n'.join(r.Report for r in rs)
    step(out, '29 Fachadas', fac)
    def org():
        items = SheetPlanner.Items(doc); lst = List[Int64]()
        for i in items: lst.Add(i.ViewId)
        return SheetPlanner.Apply(doc, SheetPlanner.Preview(doc, 'preferencial', lst)).Report
    step(out, '30 Organizar folhas', org)
