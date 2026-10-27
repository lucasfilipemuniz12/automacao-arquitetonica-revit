# Ficha de modelagem — Casa Teste 03 / Residência Paineiras (CT03)

Para o ChatGPT/Codex modelar no Revit 2026. Fonte única dos números: `ficha/casa.json`.
Desenho de referência: `referencia/Casa_Teste_03.dxf` (metros; abre no AutoCAD e pode ser vinculado no Revit) e a prévia `referencia/previa.png`.

## Objetivo

Modelar uma casa pequena num **lote de esquina em declive** para estressar o plugin VEBRAM no projeto legal:

- duas frentes;
- terreno caindo para o fundo (≈ 18%) e para a rua lateral;
- um pavimento inferior só sob o fundo da casa;
- duas lajes no térreo (principal e terraço);
- confrontante direito que é uma rua.

Depois de modelada, rodar o projeto legal completo e anotar o que o plugin não resolve.

## Dados cadastrais (preencher em Cadastro do projeto, Carimbo e Quadro urbanístico)

| Campo | Valor |
|---|---|
| Projeto | Residência Paineiras |
| Código | CT03-2026 |
| Cliente | Helena Duarte Vieira e Tiago Moreira Vieira |
| Endereço | Rua das Paineiras, 245 — esquina com Rua dos Jacarandás |
| Bairro / cidade | Jardim Vale do Sol — Belo Horizonte/MG — CEP 30000-000 |
| Lote / quadra / inscrição | Lote 07 — Quadra 22 — 007.022.0245.001-4 |
| Zoneamento | ZR-2 — Zona Residencial 2 (perfil de teste) |
| Uso / intervenção | Residencial unifamiliar / Construção nova |
| Pavimentos | 2 (térreo e pavimento inferior em meio-nível sob o fundo) |
| Autor | Arq. Beatriz Nogueira Prado — CAU A000000-0 |
| Projetado / desenhado / verificado / aprovado | Beatriz N. Prado / Lucas F. Muniz / Silvana B. M. Heredia / Beatriz N. Prado |
| Emissão | 26/10/2026 |
| Confrontante esquerdo (oeste) | Lote 06 — Residência de Carlos Eduardo Lima e Ana Paula Lima, Rua das Paineiras, 235 |
| Confrontante direito (leste) | Rua dos Jacarandás (via pública, esquina) |
| Confrontante fundos (sul) | Lote 08 — terreno vago, Imobiliária Serra Verde Ltda. |

**Perfil municipal de teste** (criar no plugin como perfil pessoal): TO máx. 50%, CA máx. 1,0, TP mín. 20%, afastamento frontal 4,00 m (frente secundária da esquina 2,00 m), lateral 1,50 m, fundos 1,50 m, até 2 pavimentos.
Os valores são inventados para o teste. Tratar como reais durante o teste, mas não são a lei de BH.

## Lote, vias e terreno

- **Lote:** retangular, 15,00 × 25,00 m = 375,00 m².
- **Eixos:** x de oeste para leste (0 = divisa com o lote 06; 15 = alinhamento da Rua dos Jacarandás). y da frente para os fundos (0 = alinhamento da Rua das Paineiras; 25 = fundos).
- **Norte:** para a Rua das Paineiras.
- **ATENÇÃO às coordenadas:**
  - No DXF o norte está para cima. Por isso, **Y do DXF = −y do casa.json**: os fundos do lote ficam em Y = −25.
  - Ao modelar no Revit, colocar o lote em X 0…15 e Y 0…−25, ou vincular o DXF na origem e seguir o desenho. Nunca colocar a casa em Y positivo: ela cairia sobre a Rua das Paineiras.
- **Arquivo:** **criar um PROJETO NOVO** a partir do template V03 e salvar como `Casa_Teste_03.rvt`. **Nunca modelar nem salvar dentro do arquivo do template (.rte).**
- **DXF:** só **vincular** como referência (Inserir → Vincular CAD), em vista de planta, nunca como modelo final.
- **Rua das Paineiras:** leito 9,00 m, passeio 2,50 m, plana em 0,00 (RN = meio-fio no canto oeste do lote).
- **Rua dos Jacarandás:** leito 8,00 m, passeio 2,00 m; desce junto com o lote até ≈ −4,70 no fundo.
- **Terreno natural:** z(x,y) = −0,18·y − 0,025·x + 0,003·(x − 7,5)². Pontos a cada 5 m em `casa.json › terreno.pontos_5m` (de +0,17 na frente-oeste a −4,71 no fundo-leste).
- **Toposólido:** criar a partir desses pontos (ou de uma malha mais densa pela fórmula).
- **Movimento de terra:**
  - frente aterrada até +0,15 no acesso e na garagem;
  - corte para o pavimento inferior entre y 9,5 e 16;
  - pátio inferior em −2,85 entre y 16 e 19.
  - Usar escavação nativa do toposólido onde houver corte.

## Níveis

