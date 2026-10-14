# Atualização 0.5.4 — faixa do Revit e fachadas

Instalada após Lucas salvar e fechar Revit. Caminho/GUID fixos preservados, DLL anterior copiada para backup. SHA-256 atual: 087A1667249D772F901030883C8CB222BFE494E0893440706EC2B5A81EE99AAA. Compilação: zero erros e zero avisos. Sem job ativo de inicialização.

Ícones originais desenhados em WPF, com traços verdes iguais aos cartões dos entregáveis: planta/check para Preparar legal; controles para Preferências; tabela para Parâmetros; folha/casa para Demonstração V02; documento/lupa para Diagnóstico. Os ícones usam DrawingImage congelada e são atribuídos a LargeImage/Image de cada PushButton; não dependem de arquivos externos.

Corrigido o erro View cannot be duplicated: a rotina agora usa CreateViewTemplate na fachada já configurada em vez de chamar Duplicate no modelo de vista. Fachadas ausentes são criadas como elevações nativas com marcadores independentes, reaproveitando um perfil gráfico disponível. Um TransactionGroup engloba a recuperação e a preparação; uma falha reverte ambas. Fachadas compartilhadas continuam sendo preservadas em Reserva quando criadas as independentes.

Execução e revisão visual no Revit ainda pendentes. Não afirmar que compilação equivale a validação nativa. Reabrir o Revit e usar Preparar legal > Fachadas na cópia de teste. A rotina não exige que o usuário apague vistas existentes.

Pergunta sobre Implantação: identificação atual de piscina/deck/grama/garagem usa palavras nos nomes de Floor/DirectShape e nomes dos tipos, selecionando o maior candidato sobre o lote. Cobertura versus abrigo usa um limiar de envelope de 36 m². São heurísticas; não há reconhecimento por IA. Evolução recomendada: função explícita do elemento e seleção manual quando ausente. Essa evolução ainda não foi implementada.
