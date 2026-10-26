# VEBRAM 0.5.18 — zonas laterais e notas

## Uso
Projeto legal > Preparar legal > Organizar folhas > Calcular prévia.
A prévia passa a mostrar a folha inteira: área principal, campos laterais e área de identificação/histórico protegida. Aplicar confirma a distribuição calculada; cancelar reverte a prévia.

## Comportamento
- Leitura dos separadores e cabeçalhos da família ARQ Carimbo A1 do V02; zonas transformadas para as coordenadas de cada instância de carimbo.
- Q04 preferencialmente em Quadros/informações da folha da Planta.
- Quadro urbanístico preferencialmente em Quadros/informações; se não couber na altura, tenta Legendas/atenção. O próprio quadro continua identificado pelo seu título interno.
- Quadros que não couberem nas zonas laterais continuam na área principal, com aviso na prévia. Não se reduz texto ou escala para forçar o encaixe.
- O quadro urbanístico utiliza um tipo de viewport separado, sem o título externo duplicado. O tipo original é preservado.
- As notas são inseridas no campo Notas gerais, com conteúdo pertinente à folha. Informações urbanísticas são descritas como dados informados; não são validadas ou recalculadas nesta etapa.
- Notas geradas e não editadas podem ser atualizadas. Notas editadas ou fixadas e demais conteúdos manuais são preservados; ocupação do campo impede inserir notas automáticas sobrepostas.
- Apenas notas das folhas efetivamente reorganizadas são atualizadas. Escalas, modelo físico, identidade das vistas e dados dos quadros são preservados.
- Carimbo/histórico continuam reservados. Não há preenchimento automático de autoria, endereço ou responsabilidade técnica.

## Medição do V02
Medição nativa em teste-zonas-v0518/medicao-02/ZONAS_CARIMBO.json. O carimbo não forneceu geometria 2D pela consulta à instância; foi lida a família, sem salvar alterações nela.
O Q04 mede cerca de 193,2 x 83,6 mm com margem de segurança e coube em Quadros/informações. O urbanístico sem título externo mede cerca de 190,2 x 118,1 mm e coube no campo inferior Legendas/atenção. Os cabeçalhos fixos do template foram preservados.

## Limites e próximos passos
Esta entrega libera o espaço principal com quadros laterais e mostra todas as zonas na prévia. A escolha de escalas por tipo de vista continua pendente; esta versão preserva as escalas atuais.
A memória gráfica de áreas ainda não foi implementada. Não copiar números do PDF R20: o usuário confirmou que alguns parâmetros do teste foram arbitrários e não confirmou que o RVT de teste é a origem do PDF. Antes da memória definitiva, identificar o modelo correto e os critérios de classificação.
Continuam pendentes a leitura de textos/hachuras em cortes e os marcadores de plantas/cobertura registrados na 0.5.16.
O suporte de zonas é específico ao carimbo V02 medido. Um carimbo sem zonas reconhecidas usa a área principal e emite aviso; não libera toda a lateral indiscriminadamente.

## Preservação
Backup: backups/antes-zonas-20261015_135452. Cópia Claude anterior preservada. Instalação fixa em current/Vebram.Revit.dll. Testes em cópias do RVT; sem substituir o modelo de origem nem alterar os valores informados.

## Validação concluída em 15/10/2026
Build Release sem erros ou avisos. Teste nativo completo em teste-zonas-v0518/nativo-03, CONCLUIDO.txt, PLANEJAMENTO_018.json e PREVIA_CALCULADA.json.
Foram conferidos: prévia revertida, repetição sem novas folhas/notas duplicadas, dois quadros na lateral, notas nas quatro folhas, proteção de vistas fixadas e notas editadas, preservação de nota manual, recusa de vista maior que a folha e botão WPF da prévia. As quatro folhas exportadas foram inspecionadas visualmente.
