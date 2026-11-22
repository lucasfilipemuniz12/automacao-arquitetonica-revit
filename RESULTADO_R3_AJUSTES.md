# Resultado R3 — ajustes da revisão da Casa Teste 04

22/11/2026. Branch `claude/legal-r3-ajustes-ct04`, criada de `codex/legal-acabamento-r3` (88020d3). Trouxe também a opção local do Codex de rodar só a CT04 (modo `ct04r3`). Ela agora pode ser usada pelo script com `scripts/Validate-LegalR3.ps1 -SomenteCT04 -Casa04Project ... -OutputFolder ...`.

Só o plugin foi alterado. Nenhum modelo foi mudado para o plugin passar. Os originais conferem com os hashes registrados antes e depois da rodada:

- CT04: BCF426E7…
- CT03: 227261B9…
- Sobrado: BF5C294D…

Todos os testes rodaram em cópias novas `R3_TESTE_*`. Para os testes, o manifesto do Revit apontou para `claude-r3-ajustes\current`. O manifesto original do Codex foi restaurado ao final; o backup está em `logs\`.

## Rodada final (cópias novas, Revit 2026.4)

Pasta: `ARQ TESTE\casas-teste\testes_R3_AJUSTES\R3A_FINAL_20261122_1949`

| Casa | Preservação física | Avisos nativos | Passos | Folhas |
|---|---|---|---|---|
| CT04 | true | 0 | 18 ok + 3 avisos esperados (lote de 5 lados) | LEG-01..03 /03 |
| CT03 | true | 0 | 29 ok + 1 falha da conferência (ver abaixo) | LEG-01..03 /03 |
| Sobrado | true | 0 | 24 ok | LEG-01..03 /03 |

Compilação: zero erros e zero avisos. Testes Arithmetic 48 e SheetPacking 2933 passaram.

A falha da CT03 é da própria rotina de conferência, não das folhas. Ela procurava o Terraço em toda tabela com "Q04" no nome, inclusive nas do pré-projeto e do executivo do template. Conferi no Revit:

- o Q04 legal tem 11 ambientes e não tem o Terraço;
- o Terraço está só no quadro "Áreas descobertas".

A conferência foi corrigida para olhar só as tabelas legais. Essa correção compilou, mas não teve nova rodada no Revit.

PNGs: `casas-teste/revisao_legal_r3/CT04_ajustes`, `CT03_ajustes` e `Sobrado_ajustes`. Mesma cópia em `Casa_Teste_04\testes\R3_AJUSTES_2026-11-22`.

## O que mudou nas folhas da CT04 (comparado com ENTREGA_R3_CT04 de 12/11)

1. **LEG-01 em branco.** A folha sem Implantação, Situação e Quadro urbanístico agora fica como RESERVA: fora do conjunto emitido e fora do total. As demais viram LEG-01/03 a LEG-03/03. O relatório explica que esses três entregáveis não puderam ser preparados e que nenhum índice foi calculado.
   - O quadro urbanístico vazio do template não conta mais como conteúdo da folha (`UrbanisticReference.IsUnfilled`).
   - A identificação "número/total" é apagada das reservas.
   - Na CT03 e no Sobrado, a folha LEG-01 continua emitida, com implantação e situação.
2. **Título do pavimento superior.** Quando o título quebra em duas linhas (medido no rótulo nativo do Revit), ele passa a "Planta do pav. superior", sem mudar a escala. Na CT03, "Planta do pavimento inferior" coube inteiro e ficou como estava.
3. **Memórias gráficas.**
   - As duas memórias (térreo e superior) ficaram na mesma folha das plantas, como decidido em 26/10, sem reduzir escala.
   - A legenda fica ao lado da própria casa. Antes ela ia para depois do muro da divisa.
   - Muros, gradis, corrimãos, DXF e terreno que cruzariam o título ou a legenda ficam ocultos só naquela vista; o modelo não é alterado. Na CT04 foram 7 elementos ocultos na memória do térreo e 3 na do superior; nas duas a linha que cortava a legenda sumiu.
   - Isso vale para as memórias que não são a do lote confirmado. A memória do térreo com lote confirmado (CT03 e Sobrado) não mudou.
4. **Fachadas.**
   - Causa do "terreno a conferir": o plano de leitura do terreno ficava 5 cm à frente do muro da divisa, já fora do toposólido. Agora, se nada é encontrado ali, o plano passa para 5 cm à frente da própria casa.
   - As quatro fachadas mostram o perfil real e o texto "MENOR COTA DO PERFIL REAL…".
   - Muros e gradis da divisa que ficam inteiros entre o observador e a casa são ocultados só na vista. Ficam fora disso os elementos sobre lajes ou telhados da casa e os que estão a menos de 1 m da casa. A vista ganha a nota "MUROS E GRADIS DA DIVISA À FRENTE DA EDIFICAÇÃO OMITIDOS NESTA VISTA".
5. **Carimbo.**
   - A data de emissão é a do dia da rodada (22/11/2026).
   - As folhas emitidas recebem no histórico de revisões a linha "R3 | Emissão para conferência | 22/11/2026", com tipo de texto próprio. A família e o RTE não foram alterados.
   - O lembrete do template "00 | Folha-base…" é ocultado nas folhas emitidas.
   - O campo "CONFERIR STATUS DE EMISSÃO" ficou como no template.

Correções que apareceram durante os testes:

- O Corte A da CT04 deixou de sair porque o Revit recusou a cota de pé-direito da garagem (referência inválida) e desfazia o corte inteiro. Agora só essa cota é omitida, o texto PD fica e o relatório registra.
- As notas geradas no carimbo (número/total) não eram reconhecidas pelo próprio plugin, porque notas de texto não têm o parâmetro Comentários. Agora são identificadas pelo tipo de texto. Isso evita notas duplicadas ao reorganizar e faz a conferência de numeração passar.

## Pendente / observações

- **Fachadas norte e oeste:** a parte de baixo da casa continua escondida. A causa não são mais os muros: é o próprio terreno, que sobe para o norte acima do piso do térreo. A casa está encaixada na encosta. Não foi forçado. Na oeste também aparece, ao fundo, o gradil da divisa leste com hachura pesada; ele está atrás da casa, não à frente.
- **Garagem 14,44 x 14,45:** a diferença continua. O arredondamento do levantamento foi igualado ao da tabela, mas isso não mudou o valor. O valor real deve ficar logo abaixo de 14,445; vale conferir a configuração de arredondamento do Q04.
- **Textos sobrepostos nas escadas das plantas e "PD a conferir" nos cortes:** não foram tratados nesta rodada. São limitações conhecidas do gerador de rótulos e da medição de pé-direito, que só mede onde há par de faces de piso e teto.
- **Garagem (laje bege) sem classe na legenda da memória do térreo:** continua. A laje fica no nível Garagem (-0,60), não no Térreo. A memória do térreo mede só os pisos do próprio nível.
- **Q04 da CT04 em "não coube nas faixas laterais":** ficou na área principal, como antes.
- **Lote de cinco lados:** Implantação, Situação e índices urbanísticos continuam pendentes até o plugin calcular lotes não retangulares. Nada é calculado com o retângulo envolvente.
- **Fora do escopo:** não houve merge.
