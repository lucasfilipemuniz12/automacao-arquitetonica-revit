# Fachadas e situação — 0.5.11

## Evidência e escopo

Em 14/10/2026, a versão 0.5.9 passou no Revit 2026.4 e as quatro fachadas foram abertas e revisadas visualmente: casa, cobertura, esquadrias e terreno presentes; níveis curtos na lateral; nota abaixo; sem marcadores de corte atravessando o desenho. Muros do próprio modelo podem encobrir parte da casa e são preservados.

A versão 0.5.10 repetiu o teste nativo e ocultou os marcadores de elevação/corte na Situação. A revisão identificou linhas de separação de ambientes restantes. A versão 0.5.11 adiciona sua ocultação, exclusiva da vista Situação. Compilação sem erros/avisos; teste nativo 0.5.11.0 concluído, repetição/direções/linhas/limites físicos aprovados. Situação 0.5.11 revisada visualmente: diagrama limpo, duas cotas 29,00 e 13,60 m, área 394,40 m², confrontantes, via e norte. Contagem de paredes presentes: Norte 39, Sul 39, Leste 38, Oeste 38.

## Correção das fachadas

- ViewDirection aponta para o observador: Norte +Y, Sul -Y, Leste +X, Oeste -X.
- Rotação usa a posição física do marcador; origem da vista não substitui essa posição.
- Move o marcador e o elemento real do recorte. Mantém o plano próximo 0,1 m para dentro da posição do observador e define profundidade explícita.
- Persiste posição do marcador no estado do plugin para permitir nova execução quando a caixa do marcador não estiver disponível na planta fonte.
- Marcadores compartilhados sem vistas em folhas migram para marcadores independentes; vistas originais permanecem na reserva. Vistas em folhas ou com edições protegidas exigem revisão.
- Vistas ausentes podem ser recriadas. O preset gráfico é criado com CreateViewTemplate, sem Duplicate inválido.
- Níveis nativos indisponíveis recebem indicação textual lateral com aviso; este caso não foi necessário nas quatro fachadas da Casa Terras.

## Situação

Mantém a categoria de linhas visível e desenha lote retangular confirmado, duas cotas em metros, confrontantes, norte e uma borda de via quando informada. Oculta modelo físico, marcadores de corte/elevação e linhas de separação de ambientes na vista. Dados da demonstração são fictícios e explicitamente marcados.

É um diagrama esquemático. Não inclui automaticamente quadra, lotes vizinhos desenhados, mapa cadastral ou levantamento georreferenciado. Esses dados precisam ser fornecidos para uma representação completa exigida pelo município.

## Teste nativo

O modo interno facades055 permanece por compatibilidade; a versão real é obtida da assembly carregada. Verifica quatro elevações nativas com direções corretas, paredes presentes em cada vista, repetição sem duplicação, duas cotas e oito curvas de situação visíveis e preservação dos IDs/limites físicos do modelo. As contagens não substituem revisão visual.

As fachadas não recebem cotagem arquitetônica completa neste lote: zero cotas novas por fachada, uma nota própria e níveis nativos. Urbanismo e montagem de folhas ainda requerem validação própria; o Projeto legal inteiro não está declarado concluído.

Somente fontes e documentação são compartilhados no GitHub. RVT/RTE, famílias, materiais, DLLs e relatório detalhado de geometria permanecem locais.

