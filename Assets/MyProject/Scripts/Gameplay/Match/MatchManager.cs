using System;
using System.Collections.Generic;
using UnityEngine;

public enum MatchState
{
    Waiting,
    Playing,
    Finished
}

public class MatchManager : MonoBehaviour
{
    private readonly List<GameObject> allPlayers = new();
    private readonly List<GameObject> eliminatedPlayers = new();

    private readonly Dictionary<GameObject, PlayerDeath> deathTrackers = new();

    // Jogadores que morreram durante o round atual.
    private readonly List<GameObject> currentRoundEliminated = new();

    // Resultado final de cada jogador.
    private readonly Dictionary<GameObject, int> finalPlacements = new();

    private readonly List<GameObject> winners = new();
    private readonly List<MatchResult> results = new();

    private MatchState currentState = MatchState.Waiting;

    public event Action<MatchState> OnStateChanged;
    public event Action<GameObject> OnPlayerEliminated;
    public event Action<IReadOnlyList<MatchResult>> OnMatchFinished;

    public MatchState CurrentState => currentState;

    public IReadOnlyList<GameObject> AllPlayers => allPlayers;
    public IReadOnlyList<GameObject> EliminatedPlayers => eliminatedPlayers;
    public IReadOnlyList<GameObject> Winners => winners;
    public IReadOnlyList<MatchResult> Results => results;

    public int AlivePlayers => GetAlivePlayers().Count;


    // =========================================================
    // RESET
    // =========================================================

    public void Reset()
    {
        foreach (var pair in deathTrackers)
        {
            if (pair.Value != null)
                pair.Value.OnPlayerDeath -= HandlePlayerDeath;
        }

        allPlayers.Clear();
        eliminatedPlayers.Clear();
        deathTrackers.Clear();
        currentRoundEliminated.Clear();
        finalPlacements.Clear();
        winners.Clear();
        results.Clear();

        SetState(MatchState.Waiting);
    }


    // =========================================================
    // REGISTRAR PLAYER
    // =========================================================

    public void RegisterPlayer(GameObject player)
    {
        if (player == null || allPlayers.Contains(player))
            return;

        allPlayers.Add(player);

        PlayerMatchData data =
            player.GetComponent<PlayerMatchData>();

        if (data == null)
            data = player.AddComponent<PlayerMatchData>();

        data.ResetMatchData();

        PlayerDeath death =
            player.GetComponent<PlayerDeath>();

        if (death != null)
        {
            deathTrackers.Add(player, death);
            death.OnPlayerDeath += HandlePlayerDeath;
        }
    }


    public void SetHumanPlayer(GameObject player)
    {
        // Reservado para identificação do jogador humano.
    }


    // =========================================================
    // START MATCH
    // =========================================================

    public void StartMatch()
    {
        if (currentState == MatchState.Waiting)
            SetState(MatchState.Playing);
    }


    // =========================================================
    // PLAYER DEATH
    // =========================================================

    private void HandlePlayerDeath(PlayerDeath death)
    {
        if (currentState == MatchState.Finished)
            return;

        GameObject player = death.gameObject;

        if (!eliminatedPlayers.Contains(player))
        {
            eliminatedPlayers.Add(player);

            // IMPORTANTE:
            // Não damos placement aqui.
            //
            // Isso permite que vários jogadores que morreram
            // no mesmo round sejam tratados como um grupo.
            if (!currentRoundEliminated.Contains(player))
                currentRoundEliminated.Add(player);

            OnPlayerEliminated?.Invoke(player);
        }

        // NÃO finalizamos a partida aqui.
        //
        // O RoundManager vai esperar o movimento terminar
        // e então decidir se foi:
        //
        // - eliminação normal
        // - último sobrevivente
        // - todos morreram
    }


    // =========================================================
    // ALIVE PLAYERS
    // =========================================================

    public List<GameObject> GetAlivePlayers()
    {
        List<GameObject> alive = new();

        foreach (GameObject player in allPlayers)
        {
            if (player == null)
                continue;

            PlayerDeath death =
                player.GetComponent<PlayerDeath>();

            if (death == null || !death.IsDead())
                alive.Add(player);
        }

        return alive;
    }


    // =========================================================
    // FINALIZA ROUND
    // =========================================================

