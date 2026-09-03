using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MatchState
{
    Waiting,
    Starting,
    Playing,
    Finished
}

public class MatchManager : MonoBehaviour
{
    [Header("Match Configuration")]
    [SerializeField]
    private float countdownDuration = 3f;

    private MatchState currentState =
        MatchState.Waiting;

    private readonly List<GameObject> allPlayers =
        new();

    // Jogadores na ordem em que foram eliminados.
    private readonly List<GameObject> eliminatedPlayers =
        new();

    private readonly Dictionary<
        GameObject,
        PlayerDeath
    > playerDeathTrackers =
        new();

    private GameObject humanPlayer;

    private List<GameObject> winners =
        new();

    public event Action<MatchState>
        OnStateChanged;

    public event Action<GameObject>
        OnPlayerEliminated;

    // Agora envia TODOS os vencedores.
    public event Action<List<GameObject>>
        OnMatchFinished;


    public MatchState CurrentState =>
        currentState;

    public List<GameObject> AllPlayers =>
        new(allPlayers);

    public List<GameObject> EliminatedPlayers =>
        new(eliminatedPlayers);

    public List<GameObject> Winners =>
        new(winners);

    public int AlivePlayers =>
        GetAlivePlayersCount();


    #region PLAYER REGISTRATION

    public void RegisterPlayer(
        GameObject player
    )
    {
        if (player == null)
            return;

        if (allPlayers.Contains(player))
        {
            Debug.LogWarning(
                $"[MatchManager] " +
                $"{player.name} já está registrado."
            );

            return;
        }

        allPlayers.Add(player);

        PlayerDeath deathTracker =
            player.GetComponent<PlayerDeath>();

        if (deathTracker != null)
        {
            playerDeathTrackers[player] =
                deathTracker;

            deathTracker.OnPlayerDeath +=
                OnPlayerDied;
        }

        Debug.Log(
            $"[MatchManager] " +
            $"{player.name} registrado. " +
            $"Total: {allPlayers.Count}"
        );
    }


    public void UnregisterPlayer(
        GameObject player
    )
    {
        if (
            player == null ||
            !allPlayers.Contains(player)
        )
        {
            return;
        }

        allPlayers.Remove(player);

        eliminatedPlayers.Remove(player);

        if (
            playerDeathTrackers.TryGetValue(
                player,
                out PlayerDeath deathTracker
            )
        )
        {
            deathTracker.OnPlayerDeath -=
                OnPlayerDied;

            playerDeathTrackers.Remove(
                player
            );
        }

        Debug.Log(
            $"[MatchManager] " +
            $"{player.name} removido."
        );
    }


    public void SetHumanPlayer(
        GameObject player
    )
    {
        humanPlayer = player;

        Debug.Log(
            $"[MatchManager] " +
            $"Jogador humano definido: " +
            $"{player.name}"
        );
    }

    #endregion


    #region MATCH START

    public void StartCountdown()
    {
        if (
            currentState !=
            MatchState.Waiting
        )
        {
            Debug.LogWarning(
                "[MatchManager] " +
                "Match não está em Waiting."
            );

            return;
        }

        SetState(
            MatchState.Starting
        );

        StartCoroutine(
            CountdownCoroutine()
        );
    }


    private IEnumerator CountdownCoroutine()
    {
        for (
            int i =
                Mathf.CeilToInt(
                    countdownDuration
                );
            i > 0;
            i--
        )
        {
            Debug.Log(
                $"[MATCH] Começando em {i}..."
            );

            yield return new WaitForSeconds(
                1f
            );
        }

        Debug.Log("[MATCH] GO!");

        SetState(
            MatchState.Playing
        );
    }

    #endregion


    #region PLAYER DEATH

    private void OnPlayerDied(
        PlayerDeath deathTracker
    )
    {
        GameObject deadPlayer =
            deathTracker.gameObject;

        if (
            !eliminatedPlayers.Contains(
                deadPlayer
            )
        )
        {
            eliminatedPlayers.Add(
                deadPlayer
            );
        }

        Debug.Log(
            $"[MatchManager] " +
            $"{deadPlayer.name} foi eliminado!"
        );

        OnPlayerEliminated?.Invoke(
            deadPlayer
        );

        if (deadPlayer == humanPlayer)
        {
            Debug.Log(
                "[MATCH] Você foi eliminado."
            );
        }

        CheckForWinner();
    }

