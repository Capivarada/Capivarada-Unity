using System;
using System.Collections;
using UnityEngine;

public enum MatchmakingState
{
    Idle,
    Searching,
    MatchFound,
    FillingWithBots,
    Ready
}

public class MatchmakingManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float searchDuration = 10f;

    private MatchmakingState currentState = MatchmakingState.Idle;

    public event Action<MatchmakingState> OnStateChanged;

    public MatchmakingState CurrentState => currentState;

    public void StartSearching()
    {
        if (currentState != MatchmakingState.Idle)
        {
            Debug.LogWarning("[Matchmaking] Uma busca já está em andamento.");
            return;
        }

        Debug.Log("[MATCH] Procurando partida...");
        SetState(MatchmakingState.Searching);

        StartCoroutine(SearchCoroutine());
    }

    private IEnumerator SearchCoroutine()
    {
        yield return new WaitForSeconds(searchDuration);

        Debug.Log("[MATCH] Nenhum jogador encontrado.");
        SetState(MatchmakingState.FillingWithBots);

        yield return new WaitForSeconds(0.5f);

        Debug.Log("[MATCH] Preenchendo com bots...");
        SetState(MatchmakingState.Ready);
    }

    private void SetState(MatchmakingState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        OnStateChanged?.Invoke(currentState);

        Debug.Log($"[Matchmaking] Estado: {currentState}");
    }

    public void Reset()
    {
        StopAllCoroutines();
        SetState(MatchmakingState.Idle);
        Debug.Log("[Matchmaking] Resetado.");
    }
}
