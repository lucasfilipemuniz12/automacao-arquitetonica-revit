# Continuidade — VEBRAM 0.5.22 (17/10/2026)

Base: branch `codex/vebram-esquadrias-v0521` (PR 6). Esta branch (`claude/legal-v0522`) só acrescenta fontes; não mexe em main nem nas branches publicadas. Compila com 0 erros e 0 avisos (SDK .NET 8.0.425, Revit 2026). RVT, DLL e binlogs continuam locais.

## O que mudou desde a 0.5.21
- **Identificação de esquadrias (PlanAnnotations.cs):** busca em 8 direções e 6 distâncias, com linha de chamada além de 7 mm no papel; primeira passada evita paredes e cotas lineares, segunda mantém o comportamento antigo. P11 e P12 passaram a receber identificação (17 no total no teste da Casa Terras). Correção de linhas não limitadas (`IsBound`).
- **Aviso de modelagem local (OpeningSchedules.cs):** esquadrias que não são família carregada (in-place) e ficam sem largura/altura no quadro são listadas com a mensagem de substituir por família carregada. Nenhuma medida é deduzida.
- **Memória gráfica de áreas (MemoryAreas.cs, novo cartão no Preparar legal):** cria a vista "02 PROJETO LEGAL - VEBRAM - Memória gráfica de áreas" (cópia da planta). Pisos pintados por classe via sobreposições por elemento; Laje/RADIÊ oculta para não sobrepor; garagem coberta é um piso externo/interno sob ambiente cujo nome contém "garagem"; terreno (sólido topográfico) pintado como gramado e a área sem piso = área do lote confirmado na Implantação menos pisos pintados (conta declarada no relatório). Letras por classe (A, E, G, D, P, I, AP) na legenda e na planta. Eixos, marcas de elevação, corte e chamada ficam ocultos só nessa vista. Classes físicas apenas; critérios municipais não são avaliados.
- **Plantas limpas (LegalModules.Preset):** elevações e chamadas ocultas nas plantas do legal; marcas de corte ocultas só na cobertura (na planta baixa elas indicam por onde o corte passa).
- **Rótulos de caimento (RoofDrainage.cs):** reservam espaço de objetos pequenos e evitam paredes; anéis de posição crescentes. Na Implantação os rótulos ainda podem cair dentro do retângulo da caixa.
- **Guias ilustrados (LegalGuide.cs):** Situação ganhou desenho próprio com pinos 1 a 6 batendo com os campos; Cobertura ganhou desenho próprio; pino 2 das fachadas explica o giro. Quadro urbanístico, Áreas, Implantação, Planta e Cortes foram auditados e estão coerentes.
- **Faixa de botões (App.cs):** painéis 1 Preparar o projeto (Configurar parâmetros, Diagnosticar, Preferências), 2 Entregáveis legais (Preparar legal) e Avançado (Demonstração do V02). Um método por aba (`BuildLegal`); futuras abas Executivo e Modelagem entram como métodos novos. Cartão "Colocar uma vista" removido da janela principal.
- **Diagnosticar (Diagnostics.Checklist):** a janela abre com lista "PARA RESOLVER"; o relatório completo continua em "Salvar relatório".
- **Dados do carimbo (StampData.cs, novo cartão):** formulário para nome, número, cliente, endereço, autor e data do projeto, e projetado/desenhado/verificado/aprovado por nas folhas LEG. Só grava o que foi preenchido; "A PREENCHER" aparece como vazio; folhas EX e PRE não são alteradas.
- **Notas das folhas (SheetNotes.cs, novo cartão):** notas gerais e por folha digitadas pelo usuário, guardadas no projeto (ExtensibleStorage em ProjectInformation) e desenhadas no campo "notas" da lateral após as notas automáticas. `SheetNotes.Refresh` redesenha só as notas geradas e não editadas; notas editadas à mão são preservadas.

## Modos de teste por job (somente em cópia do RVT)
O job fica em `section-validation-job.json` ao lado da DLL; o Revit o consome na abertura. Modos novos: `areas021` (levantamento de áreas), `memoria021` (memória gráfica, repetição e modelo inalterado), `limpeza021` (plantas e visibilidade das marcas), `carimbo021` (levantamento do carimbo), `carimbo022` (gravação e leitura de volta dos dados do carimbo), `notas022` (notas por folha, sem duplicar). Todos exportam PNG e relatório e usam valores de teste só na cópia. Não rodar em projeto do usuário sem pedido explícito.

## Pendências
- Janelas novas (Notas das folhas, Dados do carimbo, Memória gráfica) só foram validadas por job; ninguém as abriu e usou na interface.
- Cotas internas dos ambientes pequenos e dos ambientes sem paredes ortogonais seguem pendentes (aceito pela usuária).
- Rótulos de caimento na Implantação.
- Itens da análise do R20 ainda não feitos: histórico de revisões, referências do conjunto, quadro de árvores, verificações de afastamento (real medido, mínimo digitado) e coluna "abertura" dos quadros como campo escolhido.
- Estressar com 2 ou 3 projetos reais antes de dar o legal por aprovado.
- Os parâmetros urbanísticos usados nos testes são arbitrários. Não copiar áreas ou medidas do PDF de referência.