    public void FinishRound()
    {
        if (currentRoundEliminated.Count == 0)
            return;

        int alive = AlivePlayers;

        // Todos os jogadores que morreram neste round
        // recebem a mesma colocação.
        //
        // Exemplo:
        // 8 jogadores
        // 3 morreram
        // 5 continuam vivos
        //
        // colocação = 5 + 1 = 6º
        int placement = alive + 1;

        foreach (GameObject player in currentRoundEliminated)
        {
            if (player == null)
                continue;

            finalPlacements[player] = placement;
        }

        currentRoundEliminated.Clear();
    }


    // =========================================================
    // FINALIZAR PARTIDA
    // =========================================================

    public void FinishMatch(bool sharedVictory)
    {
        if (currentState == MatchState.Finished)
            return;

        List<GameObject> alivePlayers =
            GetAlivePlayers();

        int aliveCount = alivePlayers.Count;


        // =====================================================
        // CASO ESPECIAL:
        // TODO MUNDO MORREU
        // =====================================================

        if (aliveCount == 0)
        {
            // Regra do Rivals Arena:
            //
            // Se todos morreram no mesmo round,
            // todos recebem resultado equivalente ao 3º lugar.
            foreach (GameObject player in allPlayers)
            {
                if (player != null)
                    finalPlacements[player] = 3;
            }

            currentRoundEliminated.Clear();
        }
        else
        {
            // Primeiro fecha o grupo que morreu no round atual.
            FinishRound();


            // =================================================
            // APENAS UM SOBREVIVENTE
            // =================================================

            if (aliveCount == 1)
            {
                GameObject winner = alivePlayers[0];

                finalPlacements[winner] = 1;

                winners.Clear();
                winners.Add(winner);
            }


            // =================================================
            // VÁRIOS SOBREVIVENTES
            // =================================================

            else if (sharedVictory)
            {
                // Exemplo:
                //
                // partida chegou ao máximo de rounds
                // e 3 jogadores continuam vivos.
                //
                // Todos são considerados vencedores.

                winners.Clear();

                foreach (GameObject player in alivePlayers)
                {
                    finalPlacements[player] = 1;
                    winners.Add(player);
                }
            }
        }


        // =====================================================
        // APLICAR RESULTADOS NOS PLAYERS
        // =====================================================

        foreach (GameObject player in allPlayers)
        {
            if (player == null)
                continue;

            PlayerMatchData data =
                player.GetComponent<PlayerMatchData>();

            if (data == null)
                continue;

            bool won = winners.Contains(player);

            int placement = 0;

            if (!finalPlacements.TryGetValue(
                    player,
                    out placement))
            {
                placement = data.Placement;
            }

            data.SetWon(won);
            data.SetPlacement(placement);
        }


        // =====================================================
        // GERAR RESULTADOS
        // =====================================================

        BuildResults();

        SetState(MatchState.Finished);

        OnMatchFinished?.Invoke(results);
    }


    // =========================================================
    // RESULTADO DO PLAYER
    // =========================================================

    public MatchResult GetResultForPlayer(
        GameObject player)
    {
        int index = allPlayers.IndexOf(player);

        if (currentState == MatchState.Finished &&
            index >= 0 &&
            index < results.Count)
        {
            return results[index];
        }

        return BuildResult(player);
    }


    // =========================================================
    // BUILD RESULTS
    // =========================================================

    private void BuildResults()
    {
        results.Clear();

        foreach (GameObject player in allPlayers)
        {
            results.Add(BuildResult(player));
        }
    }


    private MatchResult BuildResult(
        GameObject player)
    {
        PlayerMatchData data =
            player != null
                ? player.GetComponent<PlayerMatchData>()
                : null;

        int placement = 0;

        // Primeiro usa o resultado final calculado
        // pelo MatchManager.
        if (player != null &&
            finalPlacements.TryGetValue(
                player,
                out int savedPlacement))
        {
            placement = savedPlacement;
        }

        // Fallback.
        if (placement <= 0 &&
            data != null &&
            data.Placement > 0)
        {
            placement = data.Placement;
        }

        int kills =
            data != null
                ? data.Kills
                : 0;

        int rounds =
            data != null
                ? data.RoundsSurvived
                : 0;

        bool won =
            data != null &&
            data.Won;

        return new MatchResult(
            placement,
            kills,
            rounds,
            won
        );
    }


    // =========================================================
    // STATE
    // =========================================================

    private void SetState(MatchState state)
    {
        if (currentState == state)
            return;

        currentState = state;

        OnStateChanged?.Invoke(state);
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        foreach (var pair in deathTrackers)
        {
            if (pair.Value != null)
                pair.Value.OnPlayerDeath -= HandlePlayerDeath;
        }
    }
}