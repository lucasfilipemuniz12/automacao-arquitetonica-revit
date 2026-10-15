# Revisão do Claude — primeiro lote, VEBRAM 0.5.15

15/10/2026. Escopo aprovado: nomes de ambientes, estilos do template, recortes e reaproveitamento das folhas. A revisão recebida foi tratada como apontamentos para análise; suas instruções de implementação não foram executadas indiscriminadamente.

## Mudanças

- RoomNameText / ModelReviewExport: transferência usa ROOM_NAME, sem anexar Room.Number. Rótulos respeitam o nome armazenado; diagnóstico sinaliza sufixos suspeitos. Número legítimo não é apagado por heurística.
- RoomNameReview: prévia de restauração exige outro RVT como referência. Apenas nomes que correspondam exatamente ao original seguido do número repetido podem ser restaurados; nomes diferentes são preservados. Aplicação transacional recusa alterações feitas após a prévia. O teste utiliza essa rotina na cópia, com a origem da Casa Terras.
- LegalStyles centraliza seleção de textos/cotas. Prioriza ARQ A1 texto 2.5/ARQ cotas metros quando compatíveis, ou outros estilos ARQ existentes com tamanho e unidade adequados. Escolha explícita do usuário para cotas prevalece. Cria derivação somente quando falta estilo compatível; não altera o tipo original.
- BuildingFrame delimita a edificação e elementos próximos contidos na faixa de 2 m, excluindo terreno do envelope padrão. Cortes/fachadas recebem 1,5 m de margem lateral por lado e base 0,5 m abaixo da superfície medida. Sem superfície válida próxima, usa o nível fonte como referência. Incluir terreno inteiro é opção desligada por padrão nas Preferências legais.
- LegalSheets tenta a folha selecionada e depois as LEG existentes antes de criar complementar autorizada. Vista/tabela já colocada mantém sua posição, sem duplicar Q04 em cada novo clique.
- Revisar composição remove duplicatas legadas do Q04 somente quando marcadas como geradas, não fixadas, na posição padrão de colocação e com a instância original não gerada preservada na LEG-01. Conteúdo diferente/manualmente colocado é conservado. Folhas complementares só são retiradas quando ficam vazias.
- Níveis de fachada: quando a indicação precisa ser textual, oculta o datum nativo apenas naquela vista, evitando a duplicação vista durante a revisão.

## Validação

Compilação final sem erros/avisos, com binlog único. 28 testes de cálculos, retângulos e nomes passaram.

Modo nativo review015 em cópia do RVT gerado que continha sete folhas. O script Validate-Local.ps1 aceita NameReferenceProject opcional para conferir nomes na origem. Sem esse argumento, os nomes existentes não são alterados. Sem job explícito, nada é executado na inicialização.

A validação verifica: cópia de nome sem número; preservação de nomes não abrangidos pela restauração; estilos ARQ reutilizados; presença de paredes nas seis vistas; largura de recorte igual ao envelope local + 3 m; base de recorte; repetição; proteção contra edição; teste temporário com folha bloqueada para provar busca nas demais; colocação completa, composição e preservação dos limites físicos. Folhas e vistas são exportadas para revisão visual.

Resultados finais e caminho da cópia são registrados na atualização de estado. Relatórios detalhados e RVT ficam locais.

## Limites e próximas etapas

A restauração exige uma referência confiável; não é limpeza automática de qualquer nome que termine em número. O critério de vizinhança do recorte não classifica semanticamente piscina, deck ou jardim. Superfície de terreno complexa exige conferência; opção de terreno inteiro pode demandar mais folhas, preservando escala.

Composição continua específica para V02 A1 horizontal e explícita. Outros conteúdos manuais podem impedir reduzir a quantidade de folhas. Não há promessa geral de três folhas.

Caimento de coberturas por água, ampliação da cotagem de cortes/fachadas e reformulação do quadro urbanístico em quatro colunas ficam para outro lote. Há pendências de legibilidade/anotações que as imagens devem evidenciar. Dados urbanísticos são fictícios; carimbo e critérios municipais precisam de revisão antes da emissão.

Branch codex/vebram-revisao-claude-v0515, a partir de d7a3a12042e334b2d29e35f034eb4134c1a2b9ad. PR 1 e main preservadas; sem merge.


## Resultado final de 15/10/2026

Teste nativo 0.5.15.0 concluído em teste-revisao-v0515/nativo-04/VALIDACAO_review015_20261015_004202.rvt. Nove nomes restaurados por confronto com a origem; nenhuma suspeita restante no diagnóstico dessa cópia. Quatro duplicatas padrão do Q04 e quatro folhas complementares vazias retiradas, resultando em LEG-01 a LEG-03.

Envelope local longitudinal de 21,2165 m, contra terreno de 30,875 m. Cortes A/Norte/Sul com largura de recorte 24,2165 m; Corte B/Leste/Oeste com 16,20 m; todos em 1:100. Superfície medida em -0,10 m; base do recorte em -0,60 m. Repetição, proteção de edição, busca de folhas existentes e limites físicos passaram. Os nomes legítimos de referência, incluindo SUÍTE 1 e BWC 1, foram preservados.

As três folhas exportadas foram revisadas visualmente. A revisão não declara toda a documentação pronta: há textos/caimentos sobrepostos na Implantação e na Cobertura, marcadores a avaliar e cotagem ainda parcial. Esses pontos não foram escondidos para apresentar o teste como aprovação visual completa.
