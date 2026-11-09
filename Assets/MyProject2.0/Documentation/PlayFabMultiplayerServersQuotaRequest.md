# Solicitação de quota — PlayFab Multiplayer Servers

**Título sugerido:** Solicitação de quota para servidores Linux dedicados — Capivarada

Olá, equipe PlayFab,

Estamos solicitando quota inicial para utilizar **PlayFab Multiplayer Servers** no projeto **Capivarada**, atualmente em desenvolvimento.

## Jogo e uso previsto

- Jogo multiplayer competitivo de arena, desenvolvido em Unity 6 com Photon Fusion 2.
- Partidas de até **8 jogadores** por sessão.
- Região inicial: **Brazil South**.
- Build Linux dedicada já gerada e empacotada para testes controlados.
- A demanda inicial é exclusivamente de desenvolvimento, QA e validação técnica, com baixo volume de sessões simultâneas.

## Por que precisamos de um servidor dedicado

O protótipo em Shared Mode já funciona para testes de gameplay, mas o cliente não será usado como fonte confiável de resultados. Precisamos de autoridade de servidor para validar:

- entrada e composição da partida;
- mortes, eliminações e sobreviventes;
- colocação final e vitória;
- concessão de XP, nível, partidas, eliminações, vitórias e moeda;
- atualização segura das leaderboards.

O servidor dedicado enviará somente o resultado consolidado e autenticado para uma Azure Function, que então atualiza os serviços PlayFab. Jogadores visitantes e partidas de treino não recebem qualquer persistência ou recompensa.

## Quota inicial solicitada

- **2 cores** na região Brazil South;
- capacidade suficiente para validar inicialmente uma ou duas partidas simultâneas de até 8 jogadores;
- expansão gradual conforme o jogo avance para testes públicos.

Podemos fornecer o build Linux, configuração de portas, script de inicialização, logs e qualquer informação adicional necessária.

Obrigado.
