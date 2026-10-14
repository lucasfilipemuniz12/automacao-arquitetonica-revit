# Passagem para revisão independente — Claude

Atualizado em 14/10/2026. Produto VEBRAM 0.5.14 para Autodesk Revit 2026.4, C#/.NET 8/WPF. Esta passagem descreve o estado comprovado e os limites da entrega.

## Código e versão

- Repositório privado: [automacao-arquitetonica-revit](https://github.com/lucasfilipemuniz12/automacao-arquitetonica-revit).
- Branch de trabalho: **codex/vebram-projeto-legal-v03**.
- [PR 1](https://github.com/lucasfilipemuniz12/automacao-arquitetonica-revit/pull/1), em rascunho, sem merge. Confira a branch: main é outra base.
- Commit da composição 0.5.14 validada: d74ad7fd175c82688195f1ab2d2dc59d46ec2d26.
- A passagem acrescenta uma rotina de desenvolvimento para gerar a cópia só de modelagem. Não muda os botões de geração legal nem a versão de produto.
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
| ModelReviewExport.cs | Utilitário de desenvolvimento: cópia só modelagem e imagens/inventário |
| scripts/Build.ps1, Install-Local.ps1 | Compilação/instalação fixa com Revit fechado |
| scripts/Validate-Local.ps1 | Prepara cópia e job; nunca deve apontar para projeto aprovado |
| scripts/Export-ModelReview.ps1 | Prepara cópia nomeada CASA_TERRAS_SO_MODELAGEM.rvt e job modelreview |
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

## RVT só de modelagem

O arquivo local CASA_TERRAS_SO_MODELAGEM.rvt é a base limpa para inspecionar a casa. Folhas, tabelas, cortes, fachadas e vistas de Projeto legal/Executivo foram retirados nessa cópia. Mantêm-se plantas de modelagem e uma vista 3D, com classificação 00 MODELAGEM no navegador.

Famílias, materiais, níveis, ambientes, parâmetros compartilhados e templates de vista são recursos preservados. Uma vista-template mantida não é um entregável legal criado. Os estados de geração VEBRAM são removidos. A origem documentada continua separada.

INVENTARIO_MODELAGEM.json informa as vistas restantes, categorias, elementos e preservação dos identificadores/tipos/caixas físicas. As imagens PNG são exportações nativas, para que o revisor veja a casa sem depender de abrir o binário RVT.

Essa base limpa não contém as vistas de referência do template V02 exigidas por parte dos fluxos existentes. Não espere executar os testes legal05/composition014 diretamente nela. Avaliar como o plugin deve preparar uma base modelada sem referências é um tema relevante da revisão.

## Pontos que merecem revisão

- Dependências de nomes, papéis, GUIDs, carimbo/formato e convenções específicas do V02; tratamento de referências ausentes.
- Propriedade/fingerprint de anotações; proteção de conteúdo manual e semântica de substituir edições.
- Reversão de transações, falhas do Revit, migração de marcadores, dependências ao transferir viewports.
- Unidades internas em pés versus metros, mm de papel e escala; caixas de texto e centralização de títulos.
- Escopo real dos testes versus casos não cobertos: outros terrenos, casas, fases, níveis e famílias.
- Composição em linhas predefinidas, preservação de Q04/carimbo e recusa quando não cabe.
- Coesão e legibilidade do código; separar rotina de desenvolvimento do produto se apropriado.
- Preparação de uma modelagem limpa para gerar documentação sem depender de vistas já presentes.

## Pendências de produto

Cotagem arquitetônica completa; classificação explícita geral de piscina/deck/jardim e outros elementos; adaptação a outros projetos/templates/formatos; revisão de dados técnicos e carimbo para emissão; exportação PDF. Situação é um diagrama, não um levantamento cadastral.

## Execução local e limites do ambiente

Compilação do produto requer Windows, .NET 8 e Revit 2026 instalado com RevitAPI.dll/RevitAPIUI.dll. Referências Autodesk são locais e não são redistribuídas. Testes nativos precisam executar dentro do Revit. Um clone num ambiente web não oferece esse runtime.

Instalação fixa: current/Vebram.Revit.dll. AddInId 936f6231-d0c9-47a3-83e6-65813c46c176. Fechar Revit normalmente antes de atualizar; verificar liberação exclusiva; preservar DLL/PDB/deps da mesma compilação e backup. Não criar carregadores temporários nem trocar GUID para contornar bloqueios. A decisão de carregar complemento não certificado pertence ao usuário.

Não há conexão MCP com o Revit comprovada nesta entrega. GitHub conectado não concede controle local do Revit. Eventual ponte local deve ser instalada/configurada e testada separadamente.

Para começar a revisão, use o texto de PROMPT_PARA_CLAUDE.txt da entrega local.
