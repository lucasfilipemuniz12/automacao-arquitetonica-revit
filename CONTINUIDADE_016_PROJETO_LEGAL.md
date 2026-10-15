# VEBRAM 0.5.16 — continuidade em 15/10/2026

Estado: EM REVISÃO. Não tratar como versão final de entrega.

## Pedido e limite
Concluir Projeto Legal e melhorar interface com guias ilustrados vinculados aos campos, preservando template V02, PRE/LEGAL/EX e escalas. Usuário autorizou autonomia e pediu parar perto de 10% restantes na janela de cinco horas. Não criar subagentes sem pedido.

## Implementado localmente
- LegalGuide.cs: nove guias vetoriais, pins, explicações por foco/mouse e numeração dos campos; planta e corte com desenhos próprios.
- PrepareWindow.cs: guias ao lado dos formulários; caimentos por água em RoofDrainageWindow.cs.
- RoofDrainageRules.cs / RoofDrainage.cs: proposta explícita por água, prioridade da geometria modelada, compartilhamento implantação/cobertura e indicação de caimento pendente quando não há dado confiável.
- ElevationAnnotations.cs: altura total, nomes dos ambientes cortados e PD por faces reais. Base do terreno identificada como cota pontual, sem inventar perfil natural.
- UrbanisticTable.cs: quatro colunas e comparação dos índices com os limites informados.
- Implantation: guarda para exclusões dependentes. Ordenação de águas por chave em RoofWater.cs.
- Review016Validation.cs e testes/GuidePreview: verificações nativas e renderização dos guias.

## Evidências
- teste-revisao-v0516/nativo-01/CONCLUIDO.txt: teste nativo realizado no Revit com instalação 0.5.16 anterior aos últimos refinamentos dos guias.
- RVT testado: teste-revisao-v0516/nativo-01/VALIDACAO_review016_20261015_073836.rvt. Fonte original 0.5.15 preservada.
- REVISAO_015.json e REVISAO_016.json na mesma pasta. Repetição, composição em três folhas e verificações de preservação passaram. Alturas e PD presentes; quadro com quatro colunas; propostas de caimento compartilhadas em transação revertida.
- 37 verificações aritméticas passaram durante esta implementação.
- teste-revisao-v0516/guias-02: nove PNGs dos guias atuais, 18 combinações de formulários com/sem ScrollViewer passaram; compilação sem erros/avisos.
- Última compilação integral sem erros/avisos. Binlogs em logs.

## Diferença entre instalação e fonte
current contém a DLL usada em nativo-01. A fonte e staging-v0516-checkpoint contêm os últimos guias de planta/corte, numeração dos campos e um endurecimento do teste nativo:
1. zero de inclinação é detectado sem confundir 10,0% com 0,0%;
2. teste falha explicitamente se não houver duas águas planas para validar compartilhamento.
Esse último endurecimento compilou, mas ainda não foi executado em nova rodada nativa. Não sobrescrever current com Revit aberto.

## Pendências concretas antes de entregar
1. Corte B: PD 2,70 m quebra o m isolado na suíte; textos de ambientes sobre portas/esquadrias. Corte A: garagem sobre hachura densa. Melhorar posicionamento/fundo sem alterar estilos compartilhados do template.
2. Texto BASE do terreno está sobre linhas do desenho; reposicionar para leitura.
3. Marcadores/eixos antigos ocupam implantação/cobertura/planta. Revisar visibilidade local, preservando vistas e marcadores de referência do template.
4. Rótulos A1/A2 são locais à lista de águas de cada vista; avaliar identidade visual consistente entre implantação, cobertura e diálogo. Verificar escopo da seleção de telhados da cobertura.
5. Testar a interface real no Revit com os guias atualizados, incluindo campos e rolagem. Teste WPF isolado não substitui essa inspeção.
6. Nova rodada nativa e revisão visual após correções. A aprovação dos testes de dados não aprova automaticamente o aspecto das folhas.
7. Segundo pavimento opcional ainda não implementado.
8. Publicação 0.5.16 no GitHub e pacote de entrega ainda pendentes. Não anunciar como publicado.

## Caminhos e publicação
Raiz do pacote: C:/Users/lucas/Documents/Codex/2026-10-13/files-pasted-by-the-user-continuidade/outputs/PluginRevit
SDK: C:/Users/lucas/Documents/Codex/2026-10-13/files-pasted-by-the-user-continuidade/work/sdk/dotnet/dotnet.exe
Revit: D:/Autodesk/Revit2026/Revit 2026/Revit.exe
Backup pré-alterações: ../../work/backup-v0515-20261015_005817 (resolver a partir da raiz do trabalho).
GitHub privado lucasfilipemuniz12/automacao-arquitetonica-revit; PR2 draft: https://github.com/lucasfilipemuniz12/automacao-arquitetonica-revit/pull/2
Base .15 branch codex/vebram-revisao-claude-v0515, commit c2248ce1343b69e49e17f7269f8e338df7567d3a, tree b9f16ab173834b87f01f8db8b3cfc52eaa5467d5. Não há checkout Git local. Publicar via ferramentas GitHub; preservar PR1/PR2/main. Anexar qualquer novo PR ao chat.

## Retomada
Inspecionar consumo e processos. Revit estava aberto sem projeto após teste. Para controle nativo, reler skill computer-use e usar @oai/sky via node_repl. Fechar normalmente e aguardar saída real antes de instalar; a inicialização levou cerca de cinco minutos nesta máquina. Não encerrar à força nem abrir instâncias adicionais enquanto inicia/fecha.
