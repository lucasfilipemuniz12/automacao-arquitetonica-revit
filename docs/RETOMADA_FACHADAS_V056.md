# Retomada das fachadas — candidato 0.5.6

A validação nativa 0.5.5 foi finalmente executada: quatro elevações nativas, direções cardeais e repetição passaram; Situação produziu duas cotas e oito curvas com a categoria Lines ativa; limites físicos preservados.

Entretanto a revisão visual da fachada leste encontrou somente terreno e níveis, sem a casa. Portanto os testes de direção/contagem da 0.5.5 NÃO provam que a fachada está pronta. A categoria Paredes estava habilitada e o filtro de fase mostrava tudo. A disposição da situação ainda precisa de revisão visual.

Candidato 0.5.6 ajusta a profundidade local do CropBox: Min.Z = -profundidade e Max.Z = 0, em vez de conservar os limites herdados. Inclui GEOMETRIA_FACHADAS.json no teste, com origem/direção, transformação e limites do recorte, categorias, paredes presentes e caixas físicas projetadas. Compilado sem erros/avisos e instalado após fechamento das sessões e liberação exclusiva da DLL.

Teste correto do candidato está em teste-fachadas-v056-real/Casa_Terras_FACHADAS_SITUACAO_V056.rvt. A pasta teste-fachadas-v056 foi executada pela DLL 0.5.5 que ainda estava carregada num fechamento pendente; seu JSON registra 0.5.5 e não representa teste de 0.5.6. Nenhuma origem foi sobrescrita pelo job.

A reabertura da DLL nova voltou ao aviso Segurança — Complemento não certificado. A skill Computer Use impede operar esse aviso. A solicitação de carregamento está pendente com o usuário. O job da cópia 0.5.6 continua ativo para executar após o clique; não há teste aprovado ou imagem revisada desta versão ainda.

Próxima ação: após o carregamento, ler GEOMETRIA_FACHADAS.json e ERRO.txt/TESTES_FACHADAS_SITUACAO.json; abrir as quatro vistas e a Situação. Se a casa continuar ausente, comparar profundidade projetada dos elementos com o recorte e a origem do marcador antes de outra alteração. Não afirmar conclusão pela ausência de exceções ou por compilação.
