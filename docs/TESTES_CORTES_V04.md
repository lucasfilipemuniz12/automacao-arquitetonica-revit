# Cortes A/B — 0.4.2 concluída

Testes nativos concluídos em 14/10/2026. Compilação sem erros/avisos; versão instalada em dist-v042/Vebram.Revit.Legal.V042.dll.

Abrir Casa_Terras_CORTES_V04.rvt e as vistas LEGAL - Corte A / LEGAL - Corte B. Na faixa Projeto legal, Preparar legal agora oferece os dois cortes. Escolha planta e piso, eixo X/Y, posição de 5 a 95%, sentido e profundidade em metros. Escala/estilo são lidos das Preferências legais.

Resultados na Casa Terras: mesmas vistas 350964/350982 do V02; dois níveis identificados e uma cota vertical entre faces reais em cada corte; quatro anotações geradas por vista. Repetição, posição a 25%, inversão, profundidades 8/15 m e proteção de edição verificadas. IDs das vistas, elevações dos níveis e limites físicos da modelagem preservados. Essa comparação de limites físicos não equivale a auditoria completa de toda a geometria.

O estado final foi restaurado ao centro (50%), sentido normal, profundidade 15 m. Evidências em TESTES_CORTES.json e CONCLUIDO.txt. Revisão visual e cotagem/detalhamento completo ainda pendentes; não foi feita montagem de folha.

Correções anteriores: transformação direta da vista não orientou o corte; busca por visibilidade não encontrou o elemento interno. Ambas revertidas sem salvar. A versão atual identifica o recorte por nome/tipo da vista, exige um único candidato e transforma esse elemento, conservando a ViewSection. Referência: https://forums.autodesk.com/t5/revit-api-forum/rotating-a-plan-view/m-p/3671698

## Avisos de carregamento

Lucas já escolhe Sempre carregar. As rotinas temporárias de teste tiveram caminhos e GUIDs diferentes, provocando novos avisos; o produto instalado conserva GUID fixo. Para próximas etapas, padronizar caminho/identidade e substituir a DLL com Revit fechado. Não prometer ausência de todo aviso: mudança de binário/confiança pode exigir nova aprovação. Assinatura digital com certificado confiável faz parte da preparação para distribuição. Não desativar a proteção do Revit.

Instalação estável após pedido de Lucas: current/Vebram.Revit.dll; AssemblyName Vebram.Revit; mesmo GUID do produto. Recompilação do mesmo fonte 0.4.2 sem erros/avisos. Os testes nativos usaram a DLL V042 com o mesmo código antes da padronização do nome. Build.ps1 agora exige Revit fechado para atualizar o caminho fixo. O próximo carregamento no novo caminho pode pedir confiança uma vez; Sempre carregar permanece para o mesmo caminho/ID, conforme Autodesk. Não foi alterada a proteção do Revit. Evitar criar novos manifestos de teste a cada revisão; desenvolver com atualização da DLL fixa e reinício.
