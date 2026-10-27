# Resultado do teste do plugin VEBRAM na Casa Teste 03 (Claude, 26/10/2026)

- **Arquivo testado:** cópia `Casa_Teste_03_teste_plugin.rvt` (pasta da casa, no Desktop), feita do modelo salvo pelo Lucas com o terreno corrigido. O original não foi alterado.
- **Plugin:** versão da `main`, igual à branch claude/pendencias-folhas-0527, carregada pelo pyRevit.
- **Código do plugin:** NÃO foi alterado.
- **Script:** `apoio/ct03_teste_plugin.py`.
- **Imagens:** `revisao_claude/teste_plugin/`.
- **A cópia ainda não foi salva:** o pyRevit não salva. Pedir Ctrl+S ao Lucas para guardá-la.

## Passo a passo

| # | Passo | Resultado | Observação |
|---|---|---|---|
| 01 | Perfil municipal de teste | ok | "BH — ZR-2 (perfil de teste CT03)" salvo na biblioteca do plugin neste PC |
| 02 | Cadastro do projeto | **falhou → ok** | O campo Pavimentos só aceita número inteiro; o texto da ficha foi recusado. Com "2", ok |
| 03 | Carimbo | ok | 10 campos preenchidos, incluindo autor, CAU, datas e responsáveis |
| 10 | Implantação (fonte = térreo) | **falhou → ok** | "Escolha a planta de modelagem no mesmo nível da Implantação do V02": as vistas legais de Implantação e Situação do template ficam no Nível 1, que na CT03 é o pavimento **inferior**. Contornado só na cópia, recriando as duas vistas no Térreo |
| 10 | TO com 2 lajes (principal + terraço) | ok | 79,15 + 33,40 m², sobreposição contada uma vez = **111,17 m²**; quadro lista cada laje |
| 11 | TO com 1 laje (padrão) | ok | **79,15 m²**; TO ≈ 21,1% do lote de 375 m² |
| 11 | Situação | aviso | Confrontantes preenchidos. O direito é "Rua dos Jacarandás (via pública)" e aparece só como texto de confrontante, não como via |
| 11 | Quadro urbanístico | ok | Limites do perfil (TO 50%, CA 1,0, TP 20%) |
| 12 | Implantação em 1:100 | aviso | A vista fica com 49 m de altura (lote 25 m, rua, passeio e notas) e não cabe na A1. Em 1:200 cabe |
| 20 | Planta legal do térreo | aviso | 7 cotas; várias "pendentes para preservar leitura do rótulo" (hall e lavabo pequenos); terraço sem par de paredes |
| 21 | Planta legal do inferior | ok | 11 cotas |
| 22 | Cobertura | ok | 4 águas a 30% reconhecidas |
| 23 | Memória do térreo | ok | Interna 47,45 · externa 32,03 (terraço) · **garagem coberta 15,25** (detectou a garagem) · lote e terreno |
| 24 | Memória do inferior | ok | Interna 45,18 m² (box incluído) · sem lote/terreno, como previsto |
| 25 | Q04 | ok | 12 ambientes; totais por pavimento |
| 26 | Quadros de esquadrias | ok | |
| 27 | Corte A (longitudinal, x ≈ 3) | **aviso grave** | Níveis negativos ok; PD da cozinha, estar e banho ok. Mas **terraço e suíte com "PD a conferir"**, e o nível "RN / Rua" fica **sobre o terraço** |
| 28 | Corte B (transversal, y ≈ 11,2) | ok | 3 cotas de PD |
| 29 | Fachadas (4) | **aviso grave** | **Não mostram o pavimento inferior nem o terreno inclinado**: a fachada leste aparece como casa térrea em terreno plano |
| 30 | Organizar folhas | **falhou** | "Implantação não cabe… em 1:100". O organizador usa a escala das preferências, 1:100, e não aceita a Implantação em 1:200. Nenhuma folha montada |

## O que o plugin precisa melhorar, por prioridade

1. **Contexto da edificação.**
   - Hoje é a laje principal com 2 m de folga. Precisa incluir **tudo o que pertence à casa**: terraço, pavimento inferior além da laje principal, guarda-corpos.
   - Isso derruba juntos: o PD "a conferir" nos cortes; o nível sobre o terraço; as fachadas sem o inferior e sem o terreno; o afastamento de fundos medido da laje principal (12,48 m) em vez do fundo real da casa (≈ 8,9 m).
2. **Fachadas e cortes em terreno inclinado.**
   - A linha de terreno e a base da fachada vêm de uma cota só.
   - Precisa usar o perfil real do toposólido em cada vista e mostrar o pavimento abaixo da rua.
3. **Escala da Implantação no organizador.**
   - Lote de 25 m não cabe em A1 a 1:100.
   - Permitir escala por entregável, por exemplo Implantação 1:200, ou sugerir automaticamente.
4. **Nível da Implantação e Situação.**
   - Não presumir que o "Nível 1" do template é o térreo.
   - Deixar escolher o nível; casas com pavimento abaixo da rua quebram.
5. **Lote de esquina.**
   - Desenhar a segunda via (Rua dos Jacarandás) na Implantação e na Situação.
   - Aceitar "confrontante = via" e afastamento frontal secundário.
6. **Cadastro — Pavimentos:** aceitar texto ("2 — térreo + inferior") ou separar número e descrição.
7. **Planta legal:** cotas internas de ambientes pequenos (hall/lavabo) ficam pendentes. Ok como aviso, mas vale ter cota externa com chamada.

## Não testado nesta rodada

- Variante com chanfro de esquina (lote não retangular).
- Folhas finais em PNG, porque o organizador falhou. As vistas foram conferidas individualmente.
