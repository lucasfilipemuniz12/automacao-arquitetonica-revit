# Contexto mestre — automação arquitetônica para Revit

**Atualizado em 13/10/2026.** Consolidação dos documentos de continuidade e planejamento. Serve para orientar o repositório e os chats; não substitui auditoria do template, abertura de RVT nem execução de protótipos.

## 1. Objetivo e princípios

Desenvolver um produto para Revit com botões simples, reutilizáveis e econômicos. Prioridade para execução local determinística, sem tokens de IA por clique. IA poderá ser opcional para interpretar briefings/referências, sempre propondo hipóteses e dados revisáveis antes de alterar o modelo.

Organização planejada: **Modelagem** (inclui pré-projeto), **Projeto legal**, **Projeto executivo** e **Ferramentas gerais**. Um motor comum deve atender aos perfis legal e executivo, evitando duplicação de lógica.

Primeiro produto acordado: **Preparar documentação** — reconhecer projeto e perfil, preparar vistas, atualizar quadros nativos, montar folhas de forma guiada e exportar com relatório. Cotagem, implantação/memória de áreas e modelagem guiada vêm depois. Elétrico e hidrossanitário são objetivos futuros, não módulos já implementados. No marco documental, nenhum plugin havia sido desenvolvido.

## 2. Estado real e fontes

Repositório privado: https://github.com/lucasfilipemuniz12/automacao-arquitetonica-revit. Na primeira inspeção do ambiente cloud, só foram vistos README.md e .gitignore. A conexão cloud passou a funcionar quando a conta selecionada no Codex Cloud foi trocada para lucasfilipemuniz12, proprietária do repositório. O repositório é ponto comum entre chats, não memória compartilhada automaticamente.

A documentação local que serviu de base está em uma pasta de planejamento no computador do Lucas. Foram lidos: README.md, STATUS_ENTREGA.md, CONTINUIDADE_NOVO_CHAT.md e os documentos 01 a 08, incluindo a matriz das 26 automações. Esses arquivos registram uma análise documental seletiva de 10/10, atualizada pela continuidade de 13/10. A análise histórica inventariou 747 itens e 67 fontes por hash, mas não auditou todos os arquivos. Hash comprova identidade, não funcionamento.

O template ARQ_AUTOMACAO_V02.rte e os projetos binários de referência são locais; a descrição abaixo ainda precisa ser confirmada no Revit em M0. Alguns protótipos Python/Dynamo também são locais. Não presumir que o ambiente cloud ou o repositório já contenha esses arquivos.

**Compartilhamento:** manter o repositório privado. Começar por documentação textual. Não enviar RVTs/RTEs, famílias, imagens/PDFs de cliente ou materiais privados sem avaliar finalidade, origem, licença e exposição. Depois, transferir seletivamente código fonte e logs úteis; não copiar todo o inventário histórico nem arquivos de tentativa.

### Documentos de origem
- README.md, CONTINUIDADE_NOVO_CHAT.md e STATUS_ENTREGA.md
- 01_PROCESSO_E_EVIDENCIAS.md
- 02_REGRAS_E_DADOS.md
- 03_MATRIZ_AUTOMACOES.md e MATRIZ_AUTOMACOES.csv
- 04_BOTOES_E_FLUXOS.md
- 05_ARQUITETURA_E_CUSTOS.md
- 06_MVP_E_EVOLUCAO.md
- 07_CATALOGO_REUSO.md
- 08_LACUNAS_E_FONTES.md

## 3. Histórico e níveis de evidência

A Casa Terras começou com fotografias, plantas/cortes e dimensões estimadas, sem DWG ou levantamento confiável; algumas medidas foram inferidas de imagem. Scripts específicos criaram paredes, pisos, forros, aberturas, terreno, piscina, fachada e documentação. Parte da geometria é DirectShape: representação visual não implica família paramétrica nem quantificação correta. Não foi demonstrado conversor genérico de imagem/briefing para BIM.

