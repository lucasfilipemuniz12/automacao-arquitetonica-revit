# Casa Teste 03: sobrado em lote de esquina, irregular e em declive

Ficha para o ChatGPT modelar no Revit e para testar o plugin VEBRAM (projeto legal). Feita pelo Claude em 26/10/2026.

**Todos os dados abaixo foram inventados para teste**, mas devem ser tratados **como se fossem reais**: preencher todos os campos do plugin (carimbo, quadro urbanístico, confrontantes, situação) com eles, sem "fictício" ou "a preencher". Não protocolar.

Arquivos nesta pasta:
- `Casa_Teste_03_lote_esquina_declive.dxf`: lote, confrontantes, ruas, passeios, curvas de nível (com elevação real em cada polilinha), cotas do terreno, contorno da casa, ambientes, garagem, muro de arrimo, árvores, norte. Unidade: **metro**. Origem (0,0) = vértice A do lote.
- `01_implantacao_curvas.png`, `02_planta_terreo.png`, `03_planta_superior.png`: imagens de conferência.

## 1. Identificação

| Campo | Valor |
|---|---|
| Projeto | Residência Tavares (Casa Teste 03) |
| Código | CT03-ARQ-LEG |
| Tipologia | Residencial unifamiliar, sobrado, 2 pavimentos + garagem em meio-nível |
| Endereço | Rua das Quaresmeiras, 215, esquina com Rua Canela-de-Ema |
| Bairro / cidade | Vale das Encostas, Belo Horizonte, MG |
| CEP | 31.999-215 |
| Lote / quadra | Lote 06, Quadra 12 |
| Planta de parcelamento (CP) | CP 432-017-M |
| Índice cadastral (IPTU) | 432.017.006.0001-3 |
| Matrícula | 87.654, 6º Ofício de Registro de Imóveis |
| Zoneamento | OP-2 (inventado para teste) |
| Proprietários | Rafael Moreira Tavares e Juliana Castro Tavares |
| CPF (exemplo de teste) | 111.444.777-35 |
| Autor / responsável técnico | Arq. Helena Prado Siqueira, CAU A123456-7 |
| RRT | 14892301 |
| Escritório | VEBRAM Arquitetura |
| Data | 26/10/2026, revisão R00 |

## 2. Lote

Polígono de 5 lados, com chanfro na esquina. Coordenadas em metros (x para leste, y para norte) e cota do terreno natural em cada vértice:

| Vértice | x | y | Cota (m) | Lado seguinte | Comprimento |
|---|---|---|---|---|---|
| A | 0,00 | 0,00 | 100,10 | A–B frente, Rua das Quaresmeiras | 12,00 |
| B | 12,00 | 0,00 | 99,33 | B–C chanfro da esquina | 3,54 |
| C | 14,50 | 2,50 | 99,64 | C–D lateral, Rua Canela-de-Ema | 23,51 |
| D | 15,20 | 26,00 | 103,38 | D–E fundos, Lote 07 | 16,27 |
| E | -1,00 | 24,50 | 104,11 | E–A lateral, Lote 05 | 24,52 |

- **Área do lote: 383,70 m²** (calculada do polígono).
- Ponto mais baixo: B (99,33), na esquina. Ponto mais alto: E (104,11), nos fundos à esquerda. **Desnível de 4,78 m.**
- Terreno natural (para gerar o sólido topográfico): **z = 100,00 + 0,16·y − 0,06·x + 0,002·(x − 7)²**. As curvas do DXF saem dessa fórmula (a cada 0,50 m; camada CURVAS_1M nas cotas inteiras).

## 3. Ruas e confrontantes

| Lado | Confrontante |
|---|---|
| Frente (sul) | Rua das Quaresmeiras: 11,00 m de largura (passeio 2,00 + pista 7,00 + passeio 2,00), desce cerca de 6% de oeste para leste |
| Lateral direita (leste) | Rua Canela-de-Ema: cerca de 11,70 m (passeio 2,00 + pista 7,00 + passeio 2,70), sobe cerca de 16% para o norte |
| Lateral esquerda (oeste) | Lote 05, Qd 12: Rua das Quaresmeiras, 205. Casa térrea. Proprietário: Antônio Carlos Ribeiro Neves |
| Fundos (norte) | Lote 07, Qd 12: Rua Canela-de-Ema, 88. Sobrado. Proprietária: Marlene Duarte Fonseca |
| Do outro lado da Rua das Quaresmeiras | Quadra 11 (residências) |
| Do outro lado da Rua Canela-de-Ema | Quadra 13, praça municipal |

