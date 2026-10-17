# Conferência de interface — VEBRAM 0.5.22 — 17/10/2026

O teste foi executado por cliques no Revit 2026.4, em CASA_TERRAS_UI_V0522.rvt, cópia separada da entrega 0.5.21. Não foi usado o RVT do usuário como arquivo de trabalho.

## Versão e publicação
O manifesto encontrado apontava para claude-v0521-identificacoes/dll-compilada/Vebram.Revit.dll, com versão 0.5.21. Foi compilada a fonte 0.5.22 da pasta github-v0522 (commit original do Claude 433eac4595278b2abdc28c5596c7cb17c3c9d4fc), com zero erros e zero avisos e binlog. O manifesto passou a apontar para github-v0522/current/Vebram.Revit.dll, versão 0.5.22.0. A cópia do manifesto anterior está na raiz github-v0522.

O push por Git falhou por ausência de autenticação. A publicação foi feita pela conexão GitHub, preservando exatamente o conteúdo do commit original: árvore Git 94fa6fa32e32682e5cbba3591bdabc6546502068. Os identificadores dos commits local/remoto diferem; a árvore dos arquivos é a mesma. A branch local original foi preservada. Não foi alterada main nem mesclada PR.

## Testes observados
- Faixa Projeto legal: painéis e quatro botões principais abriram corretamente.
- Preparar legal: cartões e seleção da planta de origem abriram; os cartões inferiores requerem rolagem.
- Memória gráfica: o clique gerou a vista e o relatório, com cores e legenda visíveis. A geração usa as classes físicas calculadas pelo código, incluindo o saldo do lote declarado como gramado; não foi validada como decomposição geométrica sem sobreposição ou conformidade municipal.
- Notas: preenchida nota geral de teste e nota exclusiva da LEG-01; botão informou quatro folhas atualizadas. Ao reabrir, ambos os textos estavam persistidos.
- Carimbo: preenchidos nome do projeto e Projetado por com textos de teste; botão informou um campo do projeto e quatro campos em quatro folhas legais atualizados. Reabertura recuperou os dois valores; os outros campos permaneceram vazios.
- Todos os valores inseridos explicitam teste/cópia/não emitir. Dados do projeto são globais; responsáveis são por folha LEG. Não confundir a preservação dos parâmetros de folhas EX/PRE com a ausência de efeito de campos globais do projeto.

## Problemas encontrados
1. A vista da memória herda a classificação PRE/LAYOUT da planta de origem e aparece no grupo de pré-projeto do navegador, apesar do nome indicar Projeto Legal. Deve receber a classificação legal apropriada.
2. A memória gerada não aparece na lista de entregáveis do organizador de folhas. Falta integração com LegalCatalog/SheetPlanner e reconhecimento de que a vista está preparada.
3. A legenda diz Total dos pisos coloridos, mas o valor também inclui a parcela de terreno sem piso adicionada ao gramado. O título e a explicação devem refletir o conjunto de classes, sem apresentar esse saldo como piso modelado.
4. A memória foi gerada em 1:50, escala da planta de origem. Ao integrá-la ao organizador, conferir tamanho real com legenda e oferecer escala apropriada; não reduzir silenciosamente a escala de vistas já aprovadas.

Esses pontos são resultados da conferência, não correções implementadas nesta rodada. Não considerar o fluxo da memória gráfica completo. Ainda faltam testes de outros projetos reais, contornos/classes e os itens já listados na continuidade do Claude.

## Prévia e encerramento
A prévia foi calculada pelo botão da interface: três folhas utilizadas, nenhuma nova e uma existente como reserva; aplicação habilitada. A memória ficou fora da lista. A prévia foi cancelada após a conferência, sem aplicar redistribuição desnecessária. A cópia foi salva pelo comando Salvar do Revit.

Não foram reexecutados nesta rodada todos os modos de teste internos do Claude, nem testados outros projetos reais. Os testes desta rodada foram de compilação e operação da interface descritos acima. Nenhum fonte funcional do Claude foi alterado durante a conferência.
