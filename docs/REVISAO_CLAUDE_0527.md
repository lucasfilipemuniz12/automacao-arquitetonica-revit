# Revisão Claude sobre a 0.5.27 — pendências leves de folhas e fachadas

Base: `codex/revisao-grafica-pisos-0527`. Branch: `claude/pendencias-folhas-0527`.
Escrito sem compilar (sem Revit/dotnet na nuvem). Compilar, rodar os testes e testar em CÓPIA do Sobrado_Teste_02 antes de mesclar.

1. **Organizar folhas avisa o que ficou de fora.** `SheetPlanner.Unprepared` lista as vistas legais ainda não preparadas; a janela mostra "AINDA NÃO PREPARADOS (ficam fora)" com o comando que prepara cada uma (fachadas: "Preparar fachadas"). A distribuição em si não muda.
2. **Numeração.** Depois da consolidação, se uma folha existente ficou vazia (reserva) e uma folha nova recebeu número maior, as duas trocam de número (`Compact`): o conteúdo fica na LEG-03 e a folha vazia vira LEG-04 de reserva. Nenhuma folha existente é apagada.
3. **Nome em minúsculas.** `CombinedName` agora começa com maiúscula ("Fachadas").
   **"LEG-04/" no carimbo.** Hipótese: o carimbo imprime "número/total" e o total vem de um parâmetro de texto/inteiro vazio na folha nova. `NewSheet` copia da folha de referência os parâmetros cujo nome contém "total" ou "folhas" (folha e instância do carimbo), só se estiverem vazios. CONFERIR no carimbo o nome real do parâmetro; o total copiado pode precisar ser atualizado à mão.
4. **"Terreno a conferir" nas fachadas.** Quando o terreno está recortado sob a casa, não há superfície no centro. `BuildingFrame` agora amostra o terreno 0,5 m por fora de cada canto e meio de lado do contorno e usa a cota mais baixa (altura total conservadora). Texto: "BASE: TERRENO DO MODELO (COTA MAIS BAIXA JUNTO À EDIFICAÇÃO)".

Testes a rodar: regressões de folhas (`SheetPlannerValidation`, `GraphicRegression`, `CasaTestRegression`) podem acusar mudança de número/nome de folha por causa dos itens 2 e 3; conferir se é a mudança esperada antes de atualizar a referência.
5. **Recorte da planta legal (LEG-02).** O recorte crescia até o lote inteiro porque somava a caixa de toda marcação gerada, mesmo longe da casa (ex.: rótulos de ambientes do quintal/terreno), e ainda tinha folga de 3 m nas laterais e 4 m embaixo. Agora as marcações a mais de 3 m da edificação não esticam o recorte (ficam na vista, fora do recorte, e o relatório avisa quantas), e a folga caiu para 2 m em todos os lados. CONFERIR no Sobrado se as cabeças dos cortes A/B continuam aparecendo; se não, aumentar a folga em `LegalModules.cs`. Se o piso base escolhido for do tamanho do lote, o recorte continua grande: aí o problema é a escolha do piso, não o recorte.

Ainda abertos: item 5 (composição da LEG-04), anotações amontoadas na LEG-02, itens 6, 7 e 9. São ajustes visuais ou de cálculo para fazer com o Revit aberto.
