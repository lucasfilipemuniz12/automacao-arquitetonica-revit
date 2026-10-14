# Como passar o VEBRAM para o Claude

## Conectar o código

1. No Claude, abra um chat e use **+ > Add from GitHub**; em um Project, use **conhecimento > + > GitHub**.
2. Autorize sua conta no GitHub. Como o repositório é privado, conceda acesso ao repositório **lucasfilipemuniz12/automacao-arquitetonica-revit**.
3. Escolha a branch **codex/vebram-projeto-legal-v03**, que contém a 0.5.14. Selecione README.md, src/Vebram.Revit, profiles, scripts, tests e docs.
4. Use Sync quando houver atualização. Essa integração sincroniza arquivos de uma branch, não o histórico nem a PR. [Guia oficial](https://support.claude.com/en/articles/10167454-use-the-github-integration).

Para implementar alterações, o Claude Code na web trabalha com o repositório em ambiente remoto e branches próprias. Para testes nativos deste plugin, será preciso um ambiente Windows local com Revit. [Claude Code na web](https://support.claude.com/en/articles/12618689-claude-code-on-the-web).

## Enviar o contexto e o modelo

- Cole **PROMPT_PARA_CLAUDE.txt** no chat.
- Peça que leia **PASSAGEM_CLAUDE.md** e confira a branch.
- Anexe as imagens PNG e **INVENTARIO_MODELAGEM.json** para a avaliação visual/estrutural.
- O **CASA_TERRAS_SO_MODELAGEM.rvt** é para abrir no Revit 2026. Ter o RVT não significa que o Claude conseguirá interpretá-lo ou visualizar sua geometria em um chat.
- Se a sincronização não oferecer a branch correta, extraia **VEBRAM_FONTES_PARA_REVISAO.zip** e forneça as pastas/arquivos diretamente ou abra essa pasta no Claude Code local.

As fontes e docs estão no GitHub; o RVT, inventário detalhado e imagens da casa são entregues localmente. Nada desse modelo será publicado no repositório nesta passagem.

## Pedido inicial sugerido

“Faça primeiro uma revisão técnica e visual independente. Liste o que mudaria, por gravidade e com evidência no código; ainda não implemente. Diferencie limitações conhecidas e hipóteses de bugs comprovados.”

Após avaliar a revisão, peça uma alteração por vez em outra branch e valide o resultado nativo no Revit antes de incorporar à versão que usamos.

## Conteúdo da entrega

CASA_TERRAS_SO_MODELAGEM.rvt: cópia limpa da casa, somente plantas de modelagem e 3D.

PNG: imagens nativas para visualização.

INVENTARIO_MODELAGEM.json: vistas restantes, categorias/elementos e verificação de preservação.

PASSAGEM_CLAUDE.md: situação do plugin, arquitetura, evidências e pendências.

PROMPT_PARA_CLAUDE.txt: pedido pronto para copiar.

VEBRAM_FONTES_PARA_REVISAO.zip: fonte e documentação locais, sem binários nem DLLs Autodesk.