Meio-fio da Rua das Quaresmeiras em y = −2,00. Meio-fio da Rua Canela-de-Ema acompanha a divisa, cerca de 2,00 m para fora.

## 4. Níveis

| Nível | Relativo | Absoluto |
|---|---|---|
| Garagem (piso) | −0,60 | 99,80 |
| Térreo (piso acabado) | ±0,00 | 100,40 |
| Superior | +3,00 | 103,40 |
| Cobertura (respaldo) | +6,00 | 106,40 |
| Quintal dos fundos (acima do arrimo) | +1,00 | 101,40 |

Usar o critério de camadas da Silvana (laje 15 cm, piso acabado 7 cm, paredes com base +0,01). Debaixo da casa o terreno vai de 99,92 (canto sudeste, aterro de ~0,5 m) a 101,73 (canto noroeste, corte de ~1,3 m). **Muro de arrimo** em y = 13,00, de x = 0,90 a 10,00, com altura de 1,00 m.

## 5. Edificação

### Térreo: contorno 8,00 × 8,00 m, de x 1,50 a 9,50 e y 3,00 a 11,00 (64,00 m²)
Paredes externas de 15 cm e internas de 10 cm. Ambientes (medidas internas):

| Ambiente | x | y | Área |
|---|---|---|---|
| Entrada / hall | 1,65–2,75 | 3,15–6,27 | 3,43 |
| Estar / jantar | 2,85–9,35 | 3,15–7,00 | 25,03 |
| Circulação | 2,85–5,50 | 7,10–9,35 | 5,96 |
| Cozinha | 5,60–9,35 | 7,10–10,85 | 14,06 |
| Lavabo | 2,85–4,25 | 9,45–10,85 | 1,96 |
| Despensa | 4,35–5,50 | 9,45–10,85 | 1,61 |
| Escada (reta, 17 espelhos de 17,6 cm, piso 28 cm, sobe para o norte) | 1,65–2,75 | 6,37–10,85 | 4,93 |

### Superior: contorno 8,00 × 6,50 m, de x 1,50 a 9,50 e y 4,50 a 11,00 (52,00 m²)
A faixa de 1,50 m da frente do térreo fica sem pavimento em cima (laje plana impermeabilizada sobre o estar, com caimento de 2% para a frente).

| Ambiente | x | y | Área |
|---|---|---|---|
| Suíte | 5,30–9,35 | 4,65–8,20 | 14,38 |
| Quarto 02 | 2,85–5,20 | 4,65–8,20 | 8,34 |
| Hall | 2,85–5,20 | 8,30–10,85 | 5,99 |
| Banho social | 5,30–7,20 | 8,30–10,85 | 4,84 |
| Banho suíte | 7,30–9,35 | 8,30–10,85 | 5,23 |
| Rouparia | 1,65–2,75 | 4,65–6,27 | 1,78 |
| Escada | 1,65–2,75 | 6,37–10,85 | 4,93 |

### Garagem coberta (bloco separado, meio-nível): x 9,60–12,60, y 3,00–8,50 (16,50 m²)
- Piso −0,60, laje de cobertura com topo +2,20, sem paredes na frente (aberta) e portão basculante PG1 na frente.
- Ligação com o estar por uma porta na parede leste da casa (x = 9,50, y 3,60–4,40), com 3 degraus de 20 cm.
- **Laje própria, separada da laje do térreo**: serve para testar a escolha de lajes da taxa de ocupação.

### Pisos externos (usar exatamente estes nomes de tipo de piso)
| Piso | Tipo | x | y | Área |
|---|---|---|---|---|
| Rampa da garagem | CALÇADA | 9,60–12,60 | 0,00–3,00 | 9,00 |
| Caminho da entrada (com 2 degraus) | CALÇADA | 3,00–4,20 | 0,00–3,00 | 3,60 |
| Quintal pavimentado dos fundos | EXTERNO | 1,50–9,50 | 11,00–13,00 | 16,00 |
| Resto do lote | gramado (sólido topográfico) | | | 274,60 |

Pisos internos: tipo INTERNO. Boxes dos dois banhos: tipo PISO BOX.