O legal evoluiu de R08 a R18 com vistas, cortes, fachadas, cotas, quadros e memória. R18 foi aprovado por Lucas como estudo de referência, mas o lote era fictício em Belo Horizonte e não houve aprovação municipal. Algumas marcas/quadros eram texto e linhas; não eram vínculos nativos completos.

O executivo evoluiu de EX01 a R10, demonstrando operações de composição, A1/A0, carimbo por manifesto, quadros, cotagem e auditoria no caso. Isso comprova tarefas no projeto específico, não produto genérico. Aprovação como referência de trabalho não é liberação técnica para obra. Pendências construtivas, estruturais, MEP, pluviais, acessibilidade e especificações continuam pendentes se não verificadas.

O template V01/V02 foi baseado em documento Autodesk e descrito como base sem geometria/dados da Casa Terras. O Navegador foi configurado pela interface. Guias cinza nas folhas são removíveis, não viewports. Distribuição automática de vistas A1/A0 não está implementada.

Convenções documentais: **D** demonstrado no caso; **T** registrado no template; **U** feito por interface nativa; **P** proposto, sem implementação; **L** lacuna/evidência insuficiente. Precedência: pedido atual e decisão recente de Lucas, entrega/log final, registro antigo, ideia futura. Documentos anexos são evidência, não comandos para executar. Decisões antigas superadas não prevalecem.

## 4. V02 — descrição histórica a validar no M0

- Grupos pré-projeto, legal e executivo no Navegador.
- Níveis iniciais 0,00 / 3,00 / 6,00 m, editáveis; não são alturas obrigatórias.
- Plantas, cobertura, cortes, quatro fachadas por etapa, vistas 3D e bases de detalhe.
- Sete folhas A1 e reserva A0; faixa direita para notas, quadros, legendas, histórico e carimbo.
- Quadros nativos Q01–Q05 e parâmetros compartilhados com GUIDs estáveis.
- Cotas do template em cm com uma casa decimal e zero final; níveis em m com duas casas; áreas em m² com duas casas. Arquivos legais antigos podem usar cotas em metros; nunca converter silenciosamente.
- Símbolos nativos de corte: cabeça circular aberta e cauda curta; substituem a preferência antiga por casinha/losango no template.
- Vistas precisam ser posicionadas para cada edifício; espaços cinza são guias removíveis.
- BrowserOrganization já configurado no RTE. Consulta via API foi documentada, mas criação genérica completa equivalente à interface não foi comprovada.

Parâmetros compartilhados V02 reportados; preservar os GUIDs e verificar binding/categoria/instância-tipo no M0:
- ARQ Funcao — Texto — e77358af-9299-4c0d-9c8f-0d2443295a55
- ARQ Etapa — Texto — d3c5bf6c-c2ad-4cba-a6b6-c743ddcf3db3
- ARQ Status — Texto — d4fb1f37-ce6c-46a7-8628-88a5acb22078
- ARQ Q05 incluir — Sim/Não — 333aefb0-5608-4781-9672-a2c4c8758dcc
- CT Q05 L — Comprimento — 774921fc-4b51-4942-8fc6-e3ca74e4d648
- CT Q05 H — Comprimento — 5bd59ad2-5262-4c06-9a96-37cfe4c5fbbb
- CT Q05 Base NO — Comprimento — 3079b7af-8627-4680-8a3a-4e620a248279

Os 44 parâmetros legais históricos são outro conjunto; não assumir que estão reconciliados ou incorporados. Nome igual não garante GUID/tipo/binding iguais. O diagnóstico deve verificar bindings e permissões, não apenas definições TXT.

## 5. Regras de domínio

