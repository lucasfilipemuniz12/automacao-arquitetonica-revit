# Hipóteses e ajustes da modelagem CT03 (Claude, 26/10/2026)

O que **não estava** na ficha, ou estava inconsistente, e foi decidido para modelar.

## Erros da própria ficha, corrigidos no modelo

1. **Escada.** A ficha pedia pisos de 26 cm e patamar de 0,90 m. Isso dá 2,98 m, mas a caixa tem só 2,85 m entre faces.
   - Modelado com 17 espelhos de 17,65 cm, **pisos de 25 cm** e patamar de 0,85 m: lance 1 com 8 espelhos, lance 2 com 9.
   - Tipo de escada `VEBRAM / Escada CT03 (piso 25 cm)`, com piso mínimo de 25 cm.
2. **V11 (escada → estar íntimo).** Na posição da ficha (y 10,0–11,0) ficava sob o lance 2, sem altura livre. Foi movido para **y 9,65–10,65**, onde a altura livre sob o lance passa de 2,40 m.
3. **P02 (garagem → escada).** Abre para a caixa da escada, que no térreo é vazio por cima dos lances. Mantido como na ficha. **Revisar**: mover P02 ou criar um patamar de chegada no térreo.

## Tipos criados por duplicação (os tipos do template não foram alterados)

- **Paredes:**
  - `VEBRAM PAR-10 / cerâmico revestido (CT03)` — 10 cm, mesma composição da PAR-15 com o núcleo reduzido.
  - `VEBRAM PAR-20 / concreto parcialmente enterrada (CT03)` — 20 cm, função Exterior.
  - `VEBRAM CONTENÇÃO / concreto 25 cm (CT03)` — 25 cm, função Contenção.
  - As duas últimas vêm do tipo "Retenção", que no template **não delimita ambiente**; a delimitação foi ligada nas três paredes.
- **Pisos:**
  - `PISO INTERNO / porcelanato e contrapiso 5 cm`, cópia da cerâmica 5 cm.
  - `PISO EXTERNO / cerâmica antiderrapante 5 cm`, cópia do cimentado 5 cm.
  - Mesma espessura; só o nome e a classe mudam.
- **Terreno:** `Pradaria CT03 / 8 m`. O tipo de 1,2 m não comporta 4,7 m de desnível: o Revit recusa por "muito fino".
- **Esquadrias:**
  - `VEBRAM Madeira / 90 x 210`.
  - `VEBRAM Portão metálico / 260 x 220` e `VEBRAM Porta de correr / 200 x 210 · 160 x 210`, da família de correr metálica modular. O portão da ficha é **basculante**; foi usada a família de correr disponível.
  - Janelas de correr nos tamanhos da ficha (família VEBRAM 4 folhas).
  - Maxim-ar/basculantes J03, J04 e J14 com a família VEBRAM 2 módulos, no tamanho certo; o tipo de abertura não é representado.
- **Marcas de tipo:** únicas para cada tipo novo (CT03-…), para não gerar aviso de marca duplicada.

## Alturas adotadas (construtivas, para estudo)

- **Lajes:**
  - térreo (principal e terraço): 15 cm, topo +0,10, mais 5 cm de acabamento até +0,15;
  - inferior sobre solo: 10 cm, topo −2,90, mais acabamento até −2,85;
  - forro: 10 cm, topo +3,15.
- **Paredes:**
  - do térreo: de 0,00 a +3,05 (face inferior da laje de forro);
  - do inferior: de −3,00 a −0,05 (face inferior da laje do térreo).
- **Box:** piso a −2,86, 1 cm abaixo do banho.
- **Telhado:** apoiado em +3,15, 4 águas, 30%, beiral 0,60. A cumeeira ficou em **+4,73**, resultado da inclinação.
- **Guarda-corpo:** 1,10 m, tipo "1100mm" do template, nas três bordas do terraço. O terraço é fechado como ambiente por linhas de separação.

## Terreno (v2, refeito em 26/10 após o "flutuando" apontado pelo Lucas)

**O que estava errado na v1:** o corte do inferior e do pátio era um **furo que atravessava o sólido topográfico inteiro**, com 8 m de espessura. Por baixo da casa e do pátio não havia terreno, só vazio. Além disso, o aterro ficava 10 a 30 cm abaixo das paredes e calçadas, deixando frestas.

**v2** (script `apoio/ct03_terreno_v2.py`), sem furo:

- **Escavação:**
  - sob o pavimento inferior, x 1,4–10,6 / y 9,375–16,075, até −3,05, logo abaixo da laje (−3,00);
  - sob o pátio, y 16,075–19,0, até −2,95, logo abaixo do piso (−2,90).
- **Aterro/plataforma:**
  - sob a laje do térreo na frente, x 1,425–10,575 / y 3,925–9,375, em −0,10;
  - em volta das paredes do térreo, em 0,00, a cota da base das paredes;
  - sob as calçadas de acesso, em +0,06.
- **Taludes 1:1** — a ficha não define a extensão; adotado o mínimo:
  - descendo da plataforma até o terreno natural;
  - subindo da borda do pátio até o natural.
  - Junto às paredes do inferior (contenção e paredes de 20 cm), o terreno fica na cota natural, encostado nelas.
- **Terreno natural:** fórmula do casa.json em toda a área fora da intervenção.
- **Pontos:** malha de 0,25 m, com pontos a cada 5 cm nas bordas das áreas de corte e aterro; nenhum ponto da malha a menos de 12 cm delas. Assim a triangulação não atravessa as lajes.
- **Resultado:** zero avisos.
- **Simplificação:** não há muro de arrimo nas laterais do pátio, só talude 1:1. A plataforma da frente é aterro: a fundação não foi modelada.
- **Passeios:**
  - da Rua das Paineiras: plano, em 0,00;
  - da Rua dos Jacarandás: **não modelado**, porque é em rampa;
  - leitos das ruas: só no DXF.

## Arquivo

- O projeto foi criado a partir da V03 restaurada. O Lucas salvou como `Projeto2.rvt` no OneDrive; pedido "Salvar como" `Casa_Teste_03.rvt` na pasta da casa.
- O DXF está **vinculado**, só na vista `PRE - Layout / Nível 2` (térreo), origem com origem.
- **Vistas ↔ níveis do template:** Nível 1 = Inferior (−2,85); Nível 2 = Térreo (+0,15); Nível 3 = Cobertura (+3,15). Os nomes das vistas foram mantidos, porque o plugin os usa.
- Foi criado o nível "RN / Rua" (0,00), sem vistas, para hospedar o terreno e os acessos.
