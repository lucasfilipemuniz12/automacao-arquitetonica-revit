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

## Pendente (não feito nesta revisão)
- **Teste no Revit**: não foi feito. Instalar esta versão substitui o plugin instalado que a Silvana usa; por isso depende da autorização dela e deve ser feito somente numa cópia dos RVTs (pasta de testes), nunca no original.
  - Conferir na Planta legal da Casa Terras se as cotas dos ambientes 01, 02, 03, 08, 09 e 12 continuam corretas ou viraram pendências com a nova mensagem.
  - Renomear a memória gráfica numa cópia e confirmar o aviso.
- Itens já conhecidos e mantidos como pendência: histórico de revisões, referências do conjunto, coluna de abertura dos quadros, união de contornos na memória e classes que ainda dependem do nome do tipo de piso.
- Quadros de esquadrias renomeados também são recriados com o nome padrão (mesmo comportamento das versões anteriores); não foi alterado para não bloquear cópias intencionais.
- Esquadrias in-place continuam sem medidas no quadro até serem trocadas por famílias carregadas.
- Testar o fluxo em dois ou três projetos reais antes de aprovar.
