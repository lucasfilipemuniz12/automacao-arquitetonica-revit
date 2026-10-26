# Testes nativos — 0.3.1, concluídos em 13/10/2026

Produto e carregador compilados sem erros/avisos. A execução nativa terminou e salvou a cópia local Casa_Terras_LEGAL_V03.rvt; evidências locais: TESTES_NATIVOS.json, DIAGNOSTICO_TELHADO.json e CONCLUIDO.txt.

## Resultados executados

- Planta legal nível 1: mesma vista 350659 do V02, duas cotas gerais, 16 elementos de anotação.
- Cobertura: mesma vista 350855, uma superfície plana reconhecida, duas anotações (inclinação e nota).
- Q04: mesma tabela 351017; nove ambientes com área e nenhum ambiente sem área nessa amostra.
- Geração repetida: identidade da vista e quantidade de elementos estáveis nos três módulos.
- Edição de texto gerado: bloqueio de substituição automática confirmado; refazer explícito confirmado.
- Estilo de cota escolhido: aplicado às cotas geradas; preferências restauradas ao padrão de metros no piloto final.
- IDs dos elementos físicos e das vistas existentes preservados. Essa verificação não é comparação completa de geometria nem validação de todas as vistas possíveis.
- Casa básica: duas águas reconhecidas, direções opostas no eixo X. Inclinação por face 19,739556%, coincidente com o contorno nativo. A rotina antiga de criação da casa usou Math.Atan(.2) onde a API exigia a razão .2. Seu fonte foi corrigido para próximas criações; o telhado existente não foi alterado.

## Histórico das correções

Abertura da cópia passou a usar OpenDocumentFile em segundo plano; registro consumido passou a ter nome único; filtro Room passou a SpatialElement + OfType<Room>; documento de teste é fechado também em falha. A expectativa de 20% no teste foi substituída por comparação independente com get_SlopeAngle do contorno nativo, após consulta à documentação Autodesk.

Referência: https://blog.autodesk.io/footprintroofslopeangle-property-value-meanings-and-unit/

## Limites

A Planta legal ainda tem somente cotas gerais e textos de ambientes; não implementa cotagem interna completa nem tags de esquadrias. Não houve teste de fachadas/cortes/Situação/urbanismo/folhas, superfícies curvas, três/quatro águas ou geração completa em outra casa. A leitura de duas águas da casa básica não equivale a um teste completo do módulo nessa casa. Implantação mantém as evidências históricas do piloto anterior; suas alterações 0.3 por água ainda precisam de validação específica. Não houve aprovação municipal.

Revisão visual: formulário de preferências abre e exibe padrão metros/0,01 m, escala 100 e nota persistida. Planta legal aberta no Revit. Há rótulos sobrepostos nos ambientes pequenos; disposição e nomes repetidos ainda precisam de refinamento.
