# VEBRAM 0.5.26 — Casa Teste 01

## Escopo

Revisão da PR 10 e diagnóstico dos comandos do plugin com a Casa Teste 01,
sem corrigir a arquitetura ou modificar a modelagem para adequá-la aos testes.
A PR 10 foi revisada e mesclada na `codex/revisao-legal-v0525`
(merge `b7204154fb49f0e2a106a27185a96b2cbbc15a80`).

Os ensaios nativos usam uma cópia explícita do RVT salvo pelo usuário:
`codex-testes-casa-0526/Casa_Teste_01_SALVA_DIAGNOSTICO.rvt`.
O original e o volume de demonstração da Silvana não foram alterados.
As execuções pelo pyRevit usam `run_modify(dry_run=true)`; não salvam o RVT.

## Falhas reproduzidas e corrigidas

### Quadros de esquadrias

`OpeningSchedules.Apply` criava corretamente as tabelas de portas e janelas.
Depois, `PrepareLegalCommand` abria a tabela de portas e chamava
`UIView.ZoomToFit`, porque só a chave `areas` estava isenta dessa chamada.
O Revit lançava `View is not a graphical view.` e o comando retornava
`Result.Failed`, revertendo a criação das tabelas.

O journal da sessão original registra a abertura da tabela e esse erro.
O mesmo erro foi reproduzido pelo pyRevit na cópia.
A navegação agora testa o tipo real da vista: abre `ViewSchedule` sem aplicar
zoom gráfico. Plantas continuam recebendo `ZoomToFit`.

### Prévia de Organizar folhas

Com otimização, a distribuição criava folhas complementares, reunia conteúdos
e tentava excluir uma folha nova que ficara vazia. A checagem de exclusão
considerava apenas elementos encontrados por um coletor limitado à vista.
O Revit também exclui elementos internos da folha, incluindo `SketchPlane`
e objetos sem categoria que esse coletor não retorna. Isso provocava
`Dependências impediram retirar uma folha nova vazia.` e revertia a prévia.

O organizador registra os dependentes nativos da folha imediatamente após
criá-la. Ao excluir uma folha nova vazia, aceita esses IDs e seus elementos
pertencentes à folha. Dependências externas continuam impedindo a operação;
folhas que já existiam continuam sendo preservadas como reserva.

Na primeira validação nativa, a distribuição sem otimização usou 5 folhas;
com a correção e otimização, 4 folhas. Prévia e aplicação passaram, mantendo
as escalas. Este resultado é da configuração salva nessa cópia, não uma
quantidade fixa de folhas para qualquer projeto.

## Cadastros

Foram examinados o cadastro do projeto, a biblioteca municipal, a cópia
versionada do perfil dentro do RVT e o reaproveitamento dos dados nos
entregáveis. Valores previamente salvos, inclusive campos apagados,
continuam tendo prioridade sobre sugestões. A correspondência do perfil
usa município, UF, zona, uso e intervenção.

Nenhum índice urbanístico foi criado ou validado nesta revisão. O perfil BH
de teste continua dependendo da conferência da zona e da via pelo usuário.
Não houve mudança no funcionamento desses cadastros além da correção já
revisada na PR 10 para cadastro ilegível.

## Validação reproduzível

`CasaTestRegression.Run(UIApplication, output)` exige `_DIAGNOSTICO` no nome
do RVT e reverte seu próprio grupo de transações. Foi criada especificamente
para testar os comandos e o botão WPF com um projeto diferente da Casa Terras.
Escreve relatório JSON e uma imagem da interface WPF, sem salvar ou exportar
o documento Revit. As rotinas antigas que chamam `doc.Save`/`ExportImage`
não foram executadas pelo runtime protegido do pyRevit.

Compilação Release e testes independentes são registrados em binlogs locais.
As suítes existentes passaram: 2.932 verificações de distribuição e 37
verificações aritméticas. Elas não substituem os testes nativos do Revit.

Resultado nativo: **20 verificações passaram**, incluindo abertura das duas
tabelas, reaplicação, proteção de vista fixada e nota manual, botão WPF,
preservação da modelagem, cadastro, biblioteca e marcas. O documento voltou
ao estado não modificado ao final. Relatório e imagem em
`validation/casa-teste-0526/`.

Implantação, situação, quadro urbanístico, planta legal, cobertura, Q04 e
memória foram executados duas vezes na cópia com as configurações salvas:
todos reutilizaram as vistas e a geometria permaneceu igual. Cortes e
fachadas existentes participaram da prévia/aplicação; suas posições não
foram redefinidas para testar a geração.

A planta informou três cotas pendentes por legibilidade (ambientes 02, 04 e
05); isso não foi tratado como falha de execução. A memória informou saldo
aritmético negativo e piso genérico sem classe: os quantitativos dessa
memória **não estão validados**. Sobreposições e elementos fora do lote não
são resolvidos pela soma atual, e não houve alteração para mascará-los.

## Limites

- A revisão não valida a legislação nem a qualidade arquitetônica da casa.
- A classificação da memória gráfica continua baseada nos nomes dos pisos;
  esta revisão não mudou seus critérios nem as áreas do modelo.
- As tabelas continuam abrangendo as esquadrias da fase em todos os níveis,
  conforme o comportamento existente.
- O número de folhas depende do formato, das escalas, das vistas selecionadas
  e do conteúdo manual existente.
