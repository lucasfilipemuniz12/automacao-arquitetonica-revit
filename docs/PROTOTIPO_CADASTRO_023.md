# VEBRAM 0.5.23 — protótipo de cadastro e biblioteca municipal

A aba Projeto legal passa a ter três painéis: **1 Dados do projeto**, **2 Municípios e regras** e **3 Preparar entregáveis**. Configuração de parâmetros, diagnóstico, preferências e demonstração continuam disponíveis no menu Ferramentas do terceiro painel. Os entregáveis e as revisões pendentes da 0.5.22 não foram alterados nesta etapa.

## Dados do projeto

Formulário ilustrado com nome, código, cliente, endereço, lote/quadra/inscrição, município/UF, zoneamento, uso, intervenção, pavimentos e observações. Nome, município e UF são o mínimo para salvar. Os demais campos podem ficar pendentes. Uso e intervenção são descrições do empreendimento, não classificações legais universais de porte.

O cadastro fica em ExtensibleStorage em ProjectInformation. Nome/código/cliente/endereço preenchidos também atualizam os parâmetros nativos já usados pelo carimbo. Campos vazios não apagam os parâmetros nativos. Endereço e cadastro do lote não posicionam nem desenham o terreno. Ainda não há alimentação automática desses novos campos em situação/notas, nem comparação entre o cadastro e os contornos do modelo.

Um perfil escolhido é copiado integralmente para o cadastro do RVT, com ID e revisão. Edições posteriores da biblioteca não mudam essa cópia. A escolha explícita de outro perfil na tela sugere município, UF, zoneamento, uso e intervenção; esses dados devem ser conferidos antes de salvar.

Salvar cadastro encerra a tela. Salvar e preparar abre a janela já existente de entregáveis; cancelar essa segunda janela preserva o cadastro que já foi salvo.

## Municípios e regras

Biblioteca pessoal em `%LOCALAPPDATA%/VEBRAM/Municipios/municipios-v1.json`, reutilizável entre RVTs neste computador. Novos perfis e edição de perfis existentes, nome livre, município/UF, zoneamento/setor, uso, intervenção, condições/exceções, referência documental e data de conferência.

Parâmetros opcionais: ocupação máxima, aproveitamento máximo, permeável mínimo, afastamentos frontal/lateral/fundos e altura máxima. São valores digitados pelo usuário; campos desconhecidos ficam vazios. Percentuais têm validação de entrada de 0 a 100; os demais números devem ser finitos e não negativos. Isso valida o formato de entrada, não sua aplicabilidade legal.

Cada gravação incrementa a revisão. Gravação com arquivo temporário, backup da versão anterior e bloqueio de edição concorrente/desatualizada. Não há pacote oficial pré-instalado, coleta de leis, importação de PDF, sincronização entre computadores, exclusão pela tela ou motor de verificação municipal nesta versão. Nenhuma regra municipal foi inventada para o exemplo. O perfil de demonstração tem parâmetros numéricos vazios e se identifica como não validado.

## Verificação

Compilação .NET 8 para Revit 2026 com zero erros e avisos e binlogs. Modo de job local `cadastro023`, somente numa cópia: persistência da biblioteca em pasta isolada, incremento de revisão, rejeição de edição antiga, preservação da cópia do perfil no RVT, atualização do nome nativo e geometria preservada. Relatório CADASTRO_023.json na pasta de entrega.

Conferência pela interface: abrir os dois cadastros, exibir opções de uso/intervenção, criar e salvar perfil demonstrativo na biblioteca pessoal, escolher esse perfil no projeto e seguir para os entregáveis. Prints reais do Revit, sem alteração da imagem, acompanham a entrega local em PROTOTIPO_VEBRAM_V0523_CADASTRO.

As correções de memória gráfica e a revisão global permanecem adiadas conforme o usuário solicitou. Este protótipo organiza e persiste informações; não declara um projeto aprovado pela prefeitura.

Conferência final da DLL instalada: o perfil pessoal permaneceu na biblioteca após reiniciar o Revit. Salvar e preparar, fechar a janela de entregáveis e reabrir o cadastro preservou o perfil escolhido e seu zoneamento. A cópia RVT foi salva pela interface.
