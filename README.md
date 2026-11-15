# Capivarada

Capivarada é um jogo multiplayer de arena para até oito participantes. Os jogadores controlam capivaras personalizáveis, sobrevivem às rodadas da arena e evoluem perfil, ranking e itens cosméticos através de PlayFab, Photon Fusion e Azure Functions.

## Componentes

| Componente | Responsabilidade |
| --- | --- |
| Unity 6 | Cliente, UI, gameplay, personalização e build dedicado Linux. |
| Photon Fusion | Simulação cliente-servidor, spawn e replicação da arena. |
| PlayFab MPS | Alocação do servidor dedicado EastUS. |
| Azure Functions | Alocação, tokens de entrada, settlement, ranking e perfis públicos. |
| PlayFab | Autenticação, perfis públicos, estatísticas, Economy e MPS. |
| Vivox | Chat global, chat de grupo e voz. |

## Requisitos locais

- Unity `6000.6.7f1`.
- .NET SDK 10 para `Capivarada-OAuth`.
- Azure Functions Core Tools e Azure CLI autenticados para publicar o backend.
- Uma conta/configuração Photon Fusion com as regiões `us` habilitada.
- Build PlayFab MPS publicada exclusivamente em `EastUs`.

## Execução local

1. Abra `Capivarada-Unity` pelo Unity Hub na versão indicada.
2. Abra `Capivarada-OAuth` e configure os segredos somente em `local.settings.json`.
3. Execute `dotnet build --no-restore` no backend.
4. Execute `func start` no backend quando precisar testar endpoints locais.
5. Use `Boot_Scene` para o fluxo de autenticação e lobby.

## Multiplayer dedicado

O botão Jogar chama `POST /api/matches/allocate`. O backend reutiliza uma sessão ativa do jogador ou preenche uma sessão com vaga; somente quando necessário pede um novo servidor MPS. O servidor e o cliente fixam a região Photon `us`, correspondente a EastUS.

A rodada só inicia quando ao menos um humano entrou. Ao último humano sair, o servidor encerra a rodada no caminho autoritativo, envia o settlement e deixa o MPS encerrar a sessão vazia.

## Configuração do backend

No Function App, configure ao menos:

- `PLAYFAB_TITLE_ID`
- `PLAYFAB_SECRET_KEY`
- `CAPIVARADA_MPS_BUILD_ID`
- `CAPIVARADA_MATCH_TOKEN_SECRET`
- `CAPIVARADA_MATCH_SERVER_SECRET`
- `STATISTIC_PLAYER_PERFORMANCE`

No build MPS, referencie o segredo de settlement com o mesmo valor de `CAPIVARADA_MATCH_SERVER_SECRET`. O servidor aceita os nomes de ambiente `PF_MPS_SECRET_CAPIVARADA-MATCH-SERVER-SECRET` e `PF_MPS_SECRET_CAPIVARADA_MATCH_SERVER_SECRET`.

## Publicação do backend

```powershell
dotnet build --no-restore
func azure functionapp publish capivarada-functions-dev
```

Verifique depois:

```powershell
curl.exe https://capivarada-functions-dev-d9djf2f2dqe4b9hu.westus2-01.azurewebsites.net/api/health
```

## Validação mínima

1. Login e carregamento de perfil criam a entrada de ranking, inclusive com estatísticas zero.
2. Dois clientes entram na mesma alocação EastUS e veem os cosméticos um do outro.
3. Uma alocação sem conexão de humano não inicia bots.
4. Ao último humano sair, a partida é encerrada e o settlement retorna HTTP 200.
5. Chat global funciona; a aba Grupo fica desabilitada fora de um grupo.

## Segurança

Nunca versione `local.settings.json`, tokens PlayFab, segredo MPS ou credenciais Azure. O cliente recebe apenas tokens curtos de entrada de partida; resultados, XP e pontos são calculados no servidor.