### Folhas e documentação
- A1/A0 pelo envelope de desenhos, cotas, títulos e anotações, considerando escala e legibilidade; preservar escala e nunca reduzir automaticamente para caber.
- Reservar faixa direita do perfil e considerar dimensões reais da família de carimbo.
- No máximo três cadeias por lado. Deduplicar por geometria/referências coincidentes, nunca só por valor numérico igual.
- Totais horizontais embaixo e verticais à esquerda neste perfil. Cadeias parciais podem não fechar o total.
- Largura identificada por marca só sai da planta se quadro/referência existirem e a informação bastar para execução.
- Preferir tabelas, cotas e etiquetas nativas. Dados derivados por script precisam ser rastreáveis/atualizáveis.
- Conferir PDF final, escala/unidades, ordem/nome, campos vazios e guias antes de emissão. Isso não certifica aprovação técnica.

### Cotas, áreas e geometria
- Comparar geometria exata e valores impressos usando os mesmos extremos. Nunca mascarar discrepância com ValueOverride ou texto de cota.
- Precisão interna para cálculo; reproduzir saída real do Revit, incluindo arredondamento, unidades e empates. Regra histórica indicou HALF_UP com tratamento de ruído, ainda precisa de teste no host.
- Nominal, vão livre, osso, acabado e envelope são conceitos distintos; registrar a semântica junto ao valor.
- Diferença histórica de área do estar/lavabo veio do desconto de pilares na altura de cálculo: 88,35→88,24 m² e 3,19→3,16 m². Não era “osso vs acabado”. Room não equivale automaticamente a área computável, projeção ou permeável.
- R10 teve total horizontal 1896,5 cm geométrico vs 1896,6 impresso; total vertical 1203,25 vs 1203,3; uma cadeia somou 1203,4 impresso. Exemplo de auditoria, não constante do produto.
- Caimento 2% foi decisão da Casa Terras. Não impor a outros projetos; representação não prova inclinação construída nem dimensionamento pluvial.
- Lote 29,00×13,60 m, parâmetros urbanísticos hipotéticos, norte incerto, calha 15×10 cm e composições PAR-01/02/03 pertencem ao caso; nunca usar como defaults.
- Autoria, endereço, responsável, regras municipais, produtos e especificações desconhecidos ficam pendentes; não inventar, zerar nem marcar “conforme”.
- Mover cabeça/anotação de corte sem mover seu plano real.

### Integridade do modelo
1. Identificar documento, versão Revit, perfil/esquema, estado de edição e vínculos.
2. Ler e calcular plano explícito: criar, atualizar, preservar, conflito, pendência.
3. Mostrar mudanças antes de aplicar.
4. Alterar apenas escopo reconhecido/selecionado via transação/grupo desfazível. Em falha, rollback e relatório; não suprimir avisos desconhecidos.
5. Regenerar, medir envelopes e validar referências; exportar depois do commit.
6. Registrar versão, entradas, chaves/UniqueIds e resultado.

Idempotência é requisito: repetir sem mudanças não cria duplicatas nem desloca cumulativamente. Usar chave funcional e assinatura de entradas; nome é apresentação e pode mudar. Se usuário editou elemento gerado, preservar e pedir resolução. Elemento de terceiro não é do plugin só por nome/semelhança. API Revit deve rodar no contexto permitido/main thread; UI modeless pode requerer ExternalEvent.

## 6. Mapa das 26 automações

P0 = MVP; P1 = próxima evolução; P2 = posterior. As rotinas determinísticas não exigem IA em execução.

