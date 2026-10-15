# Passagem para revisão independente — Claude

Atualizado em 14/10/2026. Produto VEBRAM 0.5.14 para Autodesk Revit 2026.4, C#/.NET 8/WPF. Esta passagem descreve o estado comprovado e os limites da entrega.

## Código e versão

- Repositório privado: [automacao-arquitetonica-revit](https://github.com/lucasfilipemuniz12/automacao-arquitetonica-revit).
- Branch de trabalho: **codex/vebram-projeto-legal-v03**.
- [PR 1](https://github.com/lucasfilipemuniz12/automacao-arquitetonica-revit/pull/1), em rascunho, sem merge. Confira a branch: main é outra base.
- Commit da composição 0.5.14 validada: d74ad7fd175c82688195f1ab2d2dc59d46ec2d26.
- A passagem acrescenta uma rotina de desenvolvimento para conferir a base no template original. Não muda os botões de geração legal nem a versão de produto.
- A cópia local de fontes acompanha a entrega como alternativa à sincronização. Não contém DLLs Autodesk, binários, modelos ou segredos.

## Objetivo do produto

Automatizar documentação arquitetônica a partir de uma modelagem existente. Prioridade atual: Projeto legal, usando o contrato do template ARQ V02. O usuário seleciona uma planta fonte, configura os dados e pede entregáveis em Preparar legal. Não há classificação geral de elementos por IA nem aprovação municipal automática.

Implementado: implantação, plantas legais com rótulos e cotas possíveis, cortes A/B, cobertura com declividade informada por água plana, quatro fachadas nativas, diagrama de Situação, Q04 de áreas, quadro urbanístico e colocação em folhas.

Revisar composição é opcional, para V02 A1 horizontal com entregáveis já colocados: LEG-01 planta, Situação e quadro; LEG-02 cobertura e quatro fachadas; LEG-03 implantação e cortes. Quadro de 180 mm, texto 2,5 mm, guias ocultas, títulos/números centralizados. Preserva 1:100 nas vistas principais, 1:500 na Situação e 1:1 no quadro. Não organiza outras folhas automaticamente.

## Mapa de leitura

| Arquivo/pasta | Papel |
|---|---|
| App.cs, Commands.cs, PrepareWindow.cs | Ribbon, comandos e interface WPF |
| LegalCatalog.cs, LegalPreparation.cs | Catálogo de entregáveis e contrato ARQ V02 |
| Implantation.cs | Lote confirmado, contorno, implantação, persistência/propriedade de anotações |
| LegalModules.cs, PlanAnnotations.cs | Plantas e anotações |
| LegalSections.cs, LegalFacades.cs | Cortes e elevações nativas |
| LegalContext.cs, LegalMetrics.cs | Situação, quadro e cálculos urbanísticos |
| LegalSheets.cs, LegalSheetComposition.cs | Colocação e revisão explícita de composição |
| LegalPreferences.cs | Preferências legais |
| SectionValidation.cs | Jobs nativos explícitos em cópias de teste |
| ModelReviewExport.cs | Utilitário de desenvolvimento: novo RVT pelo RTE do usuário, apenas modelagem transferida, prévias temporárias revertidas |
| scripts/Build.ps1, Install-Local.ps1 | Compilação/instalação fixa com Revit fechado |
| scripts/Validate-Local.ps1 | Prepara cópia e job; nunca deve apontar para projeto aprovado |
| scripts/Export-ModelReview.ps1 | Job templatereview cria novo projeto do RTE e transfere apenas a casa, verifica geometria e preserva vistas originais |
| tests/Arithmetic | Testes sem Revit de cálculos e retângulos |
| docs/TESTES_* | Evidências e limitações por etapa |

Os nomes são relativos ao repositório. Confirme a existência das classes no código; alguns auxiliares compartilham arquivos.

## O que já foi verificado

- Compilação 0.5.14: zero erros/avisos com binlog único.
- Cortes, quatro fachadas e Situação revisados nativamente e visualmente nas etapas anteriores; veja docs/TESTES_CORTES_V044.md e docs/TESTES_FACHADAS_SITUACAO_V0511.md.
- Quadro e colocação dos doze entregáveis validados na 0.5.13; veja docs/TESTES_URBANISMO_FOLHAS_V0513.md.
- Composição 0.5.14: repetição estável, três folhas, títulos dentro das margens, sem colisão de viewports/textos do quadro, vistas/escalas/limites físicos preservados. As três folhas foram revistas no Revit. Veja docs/TESTES_COMPOSICAO_V0514.md.
- Dados urbanísticos dos testes são fictícios. Caixas físicas preservadas e repetição são verificações específicas; não equivalem a prova completa de integridade de toda a geometria ou de conformidade legal.
- Não há alegação de funcionamento geral em qualquer edifício/template.

## RVT correto: casa no template V02, antes da automação

A entrega correta é BASE_TEMPLATE_V02/CASA_TERRAS_TEMPLATE_V02_PARA_AUTOMACAO.rvt. Ela é um projeto novo criado diretamente do ARQ_AUTOMACAO_V02.rte atual do usuário, com apenas a modelagem da Casa Terras e os ambientes transferidos. Preserva Projeto legal e Projeto executivo, suas vistas de referência, tabelas, folhas, presets, parâmetros e agrupamentos. Não representa documentação pronta: os cortes, fachadas, cotas, anotações e composição ainda deverão ser preparados pela automação.

As vistas existentes podem mostrar automaticamente a geometria da casa. “Não preparadas” não significa “obrigatoriamente vazias”. Q04 e outros quadros nativos também podem listar os ambientes automaticamente. Preservar esse comportamento faz parte do uso do template.

O RTE original acompanha a base local. INVENTARIO_BASE_TEMPLATE.json compara o conjunto de vistas/presets/folhas com esse RTE e compara a modelagem com o projeto atual; também verifica ausência de estado de geração VEBRAM. As prévias PNG mostram plantas e 3D; a vista 3D temporária de exportação é revertida, sem acrescentar vistas à base entregue.

A primeira interpretação retirava todas as vistas legais/executivas. A base antiga também apresentou estado VEBRAM na inspeção; por isso a entrega final foi criada diretamente do RTE atual. Lucas esclareceu que devem existir como referências originais. Aquela cópia foi descartada da entrega e esta é a base correta para testar a automação. O utilitário agora cria a base pelo RTE atual e transfere apenas elementos de modelo/ambientes, sem importar ou preparar documentação. Não apaga as vistas do template. A transferência gera IDs novos; compara categorias/contagens/caixas físicas e áreas dos ambientes, preservando as referências do template.

## Pontos que merecem revisão

- Dependências de nomes, papéis, GUIDs, carimbo/formato e convenções específicas do V02; tratamento de referências ausentes.
- Propriedade/fingerprint de anotações; proteção de conteúdo manual e semântica de substituir edições.
- Reversão de transações, falhas do Revit, migração de marcadores, dependências ao transferir viewports.
- Unidades internas em pés versus metros, mm de papel e escala; caixas de texto e centralização de títulos.
- Escopo real dos testes versus casos não cobertos: outros terrenos, casas, fases, níveis e famílias.
- Composição em linhas predefinidas, preservação de Q04/carimbo e recusa quando não cabe.
- Coesão e legibilidade do código; separar rotina de desenvolvimento do produto se apropriado.
- Preparação dos entregáveis aproveitando as vistas já existentes no template do usuário, sem tratá-las como documentação finalizada.

## Pendências de produto

Cotagem arquitetônica completa; classificação explícita geral de piscina/deck/jardim e outros elementos; adaptação a outros projetos/templates/formatos; revisão de dados técnicos e carimbo para emissão; exportação PDF. Situação é um diagrama, não um levantamento cadastral.

## Execução local e limites do ambiente

Compilação do produto requer Windows, .NET 8 e Revit 2026 instalado com RevitAPI.dll/RevitAPIUI.dll. Referências Autodesk são locais e não são redistribuídas. Testes nativos precisam executar dentro do Revit. Um clone num ambiente web não oferece esse runtime.

Instalação fixa: current/Vebram.Revit.dll. AddInId 936f6231-d0c9-47a3-83e6-65813c46c176. Fechar Revit normalmente antes de atualizar; verificar liberação exclusiva; preservar DLL/PDB/deps da mesma compilação e backup. Não criar carregadores temporários nem trocar GUID para contornar bloqueios. A decisão de carregar complemento não certificado pertence ao usuário.

Não há conexão MCP com o Revit comprovada nesta entrega. GitHub conectado não concede controle local do Revit. Eventual ponte local deve ser instalada/configurada e testada separadamente.

Para começar a revisão, use o texto de PROMPT_PARA_CLAUDE.txt da entrega local.