### Esquadrias
| ID | Ambiente | Tipo | L × H | Peitoril | Fachada |
|---|---|---|---|---|---|
| P01 | Entrada | abrir | 0,90 × 2,10 | – | sul |
| P02 | Cozinha → quintal | abrir | 0,80 × 2,10 | – | norte |
| P03 | Lavabo | abrir | 0,70 × 2,10 | – | interna |
| P04 | Despensa | abrir | 0,70 × 2,10 | – | interna |
| P05 | Estar → garagem | abrir | 0,80 × 2,10 | – | leste |
| P06 / P07 | Suíte / Quarto 02 | abrir | 0,80 × 2,10 | – | interna |
| P08 / P09 | Banho social / Banho suíte | abrir | 0,70 × 2,10 | – | interna |
| PG1 | Garagem | basculante | 2,80 × 2,20 | – | sul |
| J01 | Estar (2 un.) | correr | 2,00 × 1,20 | 1,00 | sul |
| J03 | Cozinha | correr | 1,50 × 1,00 | 1,10 | leste |
| J04 | Lavabo | maxim-ar | 0,60 × 0,60 | 1,60 | norte |
| J05 | Escada | maxim-ar | 0,60 × 1,50 | 1,20 | oeste |
| J06 | Suíte | correr | 2,00 × 1,20 | 1,00 | sul |
| J07 | Suíte | correr | 1,20 × 1,20 | 1,00 | leste |
| J08 | Quarto 02 | correr | 1,50 × 1,20 | 1,00 | sul |
| J09 / J10 | Banhos | maxim-ar | 0,60 × 0,60 | 1,60 | norte |

As janelas da fachada oeste ficam a 1,62 m da divisa com o Lote 05.

### Cobertura
- Casa: telhado cerâmico de 2 águas, 30%, cumeeira no sentido leste–oeste em y = 7,75, beiral de 0,60 m (no lado oeste, o beiral fica a 1,02 m da divisa).
- Caixa d'água de 1.000 L sob o telhado, sobre o banho social.
- Garagem: laje impermeabilizada, 2% para a frente.
- Laje sobre a faixa da frente do estar: 2% para a frente.

### Muros e árvores
- Muro de divisa de 1,80 m com o Lote 05 e com o Lote 07. Gradil de 1,80 m nas duas frentes, com muro baixo de 0,40 m.
- AR1: ipê-amarelo (*Handroanthus albus*), 6 m de altura, em (4; 18), **manter**.
- AR2: jabuticabeira (*Plinia cauliflora*), 4 m de altura, em (11; 20), **transplantar** para (12,5; 22).
- Plantar 1 muda de pata-de-vaca (*Bauhinia forficata*) no passeio da Rua Canela-de-Ema.

## 6. Parâmetros urbanísticos (perfil de teste)

Usar o perfil "Belo Horizonte (teste)" já cadastrado no plugin. Os valores dele vieram do e-book da PBH de 2020 (Anexo XII da Lei 11.181/2019), mas **o zoneamento não foi confirmado**: servem para testar o plugin, não para aprovar. Preencher "OP-2" no zoneamento do projeto e do perfil (os dois precisam coincidir).

Conta prévia, para conferir o que o plugin calcula:
- Projeção da casa: 64,00 m². Com a garagem: 80,50 m².
- Área construída: 64,00 + 52,00 + 16,50 = 132,50 m².
- Área permeável (gramado): 274,60 m², 71,6% do lote.
- TO: 16,7% só com a casa ou 21,0% com a garagem.

## 7. O que este teste quer estressar no plugin

1. **Lote não retangular** (5 lados, com chanfro). Hoje o plugin só aceita lote retangular: anotar o que quebra.
2. **Esquina:** duas frentes. Afastamentos diferentes em cada lado (frente 3,00; lateral para a rua 1,91 na garagem e 5,01 na casa; chanfro 1,70; lateral para o Lote 05, 1,62; fundos 13,67).
3. **Declive de 4,78 m:** terreno a partir das curvas, corte e aterro sob a casa, arrimo, fachadas com o terreno inclinado.
4. **Meio-nível:** garagem a −0,60 com laje própria (contorno com mais de uma laje e a taxa de ocupação).
5. **Superior menor que o térreo:** memória por pavimento e totais por pavimento.
6. **Todos os campos preenchidos:** carimbo, proprietários, RT, cadastro, confrontantes, árvores (manter/transplantar/plantar).

## 8. Para o ChatGPT
- Criar o projeto no template `TEMPLATE_ARQUITETONICO\ARQ_AUTOMACAO_V02.rte`. Salvar em `ARQ TESTE\casas-teste\Casa_Teste_03\Casa_Teste_03.rvt`.
- Importar o DXF em metros, com a origem do DXF na origem do projeto. Norte do projeto = eixo y.
- Gerar o sólido topográfico a partir das curvas importadas (ou pontos da fórmula da seção 2).
- Não inventar medidas: se faltar alguma, anotar como hipótese num arquivo `HIPOTESES_CT03.md`.
- Depois de modelar, rodar o VEBRAM (versão da `main`) e anotar tudo que falhar ou sair estranho.