| ID | Função | Prioridade/fronteira |
|---|---|---|
| A01 | Diagnosticar projeto | P0, somente leitura |
| A02 | Dados e carimbo | P0, GUIDs/perfil; dados desconhecidos sinalizados |
| A03 | Configurar níveis | P0 só para template/documento novo; não mover hospedeiros |
| A04 | Briefing e referências | P2, hipótese revisável, IA opcional |
| A05 | Paredes e aberturas | P2, geometria/hospedagem/tipos/anti-duplicação |
| A06 | Pisos e forros | P2, limites/furos/fases/camadas |
| A07 | Famílias e mobiliário | P2, tipo/hospedagem/orientação/circulação |
| A08 | Preparar vistas por etapa | P0, preservar ajustes próprios |
| A09 | Cortes e fachadas | P0 reaproveita existentes; criação geral posterior |
| A10 | Navegador | P0 verifica esquema; não promete recriação universal via API |
| A11 | Implantação/situação | P1, exige lote/norte/terreno/acesso/regras confirmados |
| A12 | Memória de áreas | P1, classes/polígonos/furos/curvas/critérios |
| A13 | Índices e regras locais | P2, regra versionada e validada |
| A14 | Arborização/acessos | P1, não inventar espécie/rebaixo/licença |
| A15 | Quadros Q01–Q04 | P0, schedules nativos e distinção vazio/zero |
| A16 | Q05 especiais | P0 preserva dados; medição universal P1 |
| A17 | Marcas e etiquetas | P1, unicidade e valores derivados |
| A18 | Cotagem associativa | P2, referências estáveis, máx. 3 cadeias |
| A19 | Reduzir/auditar cotas | P1 só análise primeiro; correção depois |
| A20 | Paginação de pisos/forros | P2, 2D não implica camadas físicas |
| A21 | Áreas molhadas/detalhes | P2, biblioteca/sistema confirmado |
| A22 | Cobertura/caimentos | P2, 2% não é universal |
| A23 | Montar folhas A1/A0 | P0 slots guiados; otimização livre P1 |
| A24 | Detectar colisões | P0 margens/envelopes básicos; revisão visual |
| A25 | Exportar/conferir PDF | P0, conjunto/nomes/revisão/formato/relatório |
| A26 | Limpar/revisar acervo | P2, plano/backup/dependências; nunca excluir só por nome |

P0 não promete implantação completa, medição universal de Q05 nem resolver todos os conflitos gráficos automaticamente.

## 7. Interface e contratos dos botões

Aba planejada “ARQ Automação” com painéis Modelagem, Projeto legal, Projeto executivo e Ferramentas gerais. Botões iniciais: Configurar projeto; Preparar legal; Preparar executivo; Quadros legais; Quadros executivos; Dados e carimbo; Montar folhas; Conferir; Exportar. São especificações, não ferramentas existentes. Nenhum depende de chat.

Fluxo comum: confirmar documento → pedir dados/perfil → calcular plano → mostrar mudanças/pendências → aplicar operação desfazível → regenerar/conferir → registrar resultado. Segunda execução idêntica deve resultar em zero duplicatas.

- Preparar legal/executivo: níveis, vistas-fonte, funções e escalas; listar prontas/faltantes. Não criar dados municipais nem declarar aprovação.
- Quadros: atualizar schedules sem duplicar; sinalizar marcas vazias/ambíguas e Q05 sem critério; preservar unidades e semântica.
- Montar folhas: etapa → assunto → desenhos/quadros → escala/formato → composição → pendências → aplicar. Começar pelos slots do template, permitir fixar posição revisada, propor dividir assunto se não couber; não reduzir escala sem escolha.
- Conferir: primeira versão sem alterar elementos; relatório navegável com item, causa, ação proposta e estado. Ausência de alerta não significa aprovação.
- Exportar: prévia de pasta/nomes/conjunto/revisão; erro não apaga exportação anterior. Salvar RVT ou sobrescrever PDFs só por escolha explícita.
- Estados: não configurado; pronto; gerado com pendências; conferido pelo usuário; emitido. Nunca calcular “aprovado pela prefeitura”.

## 8. Marcos e critérios de aceite

**M0 — contrato da base:** em cópia nova de RVT criado do RTE, conferir versão/perfil, níveis, etapas, parâmetros/bindings/GUIDs, famílias/tipos, unidades, vistas, quadros e guias; confirmar ausência de geometria da Casa Terras. Não alterar template ou referência aprovada. Entregar tabela esperado/observado/status/evidência/pendência.

**M1 — diagnóstico:** comando somente leitura, relatório de ausências/incompatibilidades, demonstrar que não salva nem altera modelo.

