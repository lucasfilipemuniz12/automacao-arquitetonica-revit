# Resultado R3 — parcial, pausado por limite de uso

29/10/2026. Branch nova `codex/legal-acabamento-r3`, criada da main `69674ae` após a PR #15. Não incorporar sem revisão do Claude.

O usuário pediu interrupção antes de restarem 5% da janela. A consulta indicou 8% restantes; este pacote é um ponto de retomada, não uma declaração de conclusão.

## Funcionou na primeira rodada nativa

CT03: 30 etapas executadas, modelagem física preservada e zero avisos nativos. Sobrado: 23 etapas, também preservado e zero avisos. JSONs e PNGs estão em `casas-teste/revisao_legal_r3/CT03` e `Sobrado`.

- Numeração CT03: LEG-01, LEG-02, LEG-03, LEG-04, todas com total /04. Sobrado: LEG-01 a LEG-03. Reservas fora do conjunto emitido.
- Folhas novas receberam os responsáveis preenchidos. LEG-04 da CT03 mostra Beatriz N. Prado, com verificação de Silvana.
- Títulos e legendas das memórias ficaram em coluna ao lado da geometria.
- Plantas usam nomes dos níveis; instâncias CAD ficam ocultas.
- Implantação e Situação representam Rua dos Jacarandás, leito 8,00 e passeio 2,00. Afastamento secundário medido nas faces do modelo: 4,40 m. Sem perfil correspondente com mínimo, consta “a verificar”. A modelagem não foi mudada para produzir 4,43 m.
- Terraço de cerca de 30,38 m² aparece em quadro de áreas descobertas, fora do total útil do Q04. Nenhuma mudança em `OccupationFootprint`.

Na primeira composição, Q04 útil foi para LEG-03 por falta de espaço; áreas descobertas ficaram na LEG-02. Nenhuma escala foi diminuída para forçar cabimento. Implantação ficou em 1:200.

## Código posterior ainda precisa da rodada final

Após inspecionar os PNGs: título “Planta do pavimento inferior”, terreno oculto apenas na memória inferior, nota do frontal secundário deslocada para o bloco de notas e título próprio do quadro de áreas descobertas. Incluídas conferências nativas de total/responsáveis/Q04 e repetição da prévia. Esses ajustes e a validação CT04 ainda precisam ser executados juntos na versão final.

Build inicial: zero erros/avisos; Arithmetic 46; SheetPacking 2932. Foram acrescentadas verificações do polígono CT04 e do agrupamento das áreas descobertas; último build do checkpoint passou com zero erros/avisos, Arithmetic 48 e SheetPacking 2933. A DLL mais recente não foi instalada nesta pausa.

## CT04 — base modelada e salva, teste legal pendente

Arquivo: `ARQ TESTE\casas-teste\Casa_Teste_04\Casa_Teste_04.rvt`. Projeto novo via `NewProjectDocument(ARQ_AUTOMACAO_V03.rte)`. O RTE não foi aberto para modelar nem salvo.

- Alvenaria do template; nenhuma parede cortina.
- Quinze ambientes reconhecidos. Áreas internas conferem com as dimensões da ficha. Escada nativa: 17 espelhos de 0,17647 m e pisos de 0,28 m.
- DXF vinculado, metros, origem preservada, eixos X/Y sem giro; y positivo é norte.
- Terreno com 329 pontos lidos das curvas, complementação do perímetro pela fórmula fornecida. Corte nativo sob a garagem, solo em -0,22 sob a laje da casa e -0,07 sob o quintal pavimentado. Volumes de solo adjacentes unidos; zero avisos nativos na conferência final.
- Muros, arrimo e gradis nas duas frentes. Telhado cerâmico de duas águas a 30%, beiral 0,60, oitões de alvenaria. Coberturas planas de concreto, 2% para frente.
- Polígono real: 383,70 m². Casa e garagem dentro do lote. A rampa indicada na ficha invade 0,18 m² do chanfro; preservada e registrada, sem ajuste para o plugin passar.

Pendências de modelagem: boxes sem dimensões, família do portão basculante PG1 e caixa d’água não localizadas, vegetação específica/muda e detalhes dos degraus de acesso. Ver `casas-teste/CT04_esquina_irregular/HIPOTESES_CT04.md`. Não interpretar zero avisos nativos como projeto arquitetônico completo.

O código recusa explicitamente lote de cinco lados antes da Implantação, explica a limitação e impede usar seu retângulo envolvente para índices. Essa recusa e os demais módulos precisam ser comprovados na CÓPIA CT04; não há PNGs LEG da CT04 neste pacote.

## Retomar

1. Conferir build e Arithmetic/SheetPacking finais.
2. Com Revit fechado, atualizar a DLL. Preparar cópias novas com `scripts/Validate-LegalR3.ps1`, incluindo CT04.
3. Rodar e revisar todos os PASSOS/PNGs, corrigindo somente o plugin. Não mudar os modelos para passar.
4. Completar este relatório e o ZIP com as folhas CT04, atualizar o PR e manter sem merge até revisão do Claude.

Os PNGs anexados são da primeira rodada R3; não representam os ajustes posteriores ainda não validados. A imagem CT04_MODELO_3D.png é da base modelada, não uma folha legal.
