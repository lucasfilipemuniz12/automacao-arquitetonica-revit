# Estado atual — 13/10/2026

## Decisões de Lucas

Produto nativo em C#/WPF/API Revit, executado localmente. Trabalhar apenas em Projeto legal por enquanto. Interface: uma planta-fonte, botões ilustrados por entregável, configuração simples e geração da vista preparada para futura colocação na folha. Primeiro entregável: Implantação. Preferir elevações nativas para fachadas, permitindo referências alternativas de corte futuramente.

Lucas experimentou e aprovou a direção da Implantação. Pediu para ampliar os módulos e testar na Casa Terras, que é mais completa, além do projeto simples 10 × 15 m. Acrescentar escolhas práticas, incluindo estilo de cotas, escala e informação adicional.

## Estado real do template

Auditoria nativa confirmou comprimentos gerais em m, áreas em m², volumes em m³. Tipos com nome em metros estavam indevidamente em centímetros. Após pedido de padronização integral, 32 estilos de comprimento, inclusive internos, e 8 campos de comprimento das tabelas foram ajustados a metros. Tipos de ângulo/inclinação conservam unidades próprias; textos/papéis conservam unidades gráficas. O original V02 foi atualizado com backup e cópia corrigida. Projetos existentes não herdam automaticamente uma mudança no RTE.

## Implantação

Base Casa Terras migrada para o verdadeiro V02, conservando organização e referências. Piloto: lote fictício 29 × 13,60 m; quatro afastamentos coerentes com o estudo R18. Não são constantes do produto. Norte e via são dados informados. Cinco cotas nativas e 46 anotações foram registradas na versão anterior; repetição atualiza a mesma vista. Cotas ligadas a linhas geradas precisam de nova execução após mudanças do modelo. Não há montagem de folha neste comando.

Caimento de projeto de 2% pertence à decisão posterior R19/R20 da Casa Terras. Sua laje física é plana. Não aplicar esse valor universalmente. Na 0.3, telhados com várias águas planas têm inclinação e direção lidas por face. A proposta opcional de caimento da Implantação é restrita à cobertura principal plana de uma superfície. Telhados curvos e edição livre por água ainda não estão implementados.

## Novo lote 0.3

- Preferências legais: estilo de cota existente (com unidade visível), escala e nota adicional, persistidos no RVT.
- Planta legal: completa a referência do V02 com duas cotas gerais e textos de nome/área dos ambientes. Ainda não é cotagem interna completa; textos se atualizam quando o botão é executado novamente.
- Cobertura: completa a referência do V02 e lê as águas planas, caimento e direção pela geometria. Não cria nem remodela telhados.
- Áreas dos ambientes: abre o Q04 nativo existente e informa ambientes sem área; não inventa áreas computáveis nem resolve pendências de modelagem.
- Identidade das vistas mantida; anotações manuais externas ao gerador não são selecionadas para exclusão. Referências manuais dependentes das linhas geradas ainda precisam de proteção adicional. Edições de marcas próprias exigem decisão explícita de refazer.

A compilação da 0.3.1 concluiu com zero erros/avisos. Testes nativos 0.3.1 concluídos na Casa Terras: repetição, edição protegida, estilo escolhido e preservação dos IDs físicos/vistas passaram. A cópia foi salva. Evidências e limites em docs/TESTES_V03.md. A casa básica tem duas águas de 19,739556%, conferidas contra o contorno nativo; o fonte da rotina de criação foi corrigido para novas casas de 20%.

## Pendências

Cortes A/B, quatro fachadas, Situação, quadro urbanístico e folhas/exportação ainda não foram habilitados neste lote. Cotas internas, referências associativas diretas a todas as paredes/esquadrias, colisões de rótulos, múltiplos blocos, lotes girados/curvos e telhados curvos exigem evolução. A Casa Terras conserva pendências de áreas/sobreposições da migração. Nenhuma aprovação municipal foi afirmada.

## Ferramentas auxiliares

Skills Revit BIM, Architectural Civil Drafter e binlog-generation disponíveis. csharp-expert instalada do repositório oficial dotnet/skills, licença MIT; disponível na sessão atual. Nenhum conteúdo de skill incorporado ao produto.

BIMwright rvt-mcp/ToolBaker v1.1.0: pacote oficial baixado e SHA-256 conferido; instalação efetiva ainda pendente, porque o instalador exige fechar o Revit. O módulo funciona sem essa ponte. A instalação/conexão não devem ser declaradas concluídas pela existência do ZIP.

## Continuidade entre chats

Este repositório contém os fontes atuais e a documentação textual. Os arquivos RVT/RTE e famílias permanecem locais. Cloud pode revisar e editar C#, mas a validação da API exige Revit no Windows. Manter o histórico mestre; priorizar pedido recente de Lucas e evidência executada. Não reexecutar scripts históricos nos modelos aprovados.



## Atualização 0.3.2 — rótulos e cotas internas

Substitui a limitação de rótulos sobrepostos da 0.3.1. ROOM_NAME evita número duplicado; textos são distribuídos no interior ou enviados a uma coluna externa com chamadas. Na amostra da Casa Terras, quatro rótulos permaneceram internos e cinco receberam chamada; não houve sobreposição entre rótulos internos gerados no teste automatizado.

Planta final: duas cotas gerais + 16 cotas internas entre referências reais das faces de acabamento. Opções desligadas por padrão para reduzir conteúdo: espessuras (27 cotas no teste) e larguras de portas/janelas (14 cotas no teste). Valores e referências positivos conferidos; repetição, proteção de edição, estilo e preservação de IDs passaram. Pendências declaradas: duas medidas horizontais dos ambientes 01/03 não encontraram par ortogonal válido.

Fonte nativo compilado sem erros/avisos; versão instalada em dist-v032. Cópia local Casa_Terras_LEGAL_V032.rvt salva; evidências e limites em TESTES_V032.md. Revisão visual final aguarda recuperação do controle da janela Revit. Não afirmar que disposição das cotas/linhas indicadoras foi aprovada nem que cotagem de posicionamento de esquadrias está implementada.

## Correção 0.3.5 — prioridade de rótulos internos
Após a revisão de Lucas, removidas chamadas externas automáticas. Nove rótulos internos sem sobreposição de texto visível no teste. Correção da caixa de texto: Width/Height na escala, excluindo margem vazia da bounding box. Texto e quebra de linha priorizados antes das cotas. Duas cotas gerais + 11 internas; cinco internas deixadas como pendência por conflito de leitura e dois limites 01/03 já pendentes. Testes nativos passaram, cópia salva e versão instalada. Revisão visual pendente. Ver TESTES_V035.md; a versão atual substitui o resultado visual inadequado da 0.3.2.
