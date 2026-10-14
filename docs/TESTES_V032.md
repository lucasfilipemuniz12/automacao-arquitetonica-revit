# Rótulos e cotas internas — versão 0.3.2

Estado: compilação sem erros/avisos; testes nativos concluídos. Cópia RVT salva e versão instalada.

## Alterações

- Nome do ambiente lido de ROOM_NAME; evita duplicação do número acrescentado por Room.Name.
- Texto repartido em linhas; busca de posição com caixa de texto dentro do ambiente e afastada dos outros rótulos/cotas internas.
- Quando não encontra espaço legível, usa chamada externa com linha indicadora. O recorte inclui as anotações geradas.
- Cotas internas usam referências nativas às faces de acabamento de paredes ortogonais, em duas direções por ambiente quando disponíveis. Não usam valores inventados nem linhas auxiliares para essas cotas.
- Preferências: cotas internas ligadas por padrão; espessuras de paredes e larguras de esquadrias são opções separadas, desligadas por padrão.
- Famílias sem referências esquerda/direita válidas, paredes sem faces de acabamento reconhecidas e contornos não ortogonais são informados como pendências.
- A atualização é revertida quando a exclusão das marcas próprias afetaria elementos dependentes não registrados pelo gerador.
- Edição das linhas indicadoras dos rótulos passa a participar da proteção contra substituição automática.

## Validação prevista

Geração/repetição da mesma planta; cotas positivas entre referências de paredes; ausência de sobreposição entre rótulos internos gerados; proteção de edição; estilo selecionado; opções de espessura e largura; preservação dos IDs físicos e das vistas; leitura de cobertura e Q04.

Resultados: 16 cotas internas entre acabamentos e duas gerais, 32 elementos de anotação, cinco rótulos com chamadas externas. Não houve sobreposição entre rótulos internos gerados no teste. Opcionalmente foram criadas 27 cotas de espessura e 14 larguras de esquadrias; essas duas opções foram desligadas na cópia final para facilitar a revisão. Duas cotas horizontais ficaram pendentes nos ambientes 01/03. Repetição, edição protegida, estilo escolhido e preservação de IDs passaram. Evidências: TESTES_NATIVOS.json, ROTULOS_E_COTAS.json e COTAGEM_OPCIONAL.txt. Posições de esquadrias em cadeias de cotas, ambientes curvos/girados, disposição de todas as cotas e colisões das linhas indicadoras precisam de evolução.

Próximos módulos após revisão da planta: cortes A/B, fachadas nativas, situação, quadro urbanístico e folhas.


Revisão visual final pendente: o controle da janela retornou point is outside window bounds na abertura do RVT, apesar de o diálogo estar visível. O usuário foi solicitado a abrir o arquivo selecionado e maximizar o Revit. Não afirmar validação visual completa.
