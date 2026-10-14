# VEBRAM — automação arquitetônica para Revit

Add-in nativo C# / .NET 8 / WPF, desenvolvido para Lucas. Revit validado: 2026.4.1. Prioridade atual: Projeto legal; Modelagem e Executivo serão ampliados após avaliação.

## Estado atual

- Implantação: piloto funcional aprovado por Lucas; lote retangular e contorno ortogonal, cotas nativas, norte/contexto informado e marcações rastreadas.
- Versão 0.4.2 (testes nativos concluídos na Casa Terras): preferências de estilo de cota, escala e informação adicional; Planta legal com cotas gerais/internas entre acabamentos e rótulos ajustados; Cobertura com leitura por água plana; acesso ao quadro nativo Q04.
- Fachadas, Situação, quadro urbanístico, cotagem de posicionamento e tags de esquadrias, montagem de folhas e exportação: próximos passos.

Consulte [estado e evidências](docs/ESTADO_ATUAL.md) antes de trabalhar. O [contexto mestre](docs/CONTEXTO_MESTRE.md) preserva o planejamento histórico; decisões recentes estão em ESTADO_ATUAL.

## Estrutura

- src/Vebram.Revit: código completo do produto.
- profiles: contrato dos parâmetros compartilhados ARQ.
- scripts: compilação e instalação local.
- dev: rotinas locais para testes nativos em cópias de demonstração.
- tools: fontes das rotinas pontuais de casa básica e correção de unidades.
- docs: decisões, regras, evidências e pendências.

## Compilar e instalar

1. Windows, SDK .NET 8 e Revit 2026 licenciado instalado.
2. Executar scripts/Build.ps1, informando -Dotnet e -RevitInstallDir quando necessário.
3. Executar scripts/Install-Local.ps1 após a compilação.
4. Reabrir o Revit e usar um RVT criado do template V02 compatível.

O pacote compilado fica em current, com DLL Vebram.Revit.dll e ID fixo. Fechar Revit antes de compilar/atualizar; o script impede substituir a DLL durante a sessão. A API Revit deve ser obtida da instalação local; suas DLLs não são redistribuídas. O projeto não usa pacotes NuGet de terceiros nem IA durante o uso dos botões.

## Unidades e uso

Padrão atual: comprimentos em metros; áreas em m² e volumes em m³. O botão Preferências legais permite escolher outro estilo existente de forma explícita, mostrando sua unidade. A seleção é armazenada no projeto e aplicada na próxima geração.

RVT/RTE, famílias, bibliotecas Autodesk, binários, logs de máquina e dados de clientes ficam fora deste repositório. Os modelos de teste e o template são locais. Código disponível não significa que uma sessão cloud possa executar a API sem o Revit.

## Distribuição

Repositório privado. Licença comercial do produto ainda será definida por Lucas. Skills são auxiliares de desenvolvimento, não dependências redistribuídas do plugin. BIMwright/ToolBaker são ferramentas de desenvolvimento opcionais; não foram incorporadas ao produto.



Detalhes da evolução de rótulos e cotas: [testes 0.3.2](docs/TESTES_V032.md). Há opções de espessura de paredes e largura de esquadrias; referências indisponíveis são relatadas. Revisão visual final pendente.


Correção visual mais recente: [rótulos internos 0.3.5](docs/TESTES_V035.md). Nove rótulos internos sem chamadas externas no teste; cinco cotas internas pendentes para preservar sua leitura. Revisão visual da nova cópia ainda pendente.


Cortes A/B disponíveis: posição, sentido, profundidade, níveis e cota vertical. [Testes dos cortes](docs/TESTES_CORTES_V04.md). Revisão visual pendente.


