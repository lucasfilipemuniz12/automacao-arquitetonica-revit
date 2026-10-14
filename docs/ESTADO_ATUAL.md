# Atualização 0.5.13 — 14/10/2026

Quadro urbanístico e colocação nas folhas validados nativamente no Revit 2026.4. Quatro folhas abertas para revisão visual. Lucas escolheu manter 1:100 e acrescentar LEG-03/04. O quadro completo usa vista própria, preservando a referência do template e posições anteriores. Q04 reconhecido pelo catálogo. Nova folha e colocação são revertidas juntas se não couber.

Teste final 0.5.13.0: todos os 12 entregáveis de teste colocados; cálculos, pendências, repetição, proteção de edição, escala, reversão, posições e geometria preservados; sem novas colisões. Dados fictícios. Consulte [evidência e limites](TESTES_URBANISMO_FOLHAS_V0513.md).

A composição ainda exige acabamento antes de emissão: espaços-guia, campos do carimbo e sobreposição antiga entre Q04 e referência vazia na LEG-01. Cotagem completa, classificação explícita e adaptação geral a outros templates continuam pendentes. Não declarar todo o Projeto legal concluído.

Os registros abaixo são históricos.

---
# Atualização 0.5.11 — 14/10/2026

Fachadas corrigidas: quatro elevações nativas mostram a casa e foram revisadas visualmente na 0.5.9. Reexecução, direções e preservação dos limites físicos passaram. 0.5.10 oculta marcadores da Situação; 0.5.11 também oculta separações de ambientes que apareciam sobre o diagrama. Consulte TESTES_FACHADAS_SITUACAO_V0511.md para evidências e limites. A versão 0.5.11 está instalada e passou no teste nativo. A Situação foi revisada visualmente: lote, duas cotas, confrontantes, via e norte visíveis, sem marcadores ou separações sobrepostas.

Próximos módulos a validar: quadro urbanístico e composição das folhas legais. Classificação explícita de piscina/deck/jardim ainda é uma melhoria pendente; heurísticas atuais não equivalem a identificação por IA. Não declarar todo o Projeto legal concluído.

Os registros abaixo são históricos. As decisões mais recentes prevalecem.

---
# Retomada das fachadas — candidato 0.5.6

A validação nativa 0.5.5 foi finalmente executada: quatro elevações nativas, direções cardeais e repetição passaram; Situação produziu duas cotas e oito curvas com a categoria Lines ativa; limites físicos preservados.

Entretanto a revisão visual da fachada leste encontrou somente terreno e níveis, sem a casa. Portanto os testes de direção/contagem da 0.5.5 NÃO provam que a fachada está pronta. A categoria Paredes estava habilitada e o filtro de fase mostrava tudo. A disposição da situação ainda precisa de revisão visual.

Candidato 0.5.6 ajusta a profundidade local do CropBox: Min.Z = -profundidade e Max.Z = 0, em vez de conservar os limites herdados. Inclui GEOMETRIA_FACHADAS.json no teste, com origem/direção, transformação e limites do recorte, categorias, paredes presentes e caixas físicas projetadas. Compilado sem erros/avisos e instalado após fechamento das sessões e liberação exclusiva da DLL.

Teste correto do candidato está em teste-fachadas-v056-real/Casa_Terras_FACHADAS_SITUACAO_V056.rvt. A pasta teste-fachadas-v056 foi executada pela DLL 0.5.5 que ainda estava carregada num fechamento pendente; seu JSON registra 0.5.5 e não representa teste de 0.5.6. Nenhuma origem foi sobrescrita pelo job.

A reabertura da DLL nova voltou ao aviso Segurança — Complemento não certificado. A skill Computer Use impede operar esse aviso. A solicitação de carregamento está pendente com o usuário. O job da cópia 0.5.6 continua ativo para executar após o clique; não há teste aprovado ou imagem revisada desta versão ainda.

Próxima ação: após o carregamento, ler GEOMETRIA_FACHADAS.json e ERRO.txt/TESTES_FACHADAS_SITUACAO.json; abrir as quatro vistas e a Situação. Se a casa continuar ausente, comparar profundidade projetada dos elementos com o recorte e a origem do marcador antes de outra alteração. Não afirmar conclusão pela ausência de exceções ou por compilação.
---
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
---
# Atualização 0.5.4 — faixa do Revit e fachadas

Instalada após Lucas salvar e fechar Revit. Caminho/GUID fixos preservados, DLL anterior copiada para backup. SHA-256 atual: 087A1667249D772F901030883C8CB222BFE494E0893440706EC2B5A81EE99AAA. Compilação: zero erros e zero avisos. Sem job ativo de inicialização.

Ícones originais desenhados em WPF, com traços verdes iguais aos cartões dos entregáveis: planta/check para Preparar legal; controles para Preferências; tabela para Parâmetros; folha/casa para Demonstração V02; documento/lupa para Diagnóstico. Os ícones usam DrawingImage congelada e são atribuídos a LargeImage/Image de cada PushButton; não dependem de arquivos externos.

