from pyrevit.api import DB
from terrain import net
import math
def apply(doc):
    if doc.Title!='Casa_Teste_04':raise Exception('Somente CT04 em modelagem')
    source=doc.GetElement(DB.ElementId(372295)).RoofType
    slab=next(f for f in DB.FilteredElementCollector(doc).OfClass(DB.Floor) if 'LAJE' in f.Name)
    material=doc.GetElement(slab.GetTypeId()).GetCompoundStructure().GetLayers()[0].MaterialId
    tx=DB.Transaction(doc,'CT04: lajes impermeabilizadas nas cotas da ficha');tx.Start()
    typ=source.Duplicate('CT04 LAJE IMPERMEABILIZADA / concreto 15 cm')
    cs=typ.GetCompoundStructure();cs.SetLayers(net(DB.CompoundStructureLayer,[DB.CompoundStructureLayer(.15/.3048,DB.MaterialFunctionAssignment.Structure,material)]));typ.SetCompoundStructure(cs)
    for i,datum in [(372295,2.2),(372312,3.)]:
        r=doc.GetElement(DB.ElementId(i));r.ChangeTypeId(typ.Id);level=doc.GetElement(r.LevelId)
        r.get_Parameter(DB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).Set((datum-level.Elevation*.3048-.15*math.sqrt(1.0004))/.3048)
    doc.Regenerate();tx.Commit()
    return {'warnings':[w.GetDescriptionText() for w in doc.GetWarnings()]}
