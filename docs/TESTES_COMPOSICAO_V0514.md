# Composição das folhas — VEBRAM 0.5.14

Validada no Revit 2026.4 em 14/10/2026. Lucas solicitou corrigir o plugin: limpar guias, centralizar títulos/números, reduzir a importância e dimensão do quadro e aproveitar a LEG-02 mantendo as escalas.

## Resultado do produto

Preparar legal > Revisar composição é uma ação opcional e explícita para o template V02 A1 horizontal. Requer entregáveis preparados e colocados e folhas LEG-01 a LEG-03. Preparar outras vistas não reorganiza as folhas.

- LEG-01: planta legal em destaque; Situação e quadro urbanístico abaixo.
- LEG-02: cobertura e quatro fachadas, incorporando o conteúdo anterior da LEG-04.
- LEG-03: implantação e cortes A/B.
- Quadro urbanístico com 180 mm de largura, fonte de 2,5 mm e altura de linhas calculada conforme o texto.
- Títulos curtos e centralizados, números dentro das margens, guias do template ocultas.
- Referência vazia do quadro retirada da folha; sua vista e textos preservados.
- Apenas a folha complementar gerada que ficou vazia foi retirada. Carimbo reservado à direita.

A transferência de uma vista entre folhas recria seu viewport com o mesmo tipo; preserva a vista. O grupo de transações reverte a operação se não couber. Não reduz escalas automaticamente. Conteúdo manual adicional que impeça o encaixe provoca recusa, sem composição parcial.

## Evidências

Compilação 0.5.14: zero erros e zero avisos, com binlog único. DLL instalada no caminho e GUID fixos, com Revit fechado normalmente e verificação de liberação exclusiva.

Teste explícito composition014 em cópia da validação 0.5.13 contendo as quatro folhas originais. Arquivos locais em PluginRevit/teste-composicao-v0514/nativo-03:
- VALIDACAO_composition014_20261014_203347.rvt
- COMPOSICAO.json
- CONCLUIDO.txt

O relatório registra Version 0.5.14.0, 64 guias ocultas e uma folha vazia removida. Todos os indicadores passaram: Repeat, AllTitlesInsideMargins, NoViewportOverlaps, CompactTableTextNoOverlap, ScalesPreserved, PhysicalBoundsPreserved, OriginalViewsPreserved e RoofAndSouthOnLeg02. São onze vistas e o Q04 existente em três folhas. Planta, cobertura, implantação, cortes e fachadas mantêm 1:100; Situação 1:500; quadro 1:1.

As três folhas foram abertas e revisadas visualmente no ensaio nativo-02, com a mesma implementação de composição. O quadro foi ampliado para conferir legibilidade. O nativo-03 corrige somente a medição do teste de colisão de textos para Coord/Width/Height, evitando margens invisíveis da caixa genérica. A operação de produção não foi alterada entre esses ensaios. Nenhum posicionamento foi corrigido manualmente.

O primeiro ensaio recusou uma fileira de cortes larga demais e reverteu corretamente. O algoritmo passou a ajustar o intervalo entre vistas, mínimo de 6 mm, e mede os títulos após atribuir os números definitivos.

## Limites

Composição específica para V02 A1 horizontal; não é empacotamento genérico para todos os templates, formatos e edifícios. Dados urbanísticos da validação são fictícios. Dados técnicos e carimbo precisam de revisão antes da emissão. Cotagem arquitetônica completa, classificação explícita geral e exportação PDF continuam pendentes. Não representa aprovação municipal automática.

Somente fontes e documentação são publicados; modelos, binários e relatórios detalhados permanecem locais.

