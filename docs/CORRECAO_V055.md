# Correção 0.5.5 — fachadas e situação

## Diagnóstico confirmado

O journal.0102 do Revit contém a falha The datum plane cannot be visible in the view durante preparar quatro fachadas. Não é mais a duplicação do template: ocorre ao tentar modificar os níveis nativos em elevações onde o plano do datum não está disponível.

Situação: ocultar todas as categorias Model também ocultava OST_Lines, categoria utilizada pelas DetailCurve do lote e da via. Textos permaneciam visíveis, produzindo o resultado descrito por Lucas.

## Correções instaladas

Fachadas: verifica CanBeVisibleInView; cada ajuste de datum ocorre numa SubTransaction. Quando o Revit recusa o datum, reverte apenas esse ajuste e escreve nome/elevação em texto na lateral, registrando essa condição no relatório. Não modifica extensão global nem elevação dos níveis e não interrompe toda a fachada por essa indisponibilidade.

Situação: exclui Lines da ocultação de categorias do modelo, reativa Lines/subcategorias e aplica traço preto peso 3 ao diagrama. O resultado previsto contém lote retangular, cotas, norte, via quando informada e confrontantes. Não é levantamento de quadra ou mapa cadastral.

Fonte 0.5.5 compilado sem erros e sem avisos, instalado em current/Vebram.Revit.dll após salvar e fechar a sessão visível do Revit. Backup da versão anterior em staging-v055/ANTES_*. Caminho e GUID preservados. Revit foi reaberto para testar em cópia nova Casa_Terras_FACHADAS_SITUACAO_V055.rvt.

## Validação ainda bloqueada

A reabertura mostrou o aviso Segurança — Complemento não certificado. Computer Use proíbe operar esse aviso. Lucas está dormindo e pediu autonomia sem solicitações: nenhuma solicitação foi enviada.

Não há relatório TESTES_FACHADAS_SITUACAO.json nem resultado nativo aprovado. O RVT V055 é apenas uma cópia do modelo salvo antes da execução. A validação foi preparada no modo facades055, que verifica repetição, direções cardeais, categoria de linhas visível, curvas do diagrama e preservação dos limites físicos. Não valida mapa cadastral nem critérios municipais.

Job guardado inerte em teste-fachadas-v055/VALIDACAO_PENDENTE.json; recolocar explicitamente em current/section-validation-job.json na retomada, depois do carregamento e antes de reiniciar Revit. Sem job ativo não inicia testes automaticamente. Após a execução, revisar graficamente as quatro fachadas e a situação antes de declarar conclusão.

Referência API: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/88545b41-8cd2-521a-748c-94967a66dc72.htm
