# Revisão de estabilização do VEBRAM 0.5.25 (Claude)

Base: branch `codex/revisao-legal-v0525` (PR 9, que inclui a PR 8 do cadastro).
Branch desta revisão: `claude/revisao-legal-v0525-ajustes`.
Regras seguidas: nenhuma funcionalidade nova, nenhuma medida ou exigência municipal inventada, modelagem não alterada, carimbo da empresa (sem selo PBH).

## O que foi revisado
- Cadastro do projeto e reaproveitamento dos dados (`ProjectRegistration.cs`, `ProjectDataIntegration.cs`, `RegistrationWindows.cs`, uso em `PrepareWindow.cs`, `Implantation.cs` e `LegalContext.cs`).
- Geração das vistas legais e proteção de edições manuais (`LegalModules.cs`, `LegalContext.cs`, `DrawingOwnership`).
- Memória gráfica de áreas (`MemoryAreas.cs`, `MemoryAreaState.cs`).
- Quadros de esquadrias (`OpeningSchedules.cs`) e identificações na planta (`PlanAnnotations.cs`).
- Organização das folhas (`SheetPlanner.cs`, numeração `LEG-nn`).

## O que estava bom e foi mantido
- Repetição sem duplicar: plantas, cobertura, situação, quadro urbanístico, memória e quadros são reencontrados pelo nome e regenerados; as marcações antigas registradas são apagadas antes de criar as novas.
- Edições manuais: texto, posição ou exclusão de marcações geradas bloqueiam a regeneração até a opção "refazer" ser marcada. Elementos manuais dependentes revertem a transação.
- Cadastro: valores salvos nos entregáveis (inclusive campos apagados de propósito) têm prioridade sobre as sugestões; perfil municipal só é sugerido quando município, UF, zona, uso e intervenção conferem. Índices só vêm do perfil informado pelo usuário.
- Esquadrias sem medidas: o quadro avisa os códigos com largura/altura vazias e os modelos locais (in-place), sem deduzir medidas.
- Organizador de folhas: não reduz escalas, não renomeia folhas com conteúdo manual e recusa vistas fixadas.
- Verificado à parte: no Revit 2026 os parâmetros de largura/altura de porta e de janela são o mesmo identificador interno, então a checagem de medidas vazias também funciona no quadro de janelas.

## Correções feitas (seguras, sem mudar a modelagem)
1. **Cotas de ambientes pequenos/irregulares** (`PlanAnnotations.cs`): ao deslocar a linha de cota para longe do rótulo, agora se confere se, naquela posição, as duas paredes que se enfrentam continuam sendo as mesmas e se não há outra parede no meio. Antes, em ambientes em L/U, a cota podia atravessar uma parede ou medir um recorte. Quando não há posição segura, a pendência diz: "ambiente pequeno ou irregular, sem faixa livre entre as mesmas duas paredes. Cote manualmente."
2. **Memória gráfica renomeada** (`MemoryAreas.cs`): se a vista gerada foi renomeada, o plugin criava uma segunda memória. Agora ele avisa o nome atual e pede para voltar ao nome padrão, sem criar vista duplicada.
3. **Memória sem pisos classificados** (`MemoryAreas.cs`): quando nenhum tipo de piso do nível é reconhecido, aparece um aviso claro explicando por que a legenda fica sem áreas.
4. **Cadastro ilegível** (`ProjectRegistration.cs`, `RegistrationWindows.cs`, `PrepareWindow.cs`): um registro de cadastro corrompido gerava erro técnico e impedia abrir a própria janela de cadastro. Agora a mensagem é clara, a janela abre com os dados do carimbo para o usuário conferir e salvar de novo, e o botão de inserir dados do cadastro nas notas mostra o aviso em vez de fechar a janela.

## Testes feitos
- Compilação Release: 0 erros, 0 avisos.
- Suíte de distribuição de folhas (`tests/SheetPacking`): 2932 verificações passaram.
- Suíte aritmética (`tests/Arithmetic`): passou.

## Teste no Revit (18/10/2026, autorizado pela Silvana)
- Feito numa cópia da Casa Terras (`ARQ TESTE\claude-testes-revit-0525\CASA_TERRAS_TESTE_CLAUDE_0525.rvt`), copiada do RVT de revisão 0.5.25; o original não foi aberto nem salvo.
- Antes do teste, guardei uma cópia de segurança do plugin instalado (manifesto e DLL). Depois do teste, o manifesto original foi restaurado (arquivo idêntico) e o Revit foi fechado.
- Rotina automática `review025` (a mesma usada pelo ChatGPT), com uma checagem nova para a memória renomeada. Resultado: **tudo passou**.
  - Cadastro/integração: duas gerações, sugestões e campos apagados preservados (INTEGRACAO_024).
  - Planta e cobertura regeneradas sem duplicar; marcas de corte ocultas na cobertura; rótulos de caimento sem cruzar linhas nem cotas.
  - Memória gráfica: classificação legal, presença no organizador, legenda editada protegida, **memória renomeada recusada sem criar vista duplicada**, prévia/aplicação das folhas com escala preservada.
  - Modelo físico preservado antes/depois.
- Planta legal: as mesmas 11 cotas entre acabamentos da versão anterior continuaram sendo geradas (a nova checagem de ambientes irregulares não removeu nenhuma cota válida). As pendências dos ambientes 01, 02, 03, 08, 09 e 12 continuam as mesmas e precisam de cota manual.
- Folhas: três folhas (LEG-01 implantação/situação/quadros; LEG-02 plantas, memória, Q04 e quadros de esquadrias; LEG-03 cobertura, cortes e fachadas). 17 identificações de esquadrias. Quadro de portas mostra P08, P09, P10 e P13 sem medidas (modelagem local), como esperado; nenhuma medida foi deduzida.

## Pendente (não feito nesta revisão)
- Testar em dois ou três projetos reais diferentes da Casa Terras (o teste no Revit desta revisão foi só na Casa Terras).
- Itens já conhecidos e mantidos como pendência: histórico de revisões, referências do conjunto, coluna de abertura dos quadros, união de contornos na memória e classes que ainda dependem do nome do tipo de piso.
- Quadros de esquadrias renomeados também são recriados com o nome padrão (mesmo comportamento das versões anteriores); não foi alterado para não bloquear cópias intencionais.
- Esquadrias in-place continuam sem medidas no quadro até serem trocadas por famílias carregadas.
