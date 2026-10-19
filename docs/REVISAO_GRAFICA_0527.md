# VEBRAM 0.5.27 — revisão da geração gráfica

## Escopo e referência didática

Correções nas rotinas do plugin, não uma montagem manual das folhas. Os
comandos normais de Implantação, Situação, Planta, Memória e Organizar folhas
passam a usar as correções. `DrawingRefinement.Apply` apenas reúne essas
rotinas com as configurações já salvas para o ensaio integrado.

A demonstração ao lado da casa foi examinada para entender a distinção entre
terreno, laje de suporte, paredes e acabamentos com espessuras/desníveis
distintos. **Foi um exercício de aprendizado do usuário, não um padrão
construtivo, uma regra normativa ou um exemplo a copiar em outros projetos.**
As cotas/espessuras desse exercício não foram codificadas como valores de
modelagem. Nenhum projeto ou elemento físico foi corrigido para atender aos
testes. Novas modelagens dependem de critérios e normas aplicáveis ao caso.

## Correções de código

- **Memória:** usa o lote e a fonte confirmados na Implantação. Pisos fora ou
  atravessando o limite não são somados; a demonstração continua no modelo e
  fica oculta somente nessa vista. Lajes/radier identificados pelo tipo não
  entram na classe de acabamento. Pisos da mesma classe são unidos em projeção
  horizontal, evitando dupla contagem; sobreposição entre classes diferentes
  interrompe a operação para revisão, sem escolher silenciosamente uma classe.
- **Situação:** a identificação longa fica fora do lote; o texto central usa
  a largura efetiva do lote na escala da vista.
- **Notas:** os blocos de cadastro nos desenhos são compactados; fonte e
  condições completas permanecem no cadastro. As notas da Implantação são
  posicionadas abaixo dos limites reais das anotações já geradas.
- **Planta:** usa as caixas reais de texto e reserva os textos/linhas das
  cotas nativas, cujas curvas geralmente não são limitadas. As identificações
  tentam posições sem colisão e chamadas com desvio; o alvo usa a inserção da
  esquadria e a face da parede, não o centro da caixa que inclui o giro da porta.
- **Cortes:** oculta apenas na planta legal os elementos de símbolo
  `OST_Viewers` alheios aos cortes legais A/B. Ocultar `ViewSection` sozinho
  não remove seu símbolo; não foram apagados cortes de outros conjuntos.
- **Quadro urbanístico vazio:** só retira sua colocação da folha quando os
  quatro textos ainda coincidem exatamente com a referência vazia do V02.
  A vista original permanece no projeto. Referência editada ou viewport fixado
  é preservado. Ocultar o viewport não bastava para retirar seu desenho.
- **Folhas:** títulos curtos vêm do catálogo de entregáveis; a numeração
  considera também viewports ocultos, cujos números continuam reservados.
  Quadros de portas/janelas entram pelo comando existente, sem medidas inventadas.

## Evidência e proteção

Ensaios realizados em `Casa_Teste_01_SALVA_DIAGNOSTICO.rvt`, pelo pyRevit.
Primeiro `run_modify(dry_run=true)`, depois aplicação aprovada pelo usuário
com Keep no Revit. O código compara IDs e caixas geométricas dos elementos
físicos antes/depois e reverte todo o grupo se houver diferença.

`GraphicRegression.Run` inclui união de projeções, repetição do conjunto,
exclusão da demonstração da memória, referências de corte, proteção da
referência urbanística editada e conflito de número de viewport oculto.
Os testes usam transações revertidas; não salvam nem exportam RVT.

As suítes independentes continuaram passando: 2.932 verificações de
distribuição e 37 verificações aritméticas. Compilações com binlogs locais.
As 9 verificações novas de `GraphicRegression` passaram na cópia, incluindo
duas gerações completas e o cenário com viewport oculto. O relatório está
em `validation/grafica-0527/REGRESSAO_GRAFICA_0527.json`.

## Limites mantidos explícitos

- As classes de piso continuam dependendo do nome do tipo. Tipos não
  reconhecidos são relatados; não se deduz a função construtiva pela espessura.
- Esta memória mede projeções físicas de pisos; não substitui áreas de
  ambientes, área computável ou validação de permeabilidade municipal.
- Pisos inclinados/curvos e pisos que atravessam o lote exigem revisão; não
  se inventa um recorte ou uma classe para obter um número conveniente.
- Cotas ou rótulos sem espaço seguro continuam sendo informados como pendência.
- A documentação gerada ainda é um estudo fictício; os dados municipais do
  projeto continuam dependendo da conferência já indicada pelo usuário.
