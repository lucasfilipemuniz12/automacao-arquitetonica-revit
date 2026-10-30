# Projeto legal — acabamento R3

Base: main após a PR #15 (`69674ae`). Branch de trabalho: `codex/legal-acabamento-r3`.

- Organizador renumera as folhas com vistas/tabelas em sequência e completa número/total no carimbo. Referências vazias ficam como RESERVA, preservadas no documento. O total considera o conjunto emitido, não as reservas. Notas por folha acompanham a renumeração.
- Folhas novas recebem os responsáveis preenchidos nas LEG existentes. Valores genéricos como Projetista não são usados como origem.
- O carimbo V03 não tem parâmetro de total: a identificação completa é uma anotação opaca na posição medida do rótulo original, com fonte e tamanho lidos da família. O RTE e a família não são alterados. Deve-se organizar novamente após mudar a numeração/conjunto.
- Memórias reservam uma coluna à direita de toda a geometria visível. O terreno só aparece na memória do térreo. Letras de classe continuam dentro do desenho.
- Títulos das plantas usam o nível real. Instâncias CAD ficam ocultas nessas vistas; o vínculo/modelo não é apagado.
- Implantação permite segunda via, com lado, nome, passeio e leito informados. Situação reutiliza os mesmos dados. O frontal secundário é medido nas faces externas do envelope; o mínimo só vem de perfil correspondente ao cadastro, chave `afastamento_frontal_secundario`. Sem esse valor aparece “a verificar”. O formulário da biblioteca permite informá-lo.
- Q04 mantém as áreas nativas e separa áreas descobertas, fora do total útil. A classificação reconhece DESCOBERTO no nome e Terraço sem piso/forro/telhado acima do ponto do ambiente. É uma classificação física do teste, não critério municipal. O teste CT03 mede o terraço separadamente. Telhados inclinados usam envelope para essa conferência; varandas parcialmente cobertas exigem revisão.
- Lote com polígono não retangular é recusado antes de gerar Implantação ou calcular índices. A mensagem explica a limitação e preserva a geometria. O limite do sólido topográfico também é conferido para evitar aceitar silenciosamente seu retângulo envolvente. Terreno regional que não corresponda ao lote exige conferir a fonte do limite.

O cálculo de `OccupationFootprint` não foi alterado.

As rotinas `Case04Model`, `Case04Legal` e `LegalR3Validation` são testes optativos de desenvolvimento, executados somente quando existe um arquivo de trabalho de validação ao abrir o Revit. A CT04 nasce com `NewProjectDocument(template)` e destino `.rvt`, nunca abrindo o RTE para modelar. Testes legais exigem prefixo `R3_TESTE_`.

Builds capturam binlog. Instalação e cópia das DLLs somente com Revit fechado. Resultado, ressalvas e evidências finais: `RESULTADO_R3.md`.
