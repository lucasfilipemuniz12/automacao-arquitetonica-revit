# CT04 — hipóteses e conflitos da ficha

Fonte: ficha e DXF da branch `claude/casa-teste-03`, revisão consultada em 29/10/2026.

1. Coordenadas dos ambientes são faces internas; contornos 8 × 8 e 8 × 6,50 são faces externas. Eixos das paredes externas recuam metade da espessura. Divisórias ficam no meio dos intervalos de 10 cm entre ambientes. Não se usa parede cortina.
2. Referência absoluta: térreo 0,00 = 100,40 m. Curvas DXF são lidas em metros e convertidas para esse datum. Vínculo fica na origem; y positivo é norte. Pontos no perímetro completados pela fórmula fornecida.
3. Há um conflito explícito: rampa x=9,60–12,60 / y=0–3 ultrapassa em 0,18 m² o chanfro B–C (x−y=12). A casa e garagem coberta cabem. Não se recortou a rampa para facilitar o teste do plugin.
4. Contrapisos/acabamentos têm espessura total 7 cm e lajes 15 cm, conforme ficha. Materiais vêm dos tipos do template; composição miúda das camadas não foi inventada.
5. O piso do quintal pavimentado imediatamente atrás da casa não tem nível explícito. Adotado o nível do acesso da cozinha (térreo), registrado como hipótese. O Quintal +1,00 é a referência acima do arrimo.
6. Posições exatas de várias portas e janelas não estão cotadas na ficha. Posicionamento no trecho da parede do ambiente, sem alterar largura, altura ou peitoril informados. Revisar com autor antes de emissão.
7. Dimensões/posições dos boxes não estão na ficha. Não se inventou a área dos boxes: essa parte precisa de definição antes da modelagem final de PISO BOX.
8. Espessura do arrimo e muros de divisa não foi fornecida. Usa-se tipo de alvenaria disponível no template, com essa escolha declarada; nenhuma espessura foi tratada como dimensão do projeto recebido.
9. Degraus de ligação da garagem têm espelhos de 20 cm, mas não há comprimento de piso; acesso social tem dois degraus sem dimensões. Esses detalhes devem permanecer pendentes se não houver geometria cotada suficiente no material.
10. Espécies/forma das árvores, portão basculante e caixa d'água exigem famílias correspondentes. Não se apresenta outra espécie, uma porta de giro ou volume arbitrário como se fossem os objetos especificados. Conferir pendências no resultado final.
11. Ficha tem o nome antigo do DXF em uma passagem e as imagens têm cabeçalho “Casa Teste 03”. Foi usado o arquivo efetivamente publicado `Casa_Teste_04_lote_esquina_irregular.dxf`, combinado com as coordenadas da ficha CT04.

Nenhum índice urbanístico foi inferido a partir do município/zoneamento. O polígono real tem 383,70 m²; seu retângulo envolvente não pode substituí-lo no cálculo.
