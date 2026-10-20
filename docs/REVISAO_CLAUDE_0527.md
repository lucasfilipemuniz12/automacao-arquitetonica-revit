# Revisão Claude sobre a 0.5.27 — pendências leves de folhas e fachadas

Base: `codex/revisao-grafica-pisos-0527`. Branch: `claude/pendencias-folhas-0527`.
Escrito sem compilar (sem Revit/dotnet na nuvem). Compilar, rodar os testes e testar em CÓPIA do Sobrado_Teste_02 antes de mesclar.

1. **Organizar folhas avisa o que ficou de fora.** `SheetPlanner.Unprepared` lista as vistas legais ainda não preparadas; a janela mostra "AINDA NÃO PREPARADOS (ficam fora)" com o comando que prepara cada uma (fachadas: "Preparar fachadas"). A distribuição em si não muda.
2. **Numeração.** Depois da consolidação, se uma folha existente ficou vazia (reserva) e uma folha nova recebeu número maior, as duas trocam de número (`Compact`): o conteúdo fica na LEG-03 e a folha vazia vira LEG-04 de reserva. Nenhuma folha existente é apagada.
3. **Nome em minúsculas.** `CombinedName` agora começa com maiúscula ("Fachadas").
   **"LEG-04/" no carimbo.** Hipótese: o carimbo imprime "número/total" e o total vem de um parâmetro de texto/inteiro vazio na folha nova. `NewSheet` copia da folha de referência os parâmetros cujo nome contém "total" ou "folhas" (folha e instância do carimbo), só se estiverem vazios. CONFERIR no carimbo o nome real do parâmetro; o total copiado pode precisar ser atualizado à mão.
4. **"Terreno a conferir" nas fachadas.** Quando o terreno está recortado sob a casa, não há superfície no centro. `BuildingFrame` agora amostra o terreno 0,5 m por fora de cada canto e meio de lado do contorno e usa a cota mais baixa (altura total conservadora). Texto: "BASE: TERRENO DO MODELO (COTA MAIS BAIXA JUNTO À EDIFICAÇÃO)".

Testes a rodar: regressões de folhas (`SheetPlannerValidation`, `GraphicRegression`, `CasaTestRegression`) podem acusar mudança de número/nome de folha por causa dos itens 2 e 3; conferir se é a mudança esperada antes de atualizar a referência.
