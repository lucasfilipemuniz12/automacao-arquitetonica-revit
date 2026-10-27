# Passagem para o ChatGPT/Codex — 27/10/2026 (Claude → ChatGPT)

## Estado atual

- **Plugin VEBRAM (projeto legal):** está na `main` (PR #14). A `main` é a referência. Toda mudança de código sai de uma branch nova criada a partir da `main` e volta por pull request.
- **Documentos da Casa Teste 03:** na branch `claude/casa-teste-03` (pasta `casas-teste/CT03_esquina_declive`). Só documentos e scripts de modelagem; nenhum código do plugin.
- **Template V03:**
  - Restaurado em 26/10. O ChatGPT tinha modelado a casa DENTRO do `ARQ_AUTOMACAO_V03.rte`.
  - A versão estragada ficou guardada como `ARQ_AUTOMACAO_V03_estragado_ChatGPT_20261026.rte`.
  - **Nunca modelar nem salvar dentro de um .rte.**
- **Casa Teste 03 modelada pelo Claude, via pyRevit:**
  - **Arquivo:** `C:\Users\lucas\OneDrive\Área de Trabalho\ARQ TESTE\casas-teste\Casa_Teste_03_Esquina_Declive\Casa_Teste_03.rvt`. Atenção: está na pasta do OneDrive.
  - **Conteúdo:** terreno em declive com corte, aterro e taludes; paredes, lajes e pisos com os nomes que o plugin lê; 23 esquadrias; escada em U; telhado 4 águas 30%; 12 ambientes; DXF vinculado; zero avisos.
  - **Scripts e decisões:** em `apoio/` e `HIPOTESES_CT03.md`.
  - **Níveis × vistas do template:** Nível 1 = Inferior (−2,85), Nível 2 = Térreo (+0,15), Nível 3 = Cobertura (+3,15). Ver o item 4 abaixo.
- **Teste do plugin na CT03:** cópia `C:\Users\lucas\Desktop\ARQ TESTE\casas-teste\Casa_Teste_03_Esquina_Declive\Casa_Teste_03_teste_plugin.rvt`. Resultado completo em `RESULTADO_TESTE_CT03.md`; imagens em `revisao_claude/teste_plugin/`.

## Próxima tarefa: corrigir o plugin, nesta ordem

1. **"A casa inteira".**
   - **Hoje:** `BuildingFrame.Read` (src/Vebram.Revit/BuildingFrame.cs) só considera elementos dentro do contorno da laje principal + 2 m.
   - **Na CT03:**
     - terraço e fundo do inferior ficam de fora;
     - Corte A com "PD a conferir" no terraço e na suíte;
     - símbolo de nível "RN / Rua" em cima do terraço;
     - fachadas sem o pavimento inferior;
     - afastamento de fundos medido da laje principal: 12,48 m em vez de ≈ 8,9 m.
   - **Corrigir:** o contexto passa a ser a edificação inteira. Sugestão: todos os pisos, paredes, telhados e guarda-corpos ligados ao corpo da casa, em todos os níveis, sem pegar terreno nem calçadas.
   - **Não mudar:** o cálculo da TO, que já tem a escolha de lajes.
2. **Fachadas e cortes em terreno inclinado.**
   - A linha de terreno e a base vêm de uma cota só. A fachada leste da CT03 aparece como casa térrea em terreno plano.
   - Usar o perfil real do toposólido na vista e mostrar os pavimentos abaixo da rua.
3. **Organizar folhas × escala da Implantação.**
   - O organizador usa a escala das preferências (1:100) e falha: "Implantação não cabe… em 1:100".
   - Respeitar a escala de cada vista, por exemplo Implantação 1:200, ou sugerir a escala que cabe.
4. **Nível da Implantação/Situação.**
   - O plugin exige que a planta-fonte esteja no nível das vistas legais do template (Nível 1).
   - Em casa com pavimento abaixo da rua isso quebra. Permitir escolher o nível ou criar as vistas no nível escolhido.
5. **Lote de esquina:** desenhar a segunda via na Implantação e na Situação; aceitar confrontante = via.
6. **Cadastro — Pavimentos:** aceitar descrição, ou separar número e texto.

Depois de cada correção:

- compilar (.NET 8, Revit 2026; `scripts/Build.ps1` com o Revit fechado) e rodar os testes (`tests/Arithmetic` e `tests/SheetPacking`);
- testar numa **nova cópia** da CT03 com o roteiro do script `apoio/ct03_teste_plugin.py`;
- exportar as folhas LEG em PNG.

## Regras combinadas (Silvana e Lucas)

- Testes sempre em CÓPIA; nunca salvar por cima do RVT original nem do template.
- Não fechar o Revit com alterações não salvas sem perguntar.
- Não inventar medidas nem índices legais. Dado que faltar vai para um arquivo de hipóteses ou vira pergunta.
- Explicar resultados sem jargão. Pedir decisão quando for critério de projeto ou municipal; o critério da TO com abrigo/terraço é deles.
- Uso alto (> 85% da janela de 5 h): parar, subir no GitHub e escrever uma passagem como esta.
- Ao abrir o Revit aparece o aviso de segurança do complemento VEBRAM: escolher "Carregar uma vez" (o usuário clica).
