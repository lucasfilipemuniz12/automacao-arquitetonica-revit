# Resultado CT03 R2 — correções do VEBRAM

Data da rodada: 26/10/2026, horário de São Paulo. Revit 2026.4.

Branch: `codex/ct03-casa-inteira-terreno`, criada de `main` em `9d8ac749454144c005e019c96b23e6407ea55cff` (PR #14). Código em `d066cdb`.

## Situação da verificação

A primeira rodada nativa concluiu na CT03 e no Sobrado, com folhas exportadas, zero avisos do Revit e geometria física preservada nos dois arquivos. A segunda rodada, com testes adicionais e o carimbo da CT03 preenchido, aguarda a autorização de carregamento do complemento no Revit. Os PNGs abaixo são da primeira rodada, cujo código já contém as três correções principais.

## O que mudou

1. A casa é encontrada pelos elementos conectados à laje escolhida, incluindo o inferior, o terraço, a escada e os guarda-corpos. O quadro da TO continua dependendo das lajes marcadas pelo usuário; ampliar o alcance do desenho não aumenta a TO automaticamente.
2. Cortes e fachadas usam trechos reais das faces superiores do toposólido. A referência da altura informa a menor cota do perfil daquela vista. Não se cria um terreno plano quando não há perfil disponível.
3. Organizar folhas permite escolher 1:200 apenas para a Implantação. A prévia calcula a distribuição nessa escala e Aplicar mantém as escalas dos demais desenhos.
4. Implantação e Situação passam a usar o nível da planta-fonte. A vista anterior fica preservada como reserva quando está em outro nível.
5. O campo Pavimentos aceita texto descritivo em uma linha de até 160 caracteres.

## Passo a passo — primeira rodada nativa

| Etapa | Resultado | Conferência |
|---|---|---|
| Edificação completa | Funcionou | Inventário inclui inferior, terraço, escada e guarda-corpos; limite inferior do recorte ≈ −3,81 m |
| Implantação a partir do Térreo | Funcionou | Não exigiu recriar manualmente as vistas do template |
| Afastamento dos fundos | Funcionou | Desenho mede aproximadamente 8,93 m até as faces externas, em vez dos 12,48 m da laje principal |
| TO com uma laje | Funcionou | 79,15 m², aproximadamente 21,11% de 375 m² |
| TO com duas lajes | Funcionou | União de 111,17 m², aproximadamente 29,65%; não soma a sobreposição duas vezes |
| Situação | Funcionou com aviso | Usa o Térreo; Rua dos Jacarandás permanece somente como confrontante textual |
| Quadro urbanístico | Funcionou com aviso | Limites fictícios confirmados: TO 50%, CA 1,0 e TP 20%; CA/TP de projeto ficam pendentes onde a área não foi informada |
| Plantas dos dois pavimentos | Funcionou com aviso | Mantém avisos de cotas pendentes em ambientes pequenos e limites sem par de paredes |
| Cobertura | Funcionou | Quatro águas reconhecidas a 30%; geometria preservada |
| Memória do Térreo | Funcionou | Interna 47,45 m²; externa 32,03 m²; garagem coberta 15,25 m² |
| Memória do Inferior | Funcionou | Interna 45,18 m², incluindo PISO BOX |
| Q04 | Funcionou | 12 ambientes com área, totais por pavimento e total geral |
| Quadros de esquadrias | Funcionou | Portas/janelas mantidas nos quadros nativos |
| Corte A | Funcionou | Suíte com PD 2,80 m medido no modelo; terraço identificado como DESCOBERTO; RN/Rua fora da casa |
| Corte B | Funcionou | Níveis negativos e terreno real; três cotas verticais entre faces |
| Quatro fachadas | Funcionou | Inferior e terreno inclinado aparecem; áreas efetivamente enterradas continuam encobertas pelo terreno |
| Organizar folhas | Funcionou | Três folhas utilizadas: LEG-01, LEG-02 e LEG-04; Implantação em 1:200 e desenhos restantes em suas escalas |
| Preservação e avisos | Funcionou | Geometria física sem alteração e zero avisos do Revit |

Os valores de PD vêm das faces reais do modelo entregue, e podem diferir do valor nominal da ficha devido às camadas das lajes/pisos. A modelagem não foi ajustada para o plugin passar.

## Folhas da CT03

### LEG-01 — Implantação, situação, cobertura e cortes

![LEG-01](revisao_codex_r2/CT03/FOLHA%20-%20Folha%20-%20LEG-01%20-%20Implanta%C3%A7%C3%A3o,%20situa%C3%A7%C3%A3o,%20cobertura%20e%20cortes.png)

### LEG-02 — Plantas legais e áreas

![LEG-02](revisao_codex_r2/CT03/FOLHA%20-%20Folha%20-%20LEG-02%20-%20Plantas%20legais%20e%20%C3%A1reas.png)

### LEG-04 — Fachadas

![LEG-04](revisao_codex_r2/CT03/FOLHA%20-%20Folha%20-%20LEG-04%20-%20Fachadas.png)

O organizador incorporou o conteúdo da LEG-03 na LEG-01. A numeração preservada deixa o conjunto com LEG-01, LEG-02 e LEG-04.

## Sobrado Teste 02

A origem foi a cópia já cadastrada `Sobrado_Teste_02_teste_claude_0527.rvt`, duplicada novamente antes da rodada. Foram gerados os mesmos módulos, os quatro PNGs de folhas (incluindo a reserva LEG-03) e seis PNGs de cortes/fachadas. A distribuição utilizou três folhas, preservou uma folha de reserva e terminou com zero avisos e geometria física preservada.

![Sobrado — cobertura, cortes e fachadas](revisao_codex_r2/Sobrado/FOLHA%20-%20Folha%20-%20LEG-04%20-%20Cobertura,%20cortes%20e%20fachadas.png)

## Compilação e testes

- Compilação final: sem erros e sem avisos, com registro binário em `logs/`.
- Arithmetic: 46 verificações aprovadas, incluindo conexão com pavimento inferior, terraço/guarda-corpo, declive, cotas negativas e casos de interseção da malha.
- SheetPacking: 2.932 verificações aprovadas.
- Instalador agora recusa instalação com qualquer processo Revit aberto.
- `PASSOS.json`, `ENVELOPE.json`, `FOLHAS.json` e `PRESERVACAO.json` acompanham os PNGs de cada modelo.

## O que continua estranho ou pendente

- Segunda rua da esquina e afastamento frontal secundário: não implementados nesta rodada.
- O carimbo da primeira rodada da CT03 está sem os dados cadastrais, pois partiu da cópia do modelo-base. A rodada final para preencher e conferir esses campos ainda está pendente de carregamento do complemento.
- Plantas pequenas continuam com algumas cotas pendentes para preservar a leitura dos rótulos.
- A memória gráfica ainda tem legenda/texto próximos ao desenho; mantida a regra anterior.
- O Q04 foi para a área principal porque não coube na lateral do carimbo.
- LEG-03 permanece como reserva no Sobrado; no conjunto da CT03 o conteúdo foi incorporado na LEG-01.
- Não foram testados o chanfro, blocos totalmente separados, vínculos e o modo opcional de recorte do terreno inteiro.

## Arquivos protegidos

Nenhum template foi aberto para modelagem nesta rodada. As origens tiveram os mesmos SHA-256 antes e depois dos testes:

- CT03 original: `227261B93AA7FBB287646D5B34E2167C8DBC888AB681C838457DF0C3A2AEE7CD`.
- Sobrado de referência: `BF5C294DB9F64C3522D8320FA9C50C2B2E6E5A798D248C908FE57D1F9DE8CAF0`.
