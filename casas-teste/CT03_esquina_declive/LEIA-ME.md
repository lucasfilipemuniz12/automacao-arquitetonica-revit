# Casa Teste 03 — Residência Paineiras (lote de esquina em declive)

Terceira casa de teste do plugin VEBRAM, criada em 26/10/2026 a pedido do Lucas e da Silvana para estressar o projeto legal. Não veio de um DWG da internet: é um projeto inventado, com dados completos.

## Arquivos

| Arquivo | Para quê |
|---|---|
| `ficha/casa.json` | Fonte única: cadastro, perfil municipal de teste, lote, vias, terreno, níveis, lajes, paredes, ambientes, esquadrias, escada e cortes |
| `ficha/FICHA_MODELAGEM_CT03.md` | Ficha para o ChatGPT/Codex modelar no Revit e roteiro de teste do plugin |
| `referencia/Casa_Teste_03.dxf` | Desenho 2D em metros: implantação com curvas de nível de 0,50 m, plantas do térreo e do inferior, cortes A-A e B-B esquemáticos. Abre no AutoCAD como um DWG; para ter `.dwg`, abrir e "Salvar como". |
| `referencia/previa.png` / `previa.svg` | Prévia do DXF, para conferir sem AutoCAD |
| `apoio/gerar_dxf.js` | Gera o DXF a partir do `casa.json` (`node apoio/gerar_dxf.js`). Mudou a ficha, é só rodar de novo. |
| `apoio/dxf_para_svg.js` | Gera a prévia SVG do DXF |

## Resumo

- **Lote:** 15 × 25 m, de esquina entre a Rua das Paineiras (frente) e a Rua dos Jacarandás (lateral).
- **Terreno:** cai ≈ 4,7 m para o fundo e para a rua lateral.
- **Térreo (+0,15):** estar/jantar, garagem, cozinha, hall, lavabo, escada e terraço descoberto sobre laje.
- **Inferior (−2,85), só sob o fundo:** banho, estar íntimo, escada, suíte e quarto 02, com saída ao pátio.
- **Áreas em eixos:** ≈ 76,5 m² internos no térreo + 31,5 m² de terraço; 58,5 m² no inferior.
- **Estresse proposital:**
  - duas frentes e confrontante que é rua;
  - níveis negativos;
  - pavimento inferior com contorno diferente;
  - duas lajes no térreo (TO);
  - terraço sobre área interna;
  - variante opcional com chanfro (lote não retangular).

Os dados cadastrais, as pessoas e o perfil municipal são inventados. No teste, tratar como se fossem reais. Não são a legislação real de Belo Horizonte nem pessoas reais.
