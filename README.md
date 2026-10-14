# VEBRAM — automação arquitetônica para Revit

Plugin nativo C# / .NET 8 / WPF / Revit 2026. Prioridade: Projeto legal. Modelagem e Executivo serão ampliados após avaliação de Lucas.

## Estado atual

- Implantação, planta legal, cobertura por água plana e Q04: implementados e testados nos lotes anteriores. Rótulos internos priorizados; cotas sem espaço ou referência válida são relatadas.
- Cortes A/B 0.4.4: testes nativos e revisão visual concluídos na Casa Terras. Níveis curtos na lateral com uma bolha, sem texto duplicado ou cruzamento sobre a casa/terreno; cota vertical separada e nota inferior reposicionada.
- Candidato 0.5.1: código de quatro fachadas, situação esquemática, quadro urbanístico e colocação de entregáveis nas folhas LEG-01/02. Compilado sem erros/avisos; 19 testes de cálculos e retângulos passaram. Ainda não validado dentro do Revit: a sessão ficou no aviso de complemento não certificado. Não declarar o Projeto legal inteiro concluído.

Leia [continuidade atual e retomada](docs/CONTINUIDADE_014_PROJETO_LEGAL.md), [evidência de cortes](docs/TESTES_CORTES_V044.md) e [histórico](docs/ESTADO_ATUAL.md). Decisões recentes prevalecem sobre textos históricos.

## Fluxo

Na aba Projeto legal, abra Preparar legal, escolha a planta de modelagem e um botão ilustrado. Os módulos reutilizam as referências do template V02 e preservam a planta fonte. Preferências legais recebe estilo de cota, escala e nota adicional.

Situação/urbanismo exigem lote já confirmado na Implantação. Áreas computáveis/permeáveis e limites municipais são informados pelo projetista; campos vazios ficam pendentes. Situação é um diagrama, não um levantamento. Fachadas preferem elevações nativas e recusam marcadores compartilhados ou marcações manuais ao reposicionar. Folhas preservam colocações existentes e não reduzem a escala automaticamente para encaixar uma vista.

## Estrutura

- src/Vebram.Revit: fonte completo do produto e validação nativa por job explícito.
- profiles: contrato ARQ por GUID.
- scripts: compilação, instalação e preparo de cópia para validação.
- tests/Arithmetic: testes sem Revit das classes de cálculo e encaixe usadas pelo produto.
- docs: contexto, evidências e pendências.
- dev / tools: rotinas históricas; não reexecutar automaticamente em projetos aprovados.

## Compilar e instalar

Windows, SDK .NET 8 e Revit 2026 instalado. Execute scripts/Build.ps1 e scripts/Install-Local.ps1 com Revit fechado. As DLLs da Autodesk são referências locais e não são distribuídas. Instalação fixa: current/Vebram.Revit.dll; GUID 936f6231-d0c9-47a3-83e6-65813c46c176.

O aviso de segurança reapareceu após a atualização, apesar do caminho/ID fixos. Não desabilitar a segurança nem prometer ausência de novos avisos. A escolha de carregar pertence ao usuário.

## Validar

1. Testes de cálculo: compilar tests/Arithmetic/Arithmetic.csproj e executar a DLL resultante. Usar MSBuild com binlog.
2. Testes nativos: com a DLL atual instalada e Revit fechado, executar scripts/Validate-Local.ps1 informando SourceProject, OutputFolder e Mode legal05, depois abrir Revit.
3. Ler os relatórios e abrir as vistas/folhas para revisão. O teste novo utiliza dados fictícios identificados e precisa de cópia compatível com V02 contendo Implantação preparada e planta PRE do nível 1. Não substitui a origem. Sem job, o plugin não executa validação na inicialização.

## Limites

Lote retangular e contorno ortogonal; áreas de urbanismo precisam de critérios do projetista. Não há cotagem construtiva completa, posicionamento completo de esquadrias, exportação final, cadastro municipal, suporte geral a marcadores compartilhados ou aprovação legal automática. A composição das folhas, espaços-guia e cada vista exigem revisão visual.

Os RVT/RTE, famílias, materiais e DLLs permanecem locais. GitHub compartilha fontes e documentação; a API e o resultado gráfico são validados no Revit do Windows.
