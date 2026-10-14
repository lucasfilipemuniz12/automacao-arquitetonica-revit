# Continuidade — 14/10/2026

## Resultado confirmado

Cortes A/B da versão 0.4.4 executados na Casa Terras, preservando as duas vistas do template V02 e a modelagem. Testes de repetição, edição protegida, posição 25%, inversão e profundidade 8/15 m passaram. Estado final central, sentido normal, 15 m. Uma cota vertical entre faces reais em cada corte.

As linhas de nível agora ficam curtas na faixa esquerda, com um símbolo por nível. Textos duplicados removidos. O cálculo inclui o terreno e anexos visíveis; a nota inferior fica abaixo da geometria. Ambos os cortes foram abertos e revisados visualmente nesta sessão.

Cópia pronta para revisar: `teste-cortes-v044/Casa_Terras_CORTES_LIMPOS_V044.rvt`. Evidência: `teste-cortes-v044/TESTES_CORTES.json`. A verificação automatizada do afastamento dos níveis usa o contorno da casa; a inclusão de todo o contexto foi também conferida visualmente.

## Código novo — candidato 0.5.1

- **Fachadas:** formulário de piso base, distância e giro; prepara as quatro referências do V02 com elevações nativas, recorte e níveis laterais. Mantém IDs e modelo. Recusa marcadores compartilhados, vistas fixadas e marcações manuais; requer revisão nesses casos. Aceita referência existente como corte apenas quando o template já a utiliza.
- **Situação:** diagrama de lote retangular confirmado na Implantação, confrontantes informados, via e norte, duas cotas nativas. Escala 1:500. Não representa levantamento cadastral/georreferenciado.
- **Quadro urbanístico:** lote, projeção do contorno base ajustado às paredes adjacentes, áreas computável/permeável informadas, TO/CA/TP e comparação com limites informados. Dados vazios ficam pendentes. Não infere áreas legalmente computáveis ou aprovação municipal. Mantém conteúdo manual e coloca o quadro abaixo dele.
- **Folhas:** usuário escolhe um entregável preparado e LEG-01/LEG-02; encontra espaço na área esquerda, reservando 28% à direita e margem de 15 mm. Mede vista e título ou schedule, mantém 6 mm entre caixas, não altera escala, preserva colocações existentes e reverte se não couber. Espaços-guia e composição gráfica precisam de revisão. Não exporta PDF nesta versão.

Fonte e DLL em `staging-v051` compilados com zero erros e zero avisos. **19 testes executados** sobre as classes de produção `LegalMetrics` e `LegalPaperRect`; relatório `teste-legais-v05/TESTES_ARITMETICA.json`. Verificam áreas, índices, limites ausentes, encaixe/colisões e recusa de folha cheia. Esses testes não validam o Revit.

## Bloqueio real da validação nativa

Na reabertura, Revit exibiu “Segurança — Complemento não certificado”, VEBRAM Automacao, apesar de caminho e GUID fixos. A skill Computer Use proíbe operar esse aviso. Lucas está dormindo e pediu que não fossem enviadas solicitações: nenhuma foi enviada.

**Não afirmar que os módulos 0.5.1 foram executados/aprovados no Revit.** Não existe `TESTES_LEGAIS_05.json` de execução bem sucedida. A cópia `teste-legais-v05/Casa_Terras_LEGAL_V05.rvt` é apenas uma cópia dos cortes 0.4.4; não contém os novos resultados.

A instalação fixa `current/Vebram.Revit.dll` contém o primeiro candidato 0.5.0 (sem o módulo de folhas), preso pela sessão de Revit no aviso. A atualização para 0.5.1 foi tentada com abertura exclusiva da DLL e recusada por bloqueio do processo; nenhum arquivo bloqueado foi substituído. `staging-v051` contém o candidato completo. O GUID continua `936f6231-d0c9-47a3-83e6-65813c46c176`; não foram criados manifestos temporários.

O job de validação foi retirado da inicialização e guardado inerte em `teste-legais-v05/VALIDACAO_PENDENTE.json`. Sem job ativo, o plugin não executa testes de inicialização. Não restaurar esse job no candidato antigo.

## Próxima retomada

1. Resolver o aviso de segurança por ação do usuário e fechar a sessão atual do Revit. Não automatizar avisos ou alterar a segurança do Revit. Não encerrar à força o processo antigo sem janela: pode haver conteúdo não salvo.
2. Conferir liberação da DLL fixa e atualizar `current` com DLL/PDB/deps de `staging-v051` (ou recompilar a fonte atual). Manter caminho e GUID. Atualizar apenas após a DLL ser liberada.
3. Preparar uma cópia nova com `scripts/Validate-Local.ps1`, origem nos cortes 0.4.4, modo `legal05`; abrir Revit. A rotina nativa integrada verifica situação/urbanístico, repetição, proteção de edição, quatro fachadas, direções cardeais, preservação da geometria/IDs e colocação sem duplicação/sobreposição nas duas folhas. Os dados de teste são explicitamente fictícios.
4. Ler `ERRO.txt` se houver falha; não marcar aprovação pela compilação. A primeira inspeção inventaria os marcadores nativos em `MARCADORES.json`: pode revelar marcador compartilhado e exigir evolução da rotina.
5. Abrir e revisar as quatro fachadas, Situação, quadro e folhas. Refinar recorte/posição e encaixe com evidência visual. Preparar também planta/cobertura/Q04 nas folhas, ainda não abrangidos no teste novo.

Prioridade: concluir Projeto legal; não implementar Modelagem/Executivo agora. Publicar somente fontes/documentação no branch de revisão; não enviar RVT/RTE/famílias ou DLLs Autodesk. GitHub não substitui a validação local.
