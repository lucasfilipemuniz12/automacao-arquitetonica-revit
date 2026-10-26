# Correção dos rótulos — 0.3.5

Testes nativos concluídos em 14/10/2026. Versão instalada: dist-v035/Vebram.Revit.Legal.V035.dll.

Abrir Casa_Terras_ROTULOS_CORRIGIDOS_V035.rvt e a vista LEGAL - Planta baixa / Nível 1. O projeto V032 que estava aberto conserva o resultado anterior até ser regenerado; prefira esta cópia para revisar.

Resultado: nove nomes/áreas internos, nenhuma chamada externa, sem sobreposição entre as caixas de texto visível geradas no teste. O algoritmo anterior incluía a margem vazia da bounding box do Revit; usa agora largura/altura do texto na escala da vista. Prioriza 2,5 mm e tenta 2 mm quando não encontra encaixe, com quebra de linha adaptativa. Posiciona rótulos antes das cotas.

Foram mantidas duas cotas gerais e 11 internas. Cinco cotas internas foram indicadas como pendências por conflito com os rótulos, além dos dois limites horizontais 01/03 já pendentes. Cotagem completa ainda exige evolução. Repetição, proteção de edição, estilo escolhido, Q04, cobertura e preservação de IDs passaram. Evidências locais: TESTES_NATIVOS.json, ROTULOS_E_COTAS.json e DIAGNOSTICO_ROTULOS.json.

A 0.3.3 e a 0.3.4 falharam no teste de sobreposição; suas alterações não foram salvas como entrega concluída. A revisão visual da cópia 0.3.5 ainda deve ser feita. Não afirmar aprovação da disposição de todas as cotas nem de folha final.
