# VEBRAM 0.5.17 — organização automática de folhas

## Como usar
Revit > Projeto legal > Preparar legal > Organizar folhas.
Escolha o padrão e os entregáveis preparados. Clique em Calcular prévia, confira a distribuição e clique em Aplicar esta distribuição. Cancelar não altera o projeto. O botão Colocar uma vista mantém a colocação individual.

## Padrão preferencial de Lucas e Silvana
- LEG-01: Implantação, Situação e Quadro urbanístico.
- LEG-02: Plantas legais e Q04 de áreas dos ambientes.
- LEG-03: Cobertura acima; Cortes A e B juntos abaixo, quando couberem.
- LEG-04: quatro Fachadas, preferencialmente em duas linhas.

## Padrão de referência R20
PDF lido e renderizado: CT-ARQ-LEGAL-CONJ-R20.pdf, quatro folhas A1 horizontais.
P01: Situação e memória gráfica de áreas, índices, verificações e árvores.
P02: Planta térrea e quadros de esquadrias/ambientes.
P03: Implantação, Cobertura e Cortes A/B.
P04: quatro Fachadas e Cortes C/D complementares.
O segundo perfil reproduz essa separação dos entregáveis atualmente disponíveis no plugin. Não cria memória gráfica de áreas, quadros de portas/janelas, árvores ou Cortes C/D ainda não implementados. Usa a folha LEG correspondente, sem renumerar como PL-P.

## Regras
- Escalas existentes preservadas; nunca reduzir escala para caber.
- Medição nativa inclui vista e título, com margem de segurança e espaço entre vistas.
- Grupos são preferenciais. Quando não couberem, o organizador avisa na prévia e usa folhas complementares com o mesmo tipo de carimbo.
- Reutiliza folhas complementares existentes antes de criar outras.
- Preserva conteúdo manual não selecionado como obstáculo, além de todas as vistas e elementos físicos.
- Vistas fixadas, tabelas divididas ou vistas maiores que a área útil são recusadas; a operação é revertida.
- A prévia executa e reverte o cálculo no Revit. Aplicar recalcula e confere a distribuição antes de confirmar.
- Área útil calibrada para o carimbo A1 horizontal V02. Outros carimbos precisam de configuração própria; não são assumidos automaticamente.
- Não exclui folhas antigas vazias nem muda conteúdo técnico das vistas.

## Testes em 15/10/2026
Build Release: zero erros e avisos, binlogs locais.
2.917 verificações de geometria de papel e agrupamento.
Teste nativo final: teste-organizacao-v0517/nativo-02, CONCLUIDO.txt.
O modelo passou de três para quatro folhas. Padrão preferencial manteve todos os agrupamentos; repetição não criou folhas adicionais. Prévia revertida, vistas fixadas protegidas, nota manual preservada, vista grande recusada e botão WPF de prévia verificado dentro do Revit.
No perfil R20, a escala e as dimensões deste modelo exigiram uma quinta folha para a Cobertura. A prévia avisa essa divisão. Não foi alterada a escala para igualar o número de páginas do PDF.

## Instalação e preservação
Fonte integrada: src/Vebram.Revit. Mantidas alterações locais 0.5.16 (guias, caimentos, alturas e quadro). A cópia claude-v0516-organizar-folhas foi preservada.
Backup anterior: backups/antes-organizacao-20261015_132135.
DLL instalada: current/Vebram.Revit.dll, versão 0.5.17. Manifesto Vebram.Legal.addin passou a apontar para current. O projeto que estava aberto foi salvo antes do reinício.
CASA_TERRAS_FOLHAS_V0517.rvt é uma cópia de revisão; a origem não foi substituída.

## Limites desta entrega
Esta etapa organiza folhas. Continuam pendentes ajustes internos de leitura de textos/hachuras nos cortes, marcadores nas plantas/cobertura e demais itens da continuidade 0.5.16. Carimbo e dados técnicos precisam de conferência antes de emissão. O RVT não é uma declaração de conformidade legal.
