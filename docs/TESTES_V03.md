# Testes da versão 0.3.1

Produto e carregador corrigidos compilados com zero erros e avisos. A validação nativa ainda está pendente; compilação não comprova execução.

Falhas identificadas e corrigidas nas tentativas anteriores:
- OpenAndActivateDocument não é permitido no evento usado com outro projeto ativo. A cópia é aberta em segundo plano.
- Nome de registro consumido repetido bloqueava a retomada. Agora usa identificação temporal única.
- Room não aceita filtro OfClass na API nativa. Os dois coletores passaram a SpatialElement + OfType<Room>.
- O carregador passou a reaproveitar a cópia inativa aberta e fechar o documento de teste em caso de falha.

A DLL do produto tem identidade própria 0.3.1, evitando que o Revit reutilize a versão anterior já carregada na sessão.

A rotina aguarda confirmação no aviso “VEBRAM Validar projeto legal 0.3.1”. Não afirmar PASS enquanto não houver TESTES_NATIVOS.json e CONCLUIDO.txt desta execução.

Cobertura dos testes: geração/repetição de Planta legal, Cobertura e Q04 na Casa Terras; proteção de edição manual; estilo de cota escolhido; preservação dos IDs físicos e das vistas; leitura de duas águas de 20% da casa básica. Esses testes não validam fachadas, cortes ou aprovação municipal.
