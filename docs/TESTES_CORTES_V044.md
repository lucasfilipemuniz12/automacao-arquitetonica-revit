# Cortes 0.4.4 — evidência local

Teste nativo executado no Revit 2026.4 na cópia da Casa Terras. Cortes A/B mantidos nas ViewSection do V02, IDs 350964/350982. Repetição, posição 25%, inversão, profundidade 8/15 m e proteção de texto editado passaram. Limites dos elementos físicos, elevações e IDs das vistas preservados.

Cada corte terminou com uma cota vertical entre faces reais e dois níveis nativos com apenas uma bolha. Removidas as notas de nível duplicadas. Linhas curtas na lateral, cota em faixa separada e nota abaixo do terreno. O cálculo de margem incorpora todos os elementos físicos visíveis. Ambos os cortes foram revisados visualmente nesta sessão, sem sobreposição dos níveis com a casa/terreno.

O teste automatizado de afastamento compara com o contorno base; o contexto completo foi conferido também na imagem. Não equivale a detalhamento construtivo ou cotagem completa.

Evidências locais: `teste-cortes-v044/TESTES_CORTES.json` e `Casa_Terras_CORTES_LIMPOS_V044.rvt`, não distribuídos no GitHub.

## Candidato seguinte

Fontes 0.5.1 de fachadas/situação/urbanismo/folhas compilam, porém sua validação nativa foi bloqueada pelo aviso de complemento não certificado. Não confundir a evidência de cortes 0.4.4 com uma aprovação dos módulos novos.
