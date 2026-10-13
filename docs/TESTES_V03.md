# Testes da versão 0.3

O produto compilou com zero erros e avisos. O primeiro carregador também compilou sem avisos. Na execução nativa, falhou antes de alterar o modelo porque OpenAndActivateDocument não é permitido nesse evento com outro projeto ativo.

O carregador foi corrigido para abrir a cópia como documento em segundo plano e salvar/fechar após os testes. Sua compilação teve zero erros e um aviso de limpeza de DLL antiga bloqueada pelo Revit; o aviso não foi ocultado.

Um registro consumido da primeira tentativa impediu a repetição inicial; esse registro foi arquivado. O fonte passou a usar nome único de registro consumido para futuras execuções.

A rotina corrigida aguarda confirmação no aviso de segurança do Revit. Não afirmar PASS nativo enquanto não existir TESTES_NATIVOS.json e CONCLUIDO.txt.

Cobertura planejada: geração/repetição de Planta legal, Cobertura e Q04 na Casa Terras; proteção de edição manual; estilo de cota escolhido; preservação de geometria e vistas; leitura de duas águas de 20% da casa básica.
