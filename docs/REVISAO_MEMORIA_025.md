# VEBRAM 0.5.25 - revisão legal: memória, caimentos e conjunto de folhas

## O que muda
- Classificação no navegador: 02 PROJETO LEGAL / MEMORIA_AREAS. A memória preparada passa a integrar Organizar folhas, no grupo das plantas.
- Nova vista usa a escala das Preferências legais; uma memória legal já preparada conserva sua escala. O organizador continua sem reduzir escalas. Recorte inclui pisos e legenda; descrição tem largura de texto limitada.
- A soma mostra apenas áreas dos pisos classificados, sem adicionar o terreno por diferença. O saldo lote menos pisos é declarado separadamente como estimativa aritmética, sem união de contornos e sem equivalência a área permeável medida. Terreno verde é orientação visual.
- Legenda gerada recebe registro de fonte e de suas anotações. Texto/posição/região editados, removidos ou fixados exigem a opção explícita de refazer. Outra planta-fonte é bloqueada. Na primeira atualização de uma memória antiga, é preciso autorizar a substituição da legenda, pois não há registro para reconhecer edições anteriores.
- Dependências internas de regiões (esboços, planos e curvas próprios do preenchimento) acompanham a exclusão; dependências externas que não fazem parte da legenda revertem a transação. Pisos ocultados manualmente não são revelados em massa. Cores da vista gerada continuam sendo reaplicadas pelo gerador.

## Limites de interpretação
As classes ainda dependem do nome do tipo de piso. Garagem utiliza amostragem sob ambientes; exige conferência. Não calcula união de polígonos nem desconta sobreposições de pisos. Não converte cores em índices municipais. A soma não equivale automaticamente a área construída, computável ou permeável. A geometria da casa não é alterada.

## Validação
Build Release com 0 erros e 0 avisos, binlogs; suíte de distribuição com 2932 verificações. Teste nativo numa cópia da Casa Terras: repetição, classificação, presença no organizador, proteção de legenda editada, prévia/aplicação com escala preservada e geometria física preservada. Relatórios nativos publicados em validation/MEMORIA_025.json e validation/REVISAO_LEGAL_025.json. Na Casa Terras, o conjunto ocupa três folhas: LEG-01 Implantação/Situação/urbanístico; LEG-02 planta/memória/áreas e esquadrias laterais; LEG-03 cobertura, cortes e fachadas. Escalas preservadas. Foram geradas 17 identificações de esquadrias.

## Próximas revisões
Cotas internas de ambientes pequenos/irregulares permanecem pendência já aceita. Histórico de revisões, referências do conjunto, árvores, verificações geométricas dos limites e coluna de abertura dos quadros não foram implementados nesta etapa. Testar em dois ou três projetos reais antes de aprovar o fluxo geral.

## Revisão integrada
Os rótulos de caimento reservam os contornos de superfícies e aberturas, além de linhas desenhadas e objetos pequenos. Cotas também reservam espaço. A busca verifica o trajeto da chamada contra textos já colocados e reserva as chamadas anteriores para os próximos rótulos. A chamada termina fora do retângulo do texto. O job review025 regenera Implantação, Situação, Quadro urbanístico, Planta legal e Cobertura antes da memória e da distribuição. A proteção de edições é explicitamente substituída somente na cópia de teste. Cortes e fachadas existentes são preservados e redistribuídos. O guia ilustrado da memória foi aberto e conferido na interface.

O teste integrado executa duas gerações dos módulos de cadastro, verifica dois rótulos e um contorno pequeno, testa a proteção de legenda editada e compara a geometria física antes/depois. Os dados de cadastro e regras do exemplar são fictícios. Há esquadrias in-place sem dimensões no quadro: substituir por famílias carregadas, sem deduzir medidas. Resultados desta casa não substituem os testes em outros projetos.
