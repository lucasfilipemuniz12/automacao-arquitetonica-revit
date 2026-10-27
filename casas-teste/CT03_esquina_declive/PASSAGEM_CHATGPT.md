# Passagem para o ChatGPT: correções do plugin após a Casa Teste 03 (27/10/2026)

## Onde estamos
- O plugin VEBRAM tem uma versão oficial: a branch **`main`** (PR #14 mesclada em 26/10). Ela contém todo o trabalho do Codex até a 0.5.27 e os ajustes do Claude.
- A **Casa Teste 03** (Residência Paineiras: esquina, terreno em declive, pavimento inferior a −2,85) foi modelada pelo Claude e salva pelo Lucas em:
  `C:\Users\lucas\OneDrive\Área de Trabalho\ARQ TESTE\casas-teste\Casa_Teste_03_Esquina_Declive\Casa_Teste_03.rvt`
  - Zero avisos do Revit. O terreno tem plataforma de aterro, escavação do inferior e do pátio e taludes de 1:1.
  - Hipóteses adotadas: `HIPOTESES_CT03.md`.
- O plugin da `main` foi testado numa cópia (`Casa_Teste_03_teste_plugin.rvt`). O resultado completo está em `RESULTADO_TESTE_CT03.md`, nesta pasta, com prints em `revisao_claude/teste_plugin/`.
- O template `ARQ_AUTOMACAO_V03.rte` foi restaurado do backup de 19/10, porque a casa tinha sido modelada dentro dele. A versão estragada ficou guardada como `ARQ_AUTOMACAO_V03_estragado_ChatGPT_20261026.rte`. **Nunca modele nem salve dentro do .rte.**

## Sua tarefa: corrigir o plugin (nesta ordem)
Crie uma branch nova **a partir da `main`** (por exemplo `codex/ct03-casa-inteira-terreno`) e abra um PR para a `main` no fim.

1. **Enxergar a casa inteira.** Hoje o contexto da edificação é a laje principal mais 2 m. Ele precisa incluir tudo o que pertence à casa: o terraço, o pavimento inferior além da laje principal e os guarda-corpos. Isso deve resolver:
   - o "PD a conferir" do terraço e da suíte no Corte A;
   - o nível "RN / Rua" desenhado sobre o terraço;
   - as fachadas sem o pavimento inferior;
   - o afastamento de fundos medido da laje (12,48 m) em vez do fundo real da casa (≈ 8,9 m).
2. **Terreno inclinado em fachadas e cortes.** Usar o perfil real do toposólido em cada vista, e não uma cota única, mostrando o pavimento abaixo da rua.
3. **Escala da Implantação no Organizar folhas.** Um lote de 25 m não cabe no A1 em 1:100. Permitir escala por entregável (Implantação 1:200) ou sugerir a escala automaticamente. Hoje nenhuma folha é montada.
4. **Nível da Implantação e Situação.** Não supor que o "Nível 1" do template é o térreo. Deixar escolher o nível.
5. **Lote de esquina.** Desenhar a segunda via (Rua dos Jacarandás) na Implantação e na Situação. Aceitar "confrontante = via" e afastamento frontal secundário.
6. **Cadastro.** O campo Pavimentos deve aceitar texto, ou ter número e descrição separados.

Itens 1 a 3 são o essencial. Os itens 4 a 6 entram se couber.

## Como testar
- Compilar (`scripts\Build.ps1`) e rodar os testes (Arithmetic e SheetPacking): tudo tem que continuar passando.
- Instalar só com o Revit fechado (`scripts\Install-Local.ps1`).
- Testar numa **CÓPIA** da `Casa_Teste_03.rvt` e também numa cópia do `Sobrado_Teste_02`, para não quebrar o que já funcionava.
- Seguir o roteiro de `RESULTADO_TESTE_CT03.md` e exportar PNG das folhas LEG.

## Regras combinadas
- Não inventar medidas nem índices de lei. Os dados da CT03 são fictícios, mas tratados como reais no teste.
- Não alterar a modelagem para o plugin "passar".
- Testes só em cópia. Nunca salvar por cima dos RVT originais nem do template.
- Nunca fechar o Revit com alterações não salvas sem perguntar ao Lucas.
- Código: branch nova a partir da `main` e volta por PR. Não continuar de branches antigas.
- Explicar ao Lucas e à Silvana sem jargão. Eles não usam terminal.

## O que entregar
- O PR para a `main` com a descrição do que mudou.
- `RESULTADO_TESTE_CT03_R2.md` nesta pasta, com funcionou / aviso / falhou por passo e os prints novos.