    #endregion


    #region WIN CONDITION

    private void CheckForWinner()
    {
        if (
            currentState ==
            MatchState.Finished
        )
        {
            return;
        }

        if (AlivePlayers <= 1)
        {
            FinishMatch();
        }
    }


    public void FinishMatch()
    {
        if (
            currentState ==
            MatchState.Finished
        )
        {
            return;
        }

        winners =
            GetAlivePlayers();

        if (winners.Count == 0)
        {
            Debug.Log(
                "[MATCH] Todos foram eliminados."
            );
        }
        else if (winners.Count == 1)
        {
            Debug.Log(
                $"[MATCH] Vencedor: " +
                $"{winners[0].name}"
            );
        }
        else
        {
            Debug.Log(
                $"[MATCH] Vitória compartilhada! " +
                $"{winners.Count} jogadores."
            );

            foreach (
                GameObject winner
                in winners
            )
            {
                Debug.Log(
                    $"[MATCH] Vencedor: " +
                    $"{winner.name}"
                );
            }
        }

        SetState(
            MatchState.Finished
        );

        OnMatchFinished?.Invoke(
            new List<GameObject>(
                winners
            )
        );
    }


    // Chamado pelo RoundManager
    // quando chegar à rodada máxima.
    public void FinishWithRemainingPlayers()
    {
        FinishMatch();
    }

    #endregion


    #region ALIVE PLAYERS

    public List<GameObject>
        GetAlivePlayers()
    {
        List<GameObject> alivePlayers =
            new();

        foreach (
            GameObject player
            in allPlayers
        )
        {
            if (
                player == null ||
                !player.activeInHierarchy
            )
            {
                continue;
            }

            PlayerDeath death =
                player.GetComponent<PlayerDeath>();

            if (death == null)
                continue;

            if (!death.IsDead())
            {
                alivePlayers.Add(
                    player
                );
            }
        }

        return alivePlayers;
    }


    private int GetAlivePlayersCount()
    {
        return GetAlivePlayers().Count;
    }

    #endregion


    #region PLACEMENT

    public int GetPlacement(
        GameObject player
    )
    {
        if (player == null)
            return 0;

        int totalPlayers =
            allPlayers.Count;

        // Se o jogador terminou vivo,
        // ele ficou em primeiro lugar.
        if (
            winners.Contains(player)
        )
        {
            return 1;
        }

        int eliminationIndex =
            eliminatedPlayers.IndexOf(
                player
            );

        if (eliminationIndex < 0)
            return 0;

        /*
         * Com 8 jogadores:
         *
         * Primeiro eliminado = 8º
         * Segundo eliminado  = 7º
         * ...
         * Último eliminado   = 2º
         */

        return
            totalPlayers -
            eliminationIndex;
    }

    #endregion


    #region STATE

    private void SetState(
        MatchState newState
    )
    {
        if (
            currentState ==
            newState
        )
        {
            return;
        }

        currentState =
            newState;

        OnStateChanged?.Invoke(
            currentState
        );

        Debug.Log(
            $"[MATCH] Estado: " +
            $"{currentState}"
        );
    }

    #endregion


    #region RESET

    public void Reset()
    {
        StopAllCoroutines();

        // Remove eventos antes
        // de limpar o Dictionary.
        foreach (
            KeyValuePair<
                GameObject,
                PlayerDeath
            > pair
            in playerDeathTrackers
        )
        {
            if (pair.Value != null)
            {
                pair.Value.OnPlayerDeath -=
                    OnPlayerDied;
            }
        }

        allPlayers.Clear();

        eliminatedPlayers.Clear();

        playerDeathTrackers.Clear();

        winners.Clear();

        humanPlayer = null;

        SetState(
            MatchState.Waiting
        );

        Debug.Log(
            "[MatchManager] Resetado."
        );
    }

    #endregion
}