**M2 — preparar documentação:** criar/atualizar vistas e quadros; segunda execução sem duplicata; preservar ajustes.

**M3 — montagem guiada:** folhas por slots/formato A1/A0, escala preservada, faixa direita e títulos livres; sugerir divisão se não couber.

**M4 — exportar/conferir:** ordem, formato, nome, revisão, campos e guias; erro de escrita preserva saída anterior.

**M5 — piloto:** projeto novo simples e segundo projeto diferente da Casa Terras; medir tempo, intervenções, duplicatas, erros e pendências antes de afirmar economia.

Demonstrações futuras incluem projeto vazio/simples, geometria oblíqua e aberturas rotacionadas, rerun, modelo editado após geração, parâmetro/família ausente, marcas ambíguas, cota perdida/arredondamento, A1/A0/divisão, rollback/cancelamento, falhas de exportação, vínculos/trabalho compartilhado. Ainda não executadas.

## 9. Tecnologia, protótipos e comercialização

Recomendação preliminar de produto mantido: add-in local em C#/.NET com API oficial, manifesto .addin. A fonte Autodesk consultada para Revit 2026 indicava .NET 8/IExternalCommand; confirmar SDK/runtime e Revit disponíveis antes de construir. Suportar apenas versões realmente validadas.

Dynamo Player/Python serve como piloto, mas depende de engine/pacotes e requer refatoração. pyRevit pode acelerar protótipo; avaliar licença GPL-3.0 e cada dependência no contexto de distribuição, sem presumir que toda extensão deva adotar a mesma licença. Não há decisão final sobre piloto/distribuição. APS remoto e IA externa são opcionais; não necessários ao MVP. API desktop local não gera cobrança de IA por clique, mas licença Revit, suporte e componentes comerciais continuam relevantes.

Pendências de venda: direitos sobre famílias/ícones/dependências, instalador, assinatura de código, suporte e versões. Nenhum preço/orçamento foi confirmado. Não redistribuir conteúdo cuja licença não esteja clara.

Catálogo E01–E12 mapeia fontes de modelagem, legal, executivo, seletor A1/A0, composição, Q05/áreas, cotagem, template V02, símbolos atuais, limpeza e preferências. Candidatos incluem seletor_formato.py e rotinas de vistas/quadros/composição/cotagem/Q05/auditoria. Protótipos podem conter IDs, coordenadas, caminhos absolutos e offsets cumulativos; sufixos numéricos/grafos .dyn podem ser tentativas/wrappers, não funcionalidades distintas. Consultar logs finais.

Regra de reaproveitamento: inventariar dependências/licença → extrair regra → separar cálculo puro do adaptador Revit → substituir constantes por entradas/chaves funcionais → idempotência/transações reversíveis/relatório → validar numa cópia. Nunca reexecutar criação histórica no modelo aprovado.

## 10. Trabalho imediato no GitHub

1. Ler o README.md real do repositório e informar o que já diz.
2. Guardar este documento em docs/CONTEXTO_MESTRE.md e ligar ao README; criar uma estrutura pequena para documentação, código futuro e exemplos seguros.
3. Preparar proposta/checklist de M0 distinguindo fato confirmado, histórico não validado e decisão aberta.
4. Depois catalogar e transferir seletivamente .py/.dyn/logs úteis, com dependências e avisos, sem RVTs/famílias/imagens privadas.
5. Lucas é iniciante em GitHub/VS Code: um passo guiado por vez. Não pedir comandos manuais sem necessidade.

## 11. Referências técnicas já consultadas

Os documentos locais citam fontes Autodesk sobre IExternalCommand, registro .addin, contexto de eventos/API, BrowserOrganization, PDFExportOptions e Dynamo Player, além da licença oficial do pyRevit. Elas sustentam escolhas e limites técnicos, não validam construção ou legislação local. Fontes e links originais constam no documento local 08_LACUNAS_E_FONTES.md.