| Nível | Cota |
|---|---|
| RN / Rua | 0,00 (referência; pode ser só nível de referência) |
| Inferior | −2,85 (pé-direito 2,70) |
| Térreo | +0,15 (pé-direito 2,80; forro +2,95) |
| Cobertura (laje) | +3,15 |
| Telhado | 4 águas, 30%, beiral 0,60, sobre o corpo do térreo (x 1,5–10,5; y 4–12,5). Cumeeira resultante da inclinação. |

## Implantação e lajes

- **Corpo da casa:** x 1,50–10,50; y 4,00–16,00.
- **Afastamentos:** frente 4,00 · oeste 1,50 · Rua dos Jacarandás 4,50 · fundos 9,00.
- **Laje principal do térreo:** x 1,5–10,5; y 4–12,5 (76,5 m² em eixos). **Esta é a laje base da Implantação.**
- **Laje do terraço:** x 1,5–10,5; y 12,5–16 (31,5 m² em eixos), sobre o pavimento inferior.
  - Laje separada, de propósito, para testar a TO com uma e com duas lajes.
- **Sob o térreo, entre y 4 e 9,5:** solo. Não há ambiente no inferior.

## Paredes, ambientes, esquadrias e escada

Seguir exatamente `casa.json`:

- **paredes:** por eixo, com espessura; a parede y = 9,5 do inferior é de contenção, 25 cm;
- **ambientes:** áreas em eixos; o Revit medirá pelas faces e dará um pouco menos;
- **esquadrias:** 23 itens, com códigos P/PG/J/V, parede, posição, largura, altura e peitoril;
- **escada:** em U; 17 espelhos de 17,6 cm; pisos de 26 cm; lances de 1,40 m; patamar 0,90 m.

Tipos de piso — usar estes nomes, que o plugin lê para a memória gráfica:

- PISO INTERNO / porcelanato · cerâmica · madeira
- PISO EXTERNO / cimentado
- PISO EXTERNO / cerâmica antiderrapante (terraço)
- PISO BOX / cerâmica 1 cm (box do banho do inferior)
- CALÇADA / concreto (acessos)
- GRAMADO / grama esmeralda (jardim)

Ambientes Revit (Rooms) com estes nomes:

- **Térreo:** Estar / jantar, Garagem, Cozinha / serviço, Hall, Lavabo, Escada, Terraço.
- **Inferior:** Banho, Estar íntimo, Escada, Suíte, Quarto 02.

## Regras de modelagem (as mesmas do Sobrado)

- **Template e famílias:** template V03 (`ARQ_AUTOMACAO_V03.rte`). Famílias de sistema para paredes, pisos, lajes e telhado; esquadrias hospedadas. Sem DirectShape nem Modelo genérico no lugar de elementos de construção.
- **Comentários:** marcar as etapas com `CT03:`. Scripts de modelagem em `apoio/`.
- **Arquivos:** salvar como `Casa_Teste_03.rvt` nesta pasta, pela interface do Revit. Não salvar por cima de outros testes.
- **Conferência ao final:** sem avisos do Revit; esquadrias recortando as paredes; ambientes fechados; escada entre −2,85 e +0,15.

## Roteiro de teste do plugin (depois da modelagem)

1. **Cadastro + perfil municipal de teste** (tabela acima). Carimbo preenchido em todos os campos.
2. **Implantação.**
   - Fonte: planta do térreo; laje base: a principal.
   - Lote: 15 × 25 com canto em (0, 0); frente: Rua das Paineiras.
   - Rodar primeiro só com a laje principal. Depois marcar também a laje do terraço em "Taxa de ocupação — lajes adicionais".
   - Anotar as duas TOs.
3. **Situação e Quadro urbanístico.**
   - Confrontantes conforme a tabela; o direito é a Rua dos Jacarandás.
   - Conferir como o plugin mostra a segunda rua: **hoje ele representa uma via só, limitação esperada**.
4. **Plantas legais** do térreo e do inferior, **memória gráfica** de cada pavimento e **Q04** com totais por pavimento.
5. **Cortes A e B.**
   - Corte A longitudinal em x = 3,00, olhando para leste; Corte B transversal em y = 14,00.
   - Conferir níveis negativos, terreno e pé-direito dos dois pavimentos.
   - Testar também com a planta do inferior como fonte.
6. **Fachadas** (4) e **Cobertura** (telhado 4 águas 30% + terraço plano: caimento a definir na Implantação).
7. **Organizar folhas** e exportar PNG/PDF das folhas LEG.
8. **Variante de estresse (segunda rodada, opcional):** chanfro de 2,50 × 2,50 m no canto nordeste. O lote deixa de ser retangular. Registrar a mensagem do plugin.

## O que anotar como resultado

Para cada passo: funcionou / funcionou com aviso / falhou, com print. Principalmente:

- duas frentes;
- terreno inclinado nas fachadas e nos cortes (linha de terreno e cota "BASE");
- pavimento inferior com contorno diferente;
- TO com uma e duas lajes;
- terraço sobre área interna na memória;
- confrontante que é rua;
- lote com chanfro.
