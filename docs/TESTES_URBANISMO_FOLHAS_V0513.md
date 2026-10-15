# Urbanismo e folhas — VEBRAM 0.5.13

Atualizado em 14/10/2026. Plugin instalado, teste nativo 0.5.13.0 concluído e quatro folhas abertas para revisão visual no Revit 2026.4.

## Comportamento entregue

O quadro completo recebe uma vista própria, `LEGAL - Quadro urbanístico / preencher — VEBRAM`, reutilizada nas próximas execuções. A referência vazia do V02, já colocada na faixa direita da LEG-01, conserva seu conteúdo e posição. Isso evita ampliar a tabela completa sobre o carimbo.

Q04 passa a ser reconhecido pelo nome normalizado do catálogo quando o parâmetro de etapa não está disponível na tabela. Colocações existentes continuam preservadas.

Organizar folhas permite criar a próxima folha LEG com o mesmo tipo de carimbo da folha selecionada. A criação e a colocação são agrupadas: se a vista não couber ou já estiver em outra folha, a nova folha é revertida também. Não reduz a escala automaticamente. A mensagem de falta de espaço informa escala, dimensões de vista/título e área disponível.

Lucas escolheu manter 1:100 e acrescentar folhas. No caso de teste, foram necessárias LEG-03 e LEG-04. Situação conserva 1:500 e o quadro de desenho conserva 1:1.

## Evidência nativa

O job `legal05` usa dados fictícios e cópia explícita. Não regenera fachadas nem Situação. A versão é lida da assembly carregada. Testes executados:

- Quadro: dados ausentes como NÃO INFORMADA/PENDENTE; repetição sem duplicação; edição própria protegida; conteúdo manual preservado.
- Lote 394,40 m²; projeção do contorno base 200,57 m²; área computável informada 195,00 m²; permeável informada 50,00 m².
- TO 50,85%, limite fictício 70%; CA 0,49, limite fictício 1,50; TP 12,68%, mínimo fictício 20%. TP corretamente fora do limite informado.
- Todos os 12 entregáveis de teste colocados, considerando o Q04 já existente. LEG-03/04 criadas sem alterar as posições anteriores.
- Repetição, escala preservada, ausência de novas colisões, reversão de colocação recusada e reversão de nova folha vazia.
- IDs de vistas e geometria física do edifício preservados. Carimbos novos são anotações e não são confundidos com elementos físicos do edifício.

Compilação final: zero erros e zero avisos, com binlog único. Os 19 testes existentes de aritmética/retângulos passaram na retomada; as classes de cálculo não foram modificadas.

## Revisão visual e limites

As quatro folhas foram abertas no Revit. Distribuição efetivamente testada:

| Folha | Conteúdo |
|---|---|
| LEG-01 | Quadro completo, Situação, Cortes A/B, Fachada Leste e Q04 existente |
| LEG-02 | Fachadas Norte e Oeste |
| LEG-03 | Implantação e Planta legal do nível 1 |
| LEG-04 | Cobertura e Fachada Sul |

LEG-03/04 conservam a faixa direita livre para o carimbo. Os títulos e vistas foram revisados na escala geral da folha. Esta é uma composição funcional para avaliação, não uma prancha final emitida.

Há uma sobreposição preexistente entre os limites do Q04 e a referência vazia do quadro urbanístico na LEG-01. O teste registra essa condição separadamente das novas colisões. Espaços-guia, nomes de folhas, referência vazia, campos de carimbo e acabamento gráfico ainda precisam de limpeza/revisão antes de emissão. Não foram apagados ou deslocados conteúdos manuais para mascarar essa pendência.

Os dados urbanísticos são fictícios; TO considera somente a projeção do piso-base selecionado. Áreas computáveis/permeáveis e limites precisam de critérios reais do projetista. Não há verificação municipal nem aprovação legal automática. Persistem as pendências anteriores de cotagem arquitetônica completa, classificação explícita de elementos, adaptação a outros templates e exportação PDF.

## Evidências locais

Na raiz local do trabalho, resultado final: `outputs/PluginRevit/teste-urbanismo-folhas-v0513/nativo-02/VALIDACAO_legal05_20261014_193612.rvt`.

Na mesma pasta: `QUADRO_URBANISTICO.json`, `COLOCACOES.json`, `TESTES_LEGAIS_05.json` e `CONCLUIDO.txt`. `AllDeliverablesPlaced`, `NoNewSheetOverlaps`, `NewSheetFailureRolledBack`, `ExistingPlacementsPreserved`, `PhysicalBoundsPreserved` e `OriginalViewsPreserved` são verdadeiros. A sobreposição antiga aparece em `PreExistingOverlapPairs`.

Somente fontes e documentação são publicados. RVT/RTE, famílias, materiais, DLLs e relatórios detalhados com geometria/IDs permanecem locais. PR mantida em rascunho, sem merge.
