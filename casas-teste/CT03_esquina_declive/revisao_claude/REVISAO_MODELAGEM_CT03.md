# Revisão da modelagem da Casa Teste 03 feita pelo ChatGPT — 26/10/2026

Somente leitura: nada foi alterado nem salvo. Imagens nesta pasta.

## Problema mais grave: a casa foi modelada DENTRO DO TEMPLATE

- **Onde está a casa:** não existe `Casa_Teste_03.rvt`. Tudo foi feito e salvo no próprio template `C:\Users\lucas\OneDrive\Área de Trabalho\ARQ TESTE\TEMPLATE_ARQUITETONICO\ARQ_AUTOMACAO_V03.rte`, salvo em 26/10 às 21:32.
- **O que mudou no template:** os níveis foram renomeados e movidos (Inferior −2,85, RN / Rua 0,00, Terreo +0,15, Cobertura laje +3,15). Também ficaram dentro dele um DWG importado, um terreno, paredes, pisos, portas, janelas e ambientes.
- **Consequência:** todo projeto novo criado a partir da V03 já nasceria com essa casa.
- **Backup provável da versão limpa:** `ARQ_AUTOMACAO_V03.0003.rte`, na mesma pasta, de 19/10 às 21:28. Há outros dois de 19/10 em `casas-teste\Sobrado_Teste_02\revisao\`. O OneDrive também guarda o histórico de versões. Não restaurei nada: aguardo decisão.

## Comparação com a ficha

| Item | Ficha (casa.json) | Encontrado | Situação |
|---|---|---|---|
| Arquivo | Casa_Teste_03.rvt na pasta da casa | modelado no template V03 | ERRADO |
| Posição | lote x 0–15, y 0–25 (y cresce para os fundos) | casa sobre a área da Rua das Paineiras, espelhada para fora do lote | ERRADO |
| Paredes | 16 paredes de alvenaria com espessuras de 10 a 25 cm | 16 paredes do tipo **Parede cortina** (vidro) | ERRADO |
| Pisos/lajes | duas lajes no térreo, inferior e pisos com os tipos nomeados | 3 pisos "Genérico 150 mm"; nenhum tipo PISO INTERNO, EXTERNO, BOX etc. | ERRADO |
| Terreno | toposólido em declive, pela fórmula/pontos; corte e aterro | 1 toposólido "Genérico – 1000 mm" **plano**, como uma caixa | ERRADO |
| Telhado | 4 águas, 30%, beiral 0,60 | nenhum | FALTA |
| Escada | em U, 17 espelhos, de −2,85 a +0,15 | nenhuma | FALTA |
| Guarda-corpos do terraço | 3 trechos, 1,10 m | nenhum | FALTA |
| Portas | 14 itens, entre portas, portão e vãos, com larguras de 0,70 a 2,60 | 14 portas, todas "0915 x 2134mm", de pé, soltas | ERRADO |
| Janelas | 9 com medidas próprias e peitoris | 9 janelas, todas "0406 x 0610mm" | ERRADO |
| Ambientes | 12 com os nomes da ficha | 12 criados, com áreas iguais às dos eixos; "Terraço" com área 0 (não fechado). Nomes sem acento e "Escada T"/"Escada I" | PARCIAL |
| Níveis | Inferior −2,85 / Térreo +0,15 / Cobertura +3,15 / RN 0,00 | criados, mas renomeando os níveis do template | PARCIAL |
| DWG | só referência | Casa_Teste_03.dxf **importado** (não vinculado) no modelo; aparece no 3D em planos soltos | ERRADO |
| DirectShape / Modelo genérico | proibidos | nenhum | OK |
| Comentários CT03: | sim | 42 elementos marcados | OK |
| Avisos do Revit | zero | 1 | conferir |

## O que fazer

1. **Template:** decidir se restaura a V03 a partir do backup `ARQ_AUTOMACAO_V03.0003.rte` de 19/10, ou do histórico do OneDrive. Antes de restaurar, guardar a versão atual com outro nome, só como registro.
2. **Remodelar a CT03** num projeto novo, criado a partir da V03 restaurada e salvo como `Casa_Teste_03.rvt`. Usar as famílias e os tipos do Sobrado: alvenaria, lajes e pisos nomeados, esquadrias VEBRAM. O terreno é toposólido pelos pontos.
3. **Sistema de coordenadas:** no DXF, o desenho tem o norte para cima, então y do casa.json = −Y do DXF. Na modelagem, usar y do casa.json com o fundo para o sul (y negativo no Revit), ou espelhar com cuidado. Vou deixar isso explícito na ficha.
