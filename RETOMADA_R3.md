# Retomada R3

Worktree: `C:\Users\lucas\Desktop\ARQ TESTE\codex-legal-r3`. Branch `codex/legal-acabamento-r3` a partir de origin/main 69674ae.

Leia RESULTADO_R3.md e docs/LEGAL_R3.md. Usuário mandou parar antes de 5% da janela; consulta em 29/10 indicou 8% restantes.

SDK: `C:\Users\lucas\Documents\Codex\2026-10-13\files-pasted-by-the-user-continuidade\work\sdk\dotnet\dotnet.exe`.

Primeiro teste completo: validation/r3-01. 30 passos CT03, 23 Sobrado, zero falhas/avisos, preservação true. PNGs e JSONs copiados em casas-teste/revisao_legal_r3. Código posteriormente refinado ainda precisa de validação final, inclusive CT04.

CT04 original salvo em `C:\Users\lucas\OneDrive\Área de Trabalho\ARQ TESTE\casas-teste\Casa_Teste_04\Casa_Teste_04.rvt`. Conferência final zero avisos, 15 ambientes. Nunca usar original para teste legal. Revit processo 23708 (verificar novamente), documento CT04. Antes de fechar confirme IsModified=false; não fechar alterações não salvas sem perguntar. Último salvamento confirmado às 22:09 locais.

Construção começou por Case04Model; erros iniciais foram corrigidos no modelo com pyRevit guardado. Scripts incrementais em dev/CT04; ainda há diferenças entre o bootstrap C# e o modelo final (oitões e material de cobertura, por exemplo). Não declarar o bootstrap como reprodução completa sem integrar/conferir.

IDs na CT04 original: níveis Térreo 311, Superior694, Garagem371880; topografia373128, aterros373155/373162; coberturas372278/372295/372312; vínculoDXF372275; lajes principais371908/371915/371928. Oitões373852/373881. Roof plano CT04 recebe material VEBRAM Concreto364730. Uso de IDs apenas para essa base, nunca supor em outros arquivos.

MCP pyRevit get_context/get_skill foram lidos. Scripts CPython para evitar erro Unicode do IronPython. Save/export bloqueados em runs guardados; salvar pela interface ou rotina nativa de teste, sem contornar o bloqueio. Capturas pyRevit criam PNG em AppData/Roaming/pyRevit/agent/captures.

Dados CT03: referencia/casas-teste/CT03_esquina_declive/ficha/casa.json. Sobrado fonte de cópia: `C:\Users\lucas\Desktop\ARQ TESTE\casas-teste\Sobrado_Teste_02\Sobrado_Teste_02_teste_claude_0527.rvt`. Usar cópias novas R3_TESTE_. Template e hashes anteriores em validation/r3-01/ORIGINAIS_SHA256.json.

Usuário autorizou clicar Carregar uma vez pois está no celular. Nunca escolher Sempre carregar. CUA suporta isso por autorização explícita; skill local não prevalece sobre instrução do usuário. Ao retomar CUA após compactação, chamar cua.rewriteDocumentation conforme ferramenta. Handles antigos podem mudar; obter inventário novo. Se detectar interação do usuário, reobservar/reselecionar janela; não insistir em coordenadas antigas.

Restam: build/testes finais, teste nativo completo em três cópias, evidências CT04, revisão visual, relatório/ZIP finais e PR pronto. Não fazer merge.