Corrigido o erro View cannot be duplicated: a rotina agora usa CreateViewTemplate na fachada já configurada em vez de chamar Duplicate no modelo de vista. Fachadas ausentes são criadas como elevações nativas com marcadores independentes, reaproveitando um perfil gráfico disponível. Um TransactionGroup engloba a recuperação e a preparação; uma falha reverte ambas. Fachadas compartilhadas continuam sendo preservadas em Reserva quando criadas as independentes.

Execução e revisão visual no Revit ainda pendentes. Não afirmar que compilação equivale a validação nativa. Reabrir o Revit e usar Preparar legal > Fachadas na cópia de teste. A rotina não exige que o usuário apague vistas existentes.

Pergunta sobre Implantação: identificação atual de piscina/deck/grama/garagem usa palavras nos nomes de Floor/DirectShape e nomes dos tipos, selecionando o maior candidato sobre o lote. Cobertura versus abrigo usa um limiar de envelope de 36 m². São heurísticas; não há reconhecimento por IA. Evolução recomendada: função explícita do elemento e seleção manual quando ausente. Essa evolução ainda não foi implementada.
---
# Correção 0.5.2 — marcadores compartilhados

O marcador de elevação do Revit pode controlar várias fachadas. A versão anterior bloqueava essa organização comum. Agora o gerador cria uma elevação nativa independente para cada fachada compartilhada e mantém as vistas originais, com seus IDs e marcações, no grupo 00 RESERVA VEBRAM. O novo conjunto recebe os nomes legais originais e o perfil gráfico; execuções seguintes reutilizam o conjunto independente.

Fachadas compartilhadas já colocadas em folhas e marcações manuais externas ao gerador continuam protegidas: o comando recusa migração automática nesses casos. A criação e a preparação ocorrem numa transação única; falhas revertem a migração.

Compilação: zero erros e zero avisos. Versão 0.5.2 instalada em current/Vebram.Revit.dll após Lucas salvar e fechar Revit, com backup da versão anterior e verificação de liberação exclusiva da DLL. SHA-256: 509F1F06034794BAE508BA5ECF4E991BFA8844A550CA2976D1D6488A07123F3A. Caminho e AddIn GUID preservados. Nenhum job de inicialização ativado.

Ainda requer execução e revisão visual no Revit; instalação/compilação não equivalem a teste nativo aprovado. Reabrir o projeto de teste e usar Projeto legal > Preparar legal > Fachadas.

Referência de API: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/2f707487-817c-d648-4b99-559f9000f123.htm
---
# Estado atual — candidato 0.5.1

Cortes 0.4.4 testados nativamente e revisados visualmente. Fachadas, Situação, quadro urbanístico e colocação em folhas implementados e compilados, com 19 testes de aritmética/encaixe passando; validação nativa dos módulos novos bloqueada pelo aviso de complemento não certificado. Não declarar que Projeto legal inteiro está concluído.

Leia docs/CONTINUIDADE_014_PROJETO_LEGAL.md. DLL completa em staging-v051; current ainda contém o primeiro candidato 0.5.0, bloqueado pela sessão no aviso. O RVT teste-legais-v05 é somente cópia dos cortes, sem os módulos novos. Não há job ativo de inicialização.

Notas abaixo são históricas.

---
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

## Cortes A/B 0.4.2 — 14/10/2026
Dois botões habilitados com piso-fonte, eixo X/Y, posição percentual, sentido e profundidade. Testes nativos passaram na Casa Terras preservando as duas ViewSection do V02: dois níveis e uma cota vertical em cada corte; repetição, posição 25%, inversão, profundidades 8/15 m e proteção de edição. Estado final centro/15 m. IDs de vistas, elevações e limites físicos preservados. Versão instalada e cópia salva; revisão visual pendente. Detalhes em TESTES_CORTES_V04.md. As pendências anteriores de cortes referem-se às versões antigas. Fachadas são a próxima fase.

Instalação padronizada em current/Vebram.Revit.dll com AssemblyName fixo Vebram.Revit e GUID 936f6231-d0c9-47a3-83e6-65813c46c176. Build.ps1 exige Revit fechado. Não voltar a criar carregadores/GUIDs novos para cada ajuste: essa prática provocou os avisos repetidos que Lucas relatou. A confiança Sempre carregar depende de mesmo caminho/ID para DLL não assinada. Fonte 0.4.2 recompilado com nome estável, sem erros/avisos; testes nativos anteriores usaram o mesmo código na V042.

## Correção de níveis 0.4.3
Após print de Lucas, textos duplicados dos níveis removidos; linhas nativas curtas de 0,9 m em faixa à esquerda, um símbolo por nível; cota vertical em faixa separada e nota inferior afastada. Testes nativos passaram confirmando linhas fora da edificação e uma bolha por nível, além da bateria de cortes. DLL fixa atualizada com mesmo caminho/ID após fechamento das janelas e verificação de liberação da DLL; processo antigo sem janela não foi encerrado à força. Revit reabriu e executou validação pela própria DLL fixa, sem outro manifesto/carregador. Job explícito consumido uma vez; sem job o produto não inicia validação. Cópia salva; detalhes TESTES_CORTES_V043.md.







