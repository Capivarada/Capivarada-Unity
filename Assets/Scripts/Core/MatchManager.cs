using System;
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
    [SerializeField] private float countdownDuration = 3f;

    private MatchState currentState = MatchState.Waiting;

    private readonly List<GameObject> allPlayers = new();
    
    private readonly Dictionary<GameObject, PlayerDeath> playerDeathTrackers = new();

    private GameObject humanPlayer;
    private GameObject lastSurvivor;

    public event Action<MatchState> OnStateChanged;
    public event Action<GameObject> OnPlayerEliminated;
    public event Action<List<GameObject>> OnMatchFinished;

    public MatchState CurrentState => currentState;
    public List<GameObject> AllPlayers => new(allPlayers);
    public int AlivePlayers => GetAlivePlayersCount();

    public void RegisterPlayer(GameObject player)
    {
        if (player == null)
            return;

        if (allPlayers.Contains(player))
        {
            Debug.LogWarning($"[MatchManager] {player.name} já está registrado.");
            return;
        }

        allPlayers.Add(player);

        PlayerDeath deathTracker = player.GetComponent<PlayerDeath>();
        if (deathTracker != null)
        {
            playerDeathTrackers[player] = deathTracker;
            deathTracker.OnPlayerDeath += OnPlayerDied;
        }

        Debug.Log($"[MatchManager] {player.name} registrado. Total: {allPlayers.Count}");
    }

    public void UnregisterPlayer(GameObject player)
    {
        if (player == null || !allPlayers.Contains(player))
            return;

        allPlayers.Remove(player);

        if (playerDeathTrackers.ContainsKey(player))
        {
            PlayerDeath deathTracker = playerDeathTrackers[player];
            deathTracker.OnPlayerDeath -= OnPlayerDied;
            playerDeathTrackers.Remove(player);
        }

        Debug.Log($"[MatchManager] {player.name} removido. Total: {allPlayers.Count}");
    }

    public void SetHumanPlayer(GameObject player)
    {
        humanPlayer = player;
        Debug.Log($"[MatchManager] Jogador humano definido: {player.name}");
    }

    public void StartCountdown()
    {
        if (currentState != MatchState.Waiting)
        {
            Debug.LogWarning("[MatchManager] Match não está em estado Waiting.");
            return;
        }

        SetState(MatchState.Starting);
        StartCoroutine(CountdownCoroutine());
    }

    private System.Collections.IEnumerator CountdownCoroutine()
    {
        for (int i = (int)countdownDuration; i > 0; i--)
        {
            Debug.Log($"[MATCH] Começando em {i}...");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("[MATCH] GO!");
        SetState(MatchState.Playing);
    }

    private void OnPlayerDied(PlayerDeath deathTracker)
    {
        GameObject deadPlayer = deathTracker.gameObject;

        Debug.Log($"[MatchManager] {deadPlayer.name} foi eliminado!");
        OnPlayerEliminated?.Invoke(deadPlayer);

        if (deadPlayer == humanPlayer)
        {
            Debug.Log("[MATCH] Jogador eliminado.");
            Debug.Log("[MATCH] Você perdeu.");
        }

    }
    
    private int GetAlivePlayersCount()
    {
        int count = 0;

        foreach (GameObject player in allPlayers)
        {
            if (player != null && player.activeInHierarchy)
            {
                PlayerDeath deathTracker = player.GetComponent<PlayerDeath>();
                if (deathTracker != null && !deathTracker.IsDead())
                {
                    count++;
                }
            }
        }

        return count;
    }

    private GameObject GetLastSurvivor()
    {
        foreach (GameObject player in allPlayers)
        {
            if (player != null && player.activeInHierarchy)
            {
                PlayerDeath deathTracker = player.GetComponent<PlayerDeath>();
                if (deathTracker != null && !deathTracker.IsDead())
                {
                    return player;
                }
            }
        }

        return null;
    }
    public List<GameObject> GetAlivePlayers()
    {
        List<GameObject> alivePlayers = new();

        foreach (GameObject player in allPlayers)
        {
            if (player == null || !player.activeInHierarchy)
                continue;

            PlayerDeath death = player.GetComponent<PlayerDeath>();

            if (death != null && death.IsDead())
                continue;

            alivePlayers.Add(player);
        }

        return alivePlayers;
    }
    public void FinishMatch()
    {
        if (currentState == MatchState.Finished)
            return;

        List<GameObject> winners =
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
                $"[MATCH] Vencedor: {winners[0].name}"
            );
        }
        else
        {
            Debug.Log(
                $"[MATCH] Vitória compartilhada! " +
                $"{winners.Count} jogadores venceram."
            );

            foreach (GameObject winner in winners)
            {
                Debug.Log(
                    $"[MATCH] Vencedor: {winner.name}"
                );
            }
        }

        SetState(
            MatchState.Finished
        );

        OnMatchFinished?.Invoke(
            winners
        );
    }

    private void SetState(MatchState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        OnStateChanged?.Invoke(currentState);

        Debug.Log($"[Match] Estado: {currentState}");
    }

    public void Reset()
    {
        StopAllCoroutines();

        allPlayers.Clear();
        playerDeathTrackers.Clear();

        humanPlayer = null;
        lastSurvivor = null;

        SetState(MatchState.Waiting);

        Debug.Log("[MatchManager] Resetado.");
    }
}
