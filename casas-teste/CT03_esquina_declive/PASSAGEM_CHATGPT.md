# Passagem para o ChatGPT/Codex — continuar do ponto em que o Claude parou (26/10/2026)

## Onde estamos

- **Projeto legal do plugin VEBRAM:** concluído e juntado na `main` (PR #14, 26/10/2026). Toda nova tarefa começa da `main` e volta para a `main` por PR. Não continuar de branches antigas.
- **Última versão compilada:** Revit 2026, .NET 8. Instalação local em `ARQ TESTE\claude-pendencias-0527\current` (manifesto `Vebram.Legal.addin`). Ao abrir o Revit, aparece o aviso de segurança do complemento: escolher "Carregar uma vez" ou "Sempre carregar".
- **Últimas mudanças (Claude):**
  - níveis escalonados e afastados da casa em cortes e fachadas;
  - cortes com todos os pavimentos e com a planta do 2º pavimento como fonte;
  - memória gráfica por pavimento; PISO BOX como área interna;
  - TO com lajes adicionais opcionais, listadas no quadro urbanístico;
  - Q04 com total por pavimento e total geral.
- **Testado em cópia do Sobrado_Teste_02:** PNGs em `validation/pendencias-0527`. O RVT original nunca é salvo nos testes.

## Próxima tarefa: modelar a Casa Teste 03

Tudo nesta pasta (`casas-teste/CT03_esquina_declive`):

1. Ler `LEIA-ME.md` e `ficha/FICHA_MODELAGEM_CT03.md`. Números só de `ficha/casa.json`.
2. Usar `referencia/Casa_Teste_03.dxf` como referência. Abre no AutoCAD e pode ser vinculado/importado no Revit em metros; já foi testado.
3. Modelar no Revit a partir do template V03, com as mesmas regras do Sobrado:
   - famílias de sistema;
   - esquadrias hospedadas;
   - ambientes com os nomes da ficha;
   - tipos de piso com os nomes da ficha;
   - toposólido pela fórmula/pontos;
   - sem DirectShape.
4. Salvar como `ARQ TESTE\casas-teste\Casa_Teste_03_Esquina_Declive\Casa_Teste_03.rvt`, pela interface do Revit.
5. Conferir: zero avisos; vãos recortando paredes; ambientes fechados; escada de −2,85 a +0,15.
6. Rodar o roteiro de teste do plugin (seção "Roteiro" da ficha) numa CÓPIA do RVT e registrar, com prints: funcionou / aviso / falhou.

## Regras combinadas com a Silvana e o Lucas

- Não inventar medidas nem índices legais além dos que estão na ficha. A ficha é um cenário de teste com dados fictícios.
- Testes sempre em cópia; nunca salvar por cima do RVT original.
- Não fechar o Revit com alterações não salvas sem perguntar.
- Explicar resultados sem jargão. Avisar quando precisar de decisão (ex.: critério da TO com o abrigo/terraço).
- Em uso de cota alta (> 85% da janela de 5 h), parar, subir o trabalho no GitHub e escrever uma passagem como esta.
