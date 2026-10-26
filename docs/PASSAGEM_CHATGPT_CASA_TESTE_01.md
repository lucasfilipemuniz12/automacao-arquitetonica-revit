# Passagem para o ChatGPT/Codex — estado em 18/10/2026 (noite)

## 1. Revisão do plugin VEBRAM 0.5.25
- PR 10 (`claude/revisao-legal-v0525-ajustes`, base `codex/revisao-legal-v0525` = PR 9, que depende da PR 8). Ainda não mesclada.
- Correções: cotas internas só onde as mesmas duas paredes se enfrentam (ambientes pequenos/irregulares viram pendência "cote manualmente"); memória gráfica renomeada não gera vista duplicada; aviso quando nenhum piso tem classe; cadastro ilegível com mensagem clara e janela de cadastro abrindo. Detalhes e testes em `docs/REVISAO_CLAUDE_0525.md`.
- Testado no Revit (rotina `review025`) numa cópia da Casa Terras: tudo passou, modelo físico preservado.
- **Instalado no Revit agora: a 0.5.25 original do ChatGPT** (`github-v0522\current`, build `adabb9d`), sem as correções da PR 10. Manifesto `%APPDATA%\Autodesk\Revit\Addins\2026\Vebram.Legal.addin` restaurado ao original.
- Próximo passo sugerido: mesclar PR 8 → PR 9 → PR 10 (ou revisar a PR 10) e compilar/instalar a versão resultante quando a Silvana pedir.

## 2. Casa de teste 01 (novo projeto para testar o plugin fora da Casa Terras)
Pasta: `C:\Users\lucas\Desktop\ARQ TESTE\casas-teste\`
- `Casa_Teste_01.rvt` — projeto atual (template `ARQ_AUTOMACAO_V02.rte`), já salvo com os níveis novos.
- `FOLHA_DADOS_CASA_TESTE_01.pdf` (e `.html`) — **versão 5**: todos os campos a digitar no plugin, campo a campo, com origem (anúncio / modelo / fictício / a confirmar / e-book PBH).
- Cópias: `Casa_Teste_01_versao1.rvt`, `Casa_Teste_01_versao2.rvt` (antes dos níveis novos), `Casa_Teste_01_modelo_Silvana.rvt` (arquivo dela com o modelo de exemplo de níveis, intacto).
- Terreno: lote à venda na Loft (ref. 16498), Av. Deputado Último de Carvalho, Planalto, BH — 12 × 30 m (360 m²), plano, murado. Só dados públicos do anúncio.
- Casa térrea colonial: fachada simétrica, varanda 9 × 2,5 m com 4 pilares, telhado 4 águas, beiral 0,60 m, 35%. Ambientes: sala/jantar 42,92 m², circulação 9,29, quarto (suíte) 18,93, banheiro 5,27, despensa 4,90, cozinha 18,93. Portas P01–P07 e janelas J01–J08, só famílias do template.
- Níveis (valores de teste, seguindo o modelo de exemplo da Silvana ao lado da casa):
  terreno/gramado/calçada 0,00 · laje de piso 15 cm −0,14 a +0,01 (casa + varanda) · piso acabado 7 cm topo +0,08 · banheiro +0,07 · box 0,90 × 1,20 m +0,06 · varanda +0,06 · paredes +0,01 a 2,91 · laje de forro 10 cm 2,91 a 3,01 · telhado a partir de +3,01 · peitoril 1,00 m do piso (banheiro 1,60) · portas no piso acabado.
- Desníveis a decidir: entrada 2 cm, banheiro 1 cm, cozinha → quintal 8 cm (soleira/degrau), borda da varanda 6 cm. Conferir NBR 9050.
- Tipos criados (cópias renomeadas para o plugin reconhecer a classe): "Calçada - teste", "Terreno gramado - teste", "Laje de piso 15 cm - teste", "Laje de forro 10 cm - teste", "PISO EXTERNO 5CM - teste".
- O gerador da casa foi uma ferramenta local fora do repositório (`casas-teste\gerador-src`, modo `casateste01`); não faz parte do plugin.

## 3. Perfil municipal de BH (Biblioteca municipal)
A biblioteca deste computador estava vazia; a Silvana vai cadastrar o perfil pela parte 2A da folha:
TO 80% · CA 1,0 (básico) · TP 10% · afastamento frontal 3,0 m (4,0 m se a avenida for arterial) · lateral e fundos 1,5 m (H ≤ 8 m) · altura máxima vazia · altura máx. na divisa 5,0 m (nas condições).
Fonte: e-book PBH "Parâmetros urbanísticos" (SMPU, ago/2020, rev. ago/2023), resumo do Anexo XII da Lei 11.181/2019. **Zona do lote e classificação da via não confirmadas (BHMap).** Zoneamento fica vazio no perfil e no cadastro, senão o plugin não usa o perfil.

## 4. pyRevit / BIMBridge
- Instalado pela Silvana: pyRevit 7.0.0 (`%APPDATA%\pyRevit-Master`), agente ligado com política **ask** (cada mudança pede Keep/Discard no Revit).
- O instalador já registrou o servidor MCP `pyrevit` no **Codex/ChatGPT** (`~\.codex\config.toml`). Para o Codex usar: fechar e abrir o Codex/ChatGPT, abrir o Revit com um modelo e pedir "Leia o skill bimbridge e descreva o modelo Revit aberto".
- Também registrado no Claude Code (`~\.claude.json`). O VEBRAM continua carregando junto com o pyRevit (verificado no journal).
- Cópia das configurações de antes da instalação: `casas-teste\bimbridge-antes\`. Desinstalar: `uninstall.ps1` na mesma pasta (mantém o pyRevit).

## 5. O que a Silvana está fazendo agora
Conferência manual na `Casa_Teste_01.rvt` seguindo a folha de dados: Biblioteca municipal → Cadastrar projeto → Implantação (planta "PRE - Layout / Nível 1", piso base "Laje de piso 15 cm - teste", lote 12 × 30, canto 0/0, frente "Abaixo", estudo fictício) → Situação → Quadro urbanístico → Planta, Cobertura, Cortes, Fachadas, Quadros de esquadrias, Memória gráfica → Organizar folhas. Ela vai anotar mensagens confusas, cotas faltando e sobreposições para revisar.

## Regras combinadas
Não inventar medidas nem valores de lei (deixar "a confirmar"); testar só em cópias; não salvar sobre os originais; não acrescentar funcionalidades sem pedido; selo PBH não é exigido (carimbo da empresa); linguagem simples com a Silvana (ela não usa terminal).
