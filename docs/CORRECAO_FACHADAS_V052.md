# Correção 0.5.2 — marcadores compartilhados

O marcador de elevação do Revit pode controlar várias fachadas. A versão anterior bloqueava essa organização comum. Agora o gerador cria uma elevação nativa independente para cada fachada compartilhada e mantém as vistas originais, com seus IDs e marcações, no grupo 00 RESERVA VEBRAM. O novo conjunto recebe os nomes legais originais e o perfil gráfico; execuções seguintes reutilizam o conjunto independente.

Fachadas compartilhadas já colocadas em folhas e marcações manuais externas ao gerador continuam protegidas: o comando recusa migração automática nesses casos. A criação e a preparação ocorrem numa transação única; falhas revertem a migração.

Compilação: zero erros e zero avisos. Versão 0.5.2 instalada em current/Vebram.Revit.dll após Lucas salvar e fechar Revit, com backup da versão anterior e verificação de liberação exclusiva da DLL. SHA-256: 509F1F06034794BAE508BA5ECF4E991BFA8844A550CA2976D1D6488A07123F3A. Caminho e AddIn GUID preservados. Nenhum job de inicialização ativado.

Ainda requer execução e revisão visual no Revit; instalação/compilação não equivalem a teste nativo aprovado. Reabrir o projeto de teste e usar Projeto legal > Preparar legal > Fachadas.

Referência de API: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/2f707487-817c-d648-4b99-559f9000f123.htm
