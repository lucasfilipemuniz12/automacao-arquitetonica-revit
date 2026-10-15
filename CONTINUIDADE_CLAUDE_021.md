# Continuidade para Claude — VEBRAM 0.5.21 (15/10/2026)

Código local: C:\Users\lucas\Documents\Codex\2026-10-13\files-pasted-by-the-user-continuidade\outputs\PluginRevit\src\Vebram.Revit
Pacote/DLL instalada: mesma raiz, pasta current. Revit 2026, .NET 8; SDK local em work\sdk\dotnet\dotnet.exe dentro da pasta de trabalho de 13/10. Manifesto em AppData\Roaming\Autodesk\Revit\Addins\2026 aponta para current. Fechar o Revit antes de instalar DLL; Build.ps1 registra binlog e protege contra DLL em uso.

## Entregue e validado
- 0.5.19: consolidação adaptativa de assuntos compatíveis, prévia antes de aplicar, preservação de escalas, grupos de cortes/fachadas. Nesta casa, três folhas utilizadas. Folhas existentes desocupadas ficam como reserva; novas folhas desnecessárias são removidas dentro da transação. Não é otimização global exaustiva.
- 0.5.20: quadros nativos de portas/janelas, código por Marca, largura, altura, quantidade e peitoril. Família/tipo oculto para compactar. Todos os níveis e fase/filtro da planta de origem na criação. Reexecução preserva tabela personalizada. Integração ao catálogo/organizador; quadros de esquadrias preferencialmente na lateral.
- 0.5.21: textos P/J de 2mm na geração da Planta legal, sem alterar Marcas. Inclui elementos de Portas/Janelas que não são FamilyInstance. Evita textos/nomes de ambientes e tags manuais vinculadas. São textos gerados, não tags associativas: reexecutar a Planta legal depois de mudar códigos ou posições. Edições manuais são protegidas pelo fingerprint de LegalModules.
- Diagnóstico agora lê células reais dos quadros e identifica P08/P09/P10/P13 com largura/altura vazia. Não inventar medidas a partir da caixa envolvente. O filtro anterior por FamilyInstance excluía esses quatro elementos especiais.

## Evidência e pendências
Compilação 0 erros/avisos. Teste nativo final em PluginRevit\teste-identificacoes-v0521\nativo-02, ESQUADRIAS_021.json e RELATORIO.txt: 15 identificações, repetição estável, proteção de texto editado, preservação das Marcas e geometria, diagnóstico de células vazias, três folhas utilizadas.
P11/P12 ficaram sem identificação por falta de espaço próximo; algoritmo relata a pendência. A busca ainda não evita todo cruzamento com linhas de modelo, cotas e símbolos. Não declarar a planta ou o Projeto legal inteiramente concluídos.
Os quadros funcionam e estão na lateral, mas as quatro portas acima continuam com dimensões vazias. É pendência de dados/compatibilidade dos elementos, não diferença de cálculo a corrigir mudando a casa.
RVT de revisão: C:\Users\lucas\OneDrive\Área de Trabalho\ARQ TESTE\REVISAO_VEBRAM_V0521_IDENTIFICACOES\CASA_TERRAS_IDENTIFICACOES_V0521.rvt. A pasta contém PNGs e instruções. Nesta rodada, LEG04 recebeu plantas/fachadas; LEG02 ficou de reserva.

## Próxima etapa recomendada
1. Fechar a identificação P11/P12 e revisar leitura das cotas/chamadas do térreo. Tratar os dados das quatro portas especiais com origem explícita e sem medidas inventadas.
2. Implementar memória gráfica colorida de áreas: contornos e classes físicas sem sobreposição, legenda e tabela coerentes com o modelo, diferenciando área útil de ambiente, projeção, cobertura, piscina/deck, vegetação e impermeáveis. Separar classes físicas de critérios municipais. Parâmetros urbanísticos atuais foram preenchidos arbitrariamente para teste; não validá-los nem copiá-los como verdade.
3. Completar informações laterais/carimbo com dados explícitos do projeto e notas específicas, preservando o template V02 e os campos PRE/LEGAL/EX.
4. Deixar detalhes construtivos, materiais, marcenaria e ampliações para o executivo. O foco atual é um legal legível e fácil de usar.

Referência visual original: C:\Users\lucas\OneDrive\Área de Trabalho\ARQ TESTE\CASA_TERRAS_EXECUTIVO\Pranchas_LEGAL_R20\CT-ARQ-LEGAL-CONJ-R20.pdf. Página 1: situação/memória gráfica e informações laterais; 2: térreo/esquadrias; 3: implantação/cobertura/cortes; 4: fachadas/cortes complementares. Não está confirmado que o RVT atual seja o mesmo usado nesse PDF. Não copiar áreas/dimensões do PDF para o modelo atual.

Arquivos principais: SheetPlanner.cs, SheetConsolidation.cs, SheetPacking.cs, SheetPlannerWindow.cs, OpeningSchedules.cs, PlanAnnotations.cs, LegalModules.cs, PrepareWindow.cs. Validação: OpeningSchedulesValidation.cs. Documentação: IDENTIFICACOES_ESQUADRIAS_021.md e QUADROS_ESQUADRIAS_020.md.
Teste de inicialização somente em cópia local, via job explícito openings020 (cobre a 0.5.21). O job não executa no projeto do usuário sem solicitação explícita. Preservar modelo, escalas, vistas do template e edições manuais. Usuário autorizou Sempre carregar para VEBRAM; o aviso pode reaparecer após recompilar.

GitHub: esta entrega consolida .19/.20/.21 sobre a branch codex/vebram-zonas-laterais-v0518 (PR5). Usar a nova branch codex/vebram-esquadrias-v0521 como ponto de partida, não main nem a antiga branch Claude v0516. Não mesclar PRs automaticamente. RVT/DLL/backup/binlogs permanecem locais; o repositório recebe fontes, documentação e relatórios de validação.
