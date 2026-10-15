# VEBRAM 0.5.20 — quadros de esquadrias

Em Projeto legal, escolher a planta de origem e clicar em **Quadros de esquadrias**. O comando cria duas tabelas nativas do Revit. Depois, em **Organizar folhas**, selecionar os quadros e calcular uma nova prévia.

Portas: código (Marca da instância), largura, altura e quantidade. Janelas: os mesmos campos mais altura de peitoril. Medidas em metros, duas casas decimais. Uma linha por instância; códigos repetidos são relatados para conferência. Família/tipo permanece como coluna oculta, disponível no editor de tabelas, para manter o quadro compacto.

As tabelas abrangem todos os níveis, com a fase e o filtro de fase da planta usada na primeira criação. Reexecuções reutilizam as tabelas e preservam as personalizações do usuário; não redefinem fase, filtros ou colunas. Para mudar esse escopo, editar as propriedades da tabela.

As tabelas acompanham as alterações do modelo. O comando não altera marcas, dimensões, famílias nem geometria. Informa marcas ausentes e dimensões padrão não disponíveis em famílias, inclusive algumas famílias modeladas no local. Campos em branco precisam de revisão; nenhuma dimensão é inventada a partir de caixa envolvente. Abertura, material e desempenho não são deduzidos do nome da família.

O organizador tenta as zonas laterais do template V02, preserva o Q04 e o quadro urbanístico, mede os quadros reais e usa a área de desenhos ou mais folhas quando faltar espaço. As escalas das vistas são mantidas. As tabelas de esquadrias têm prioridade nas zonas laterais durante a consolidação.

Esta etapa trata das tabelas. A colocação automática das identificações P/J nas plantas ainda precisa ser implementada; os códigos mostrados são os que já existem no modelo.

Fonte: src/Vebram.Revit/OpeningSchedules.cs. Validação nativa: OpeningSchedulesValidation.cs, acionada apenas por job local explícito openings020. Nenhuma execução automática em projetos do usuário sem esse job.
