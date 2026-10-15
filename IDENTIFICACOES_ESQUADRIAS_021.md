# VEBRAM 0.5.21 — identificação de esquadrias na planta legal

Ao preparar novamente a Planta legal, o plugin acrescenta a Marca existente dos elementos visíveis das categorias Portas e Janelas, inclusive elementos que não são FamilyInstance. Não gera nem renumera códigos. Elementos sem Marca são informados como pendência.

As identificações são textos nativos de 2 mm, associados ao estado de geração da planta. Procuram posição próxima da esquadria, em diferentes lados e distâncias no papel, evitando os nomes/áreas dos ambientes, outros textos existentes e as demais identificações. Não substituem tags manuais já vinculadas ao elemento. Essa busca não garante ausência de conflito com toda linha de modelo, cota ou símbolo; revisar a planta antes de emitir.

São textos gerados, não tags associativas: após alterar a Marca ou posição de uma esquadria, preparar a Planta legal novamente e recalcular a prévia das folhas. A atualização verifica edições manuais nas anotações geradas antes de substituir, conforme a proteção já existente do módulo.

O diagnóstico dos quadros agora lê as células visíveis de largura/altura, em vez de considerar apenas parâmetros de FamilyInstance. Isso permite reconhecer os códigos com medidas vazias dos elementos especiais desta casa. Não preenche medidas com caixa envolvente e não altera geometria. Tabelas personalizadas sem as colunas esperadas recebem indicação para conferência direta.

Uso: Projeto legal > Planta legal > Preparar. Depois Quadros de esquadrias para conferir pendências e Organizar folhas > Calcular prévia > Aplicar.

Fonte: PlanAnnotations.cs, OpeningSchedules.cs e PrepareWindow.cs. Validação nativa ampliada em OpeningSchedulesValidation.cs: repetição das identificações, proteção de texto editado, preservação das Marcas e do modelo físico e detecção dos códigos com medidas ausentes.
