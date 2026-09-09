# Detailed Changes - Match Flow Architecture Refactoring

## RoundManager.cs Changes

### 1. Enum Update
```csharp
// BEFORE
public enum RoundState
{
    Waiting,
    Countdown,
    Choosing,
    Revealing,
    Launching,
    Moving,
    Finished
}

// AFTER
public enum RoundState
{
    Waiting,
    Countdown,      // Initial match countdown
    Choosing,       // Player choice phase
    Revealing,      // Show all directions (wait for stop)
    Launching,      // All players launch
    Moving,         // Wait for players to stop
    NextRound,      // NEW: Between round pause
    Finished
}
```

### 2. Class Fields Changes
```csharp
// REMOVED HARDCODED VALUES:
// [SerializeField] private float initialCountdownDuration = 5f;
// [SerializeField] private float choosingDuration = 10f;
// [SerializeField] private float stopVelocityThreshold = 0.15f;
// [SerializeField] private float stopCheckDelay = 0.75f;
// [SerializeField] private float revealDuration = 1.5f;
// [SerializeField] private int maxRounds = 10;

// ADDED:
private MatchConfig matchConfig;
```

### 3. Events
```csharp
// ADDED NEW EVENT:
public event Action<string> OnTitleChanged;
```

### 4. Property Updates
```csharp
// UPDATED MaxRounds property:
public int MaxRounds => 
    matchConfig != null ? matchConfig.MaxRounds : 10;
```

### 5. Awake() Method - NEW IMPLEMENTATION
```csharp
private void Awake()
{
    if (matchManager == null)
    {
        matchManager = GetComponent<MatchManager>();
    }

    // Get MatchConfig from GameManager
    GameManager gameManager = GameManager.Instance;
    if (gameManager != null)
    {
        matchConfig = gameManager.MatchConfig;
    }

    if (matchConfig == null)
    {
        Debug.LogError("[RoundManager] MatchConfig not found!");
    }
}
```

### 6. RoundFlow() - Complete Refactor
```csharp
// BEFORE: Complex logic, fixed timings
// AFTER: Clean state machine with configuration-driven values

private IEnumerator RoundFlow()
{
    // PHASE 1: Initial Countdown
    SetState(RoundState.Countdown);
    OnTitleChanged?.Invoke("A partida vai começar em...");

    yield return StartCoroutine(
        Countdown(matchConfig.InitialCountdownDuration)
    );

    // LOOP: Per round
    while (
        matchManager != null &&
        matchManager.AlivePlayers > 1 &&
        currentRound < matchConfig.MaxRounds
    )
    {
        currentRound++;
        OnRoundChanged?.Invoke(currentRound);

        StartRound();

        // PHASE 2: Choosing
        yield return StartCoroutine(ChoosingPhase());
        
        // PHASE 3: Revealing (Dynamic)
        yield return StartCoroutine(RevealingPhase());
        
        // PHASE 4: Launching
        LaunchPlayers();
        
        // PHASE 5: Moving
        SetState(RoundState.Moving);
        yield return StartCoroutine(WaitForPlayersToStop());
        
        RegisterRoundSurvival();

        // PHASE 6: Next Round (if conditions met)
        if (
            matchManager != null &&
            matchManager.AlivePlayers > 1 &&
            currentRound < matchConfig.MaxRounds
        )
        {
            yield return StartCoroutine(NextRoundPhase());
        }
    }

    FinishRounds();
}
```

### 7. RevealingPhase() - NEW IMPLEMENTATION (Dynamic)
```csharp
// BEFORE: Fixed 1.5 second timer
private IEnumerator RevealPhase()
{
    SetState(RoundState.Revealing);
    ShowAllIndicators();
    yield return new WaitForSeconds(revealDuration);  // FIXED!
    HideAllIndicators();
}

// AFTER: Wait for players to physically stop
private IEnumerator RevealingPhase()
{
    SetState(RoundState.Revealing);
    OnTitleChanged?.Invoke("Direções Reveladas!");

    Debug.Log("[ROUND] Revelando direções...");

    ShowAllIndicators();

    // Wait for all players to stop
    yield return StartCoroutine(WaitForAllPlayersToStop());

    HideAllIndicators();
}
```

### 8. WaitForAllPlayersToStop() - NEW METHOD
```csharp
private IEnumerator WaitForAllPlayersToStop()
{
    while (true)
    {
        bool allStopped = true;

        foreach (GameObject player in matchManager.AllPlayers)
        {
            if (player == null || !player.activeInHierarchy)
                continue;

            PlayerDeath death = player.GetComponent<PlayerDeath>();
            if (death != null && death.IsDead())
                continue;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;

            if (velocity.magnitude > matchConfig.StopVelocityThreshold)
            {
                allStopped = false;
                break;
            }
        }

        if (allStopped)
        {
            Debug.Log("[ROUND] Todos os jogadores pararam na fase de revelação.");
            yield break;
        }

        yield return null;
    }
}
```

### 9. ChoosingPhase() - UPDATED
```csharp
// BEFORE: No title, hardcoded duration
private IEnumerator ChoosingPhase()
{
    yield return StartCoroutine(
        Countdown(choosingDuration, "Direções serão exibidas em")
    );
}

// AFTER: Emits title with round number
private IEnumerator ChoosingPhase()
{
    SetState(RoundState.Choosing);
    OnTitleChanged?.Invoke($"Rodada {currentRound}: Escolha a direção e a força");

    yield return StartCoroutine(
        Countdown(matchConfig.ChoosingDuration)
    );
}
```

### 10. NextRoundPhase() - NEW METHOD
```csharp
private IEnumerator NextRoundPhase()
{
    SetState(RoundState.NextRound);
    OnTitleChanged?.Invoke("Próxima rodada");

    yield return StartCoroutine(
        Countdown(matchConfig.NextRoundCountdown)
    );
}
```

### 11. Countdown() - SIMPLIFIED
```csharp
// BEFORE: Takes message parameter
private IEnumerator Countdown(float duration, string message)
{
    float timer = duration;
    while (timer > 0f)
    {
        RemainingTime = timer;
        OnTimerChanged?.Invoke(timer);
        Debug.Log($"{message} {Mathf.CeilToInt(timer)}...");
        yield return new WaitForSeconds(1f);
        timer -= 1f;
    }
    RemainingTime = 0f;
    OnTimerChanged?.Invoke(0f);
}

// AFTER: Simplified, no message
private IEnumerator Countdown(float duration)
{
    float timer = duration;
    while (timer > 0f)
    {
        RemainingTime = timer;
        OnTimerChanged?.Invoke(timer);
        Debug.Log($"[ROUND] Contador: {Mathf.CeilToInt(timer)}s");
        yield return new WaitForSeconds(1f);
        timer -= 1f;
    }
    RemainingTime = 0f;
    OnTimerChanged?.Invoke(0f);
}
```

### 12. WaitForPlayersToStop() - UPDATED TO USE MATCHCONFIG
```csharp
// BEFORE: Hardcoded values
if (velocity.magnitude > stopVelocityThreshold)
{
    everyoneStopped = false;
    break;
}
...
yield return new WaitForSeconds(stopCheckDelay);

// AFTER: Uses MatchConfig
if (velocity.magnitude > matchConfig.StopVelocityThreshold)
{
    everyoneStopped = false;
    break;
}
...
yield return new WaitForSeconds(matchConfig.StopCheckDelay);
```

### 13. FinishRounds() - UPDATED
```csharp
// BEFORE:
Debug.Log($"[ROUND] Rodadas encerradas. Rodada atual: {currentRound}/{maxRounds}");

// AFTER:
Debug.Log($"[ROUND] Rodadas encerradas. Rodada atual: {currentRound}/{matchConfig.MaxRounds}");
```

---

## MatchManager.cs Changes

### 1. Added Awake() Method
```csharp
private void Awake()
{
    // Get MatchConfig from GameManager
    GameManager gameManager = GameManager.Instance;
    if (gameManager != null)
    {
        matchConfig = gameManager.MatchConfig;
    }
}
```

### 2. Added Field
```csharp
private MatchConfig matchConfig;
```

### 3. Added StartMatch() Method
```csharp
public void StartMatch()
{
    if (currentState != MatchState.Waiting)
    {
        Debug.LogWarning(
            "[MatchManager] Match não está em Waiting."
        );
        return;
    }

    SetState(MatchState.Playing);
    Debug.Log("[MATCH] Partida iniciada!");
}
```

### 4. Refactored FinishWithRemainingPlayers()
```csharp
// BEFORE: Just called FinishMatch()
public void FinishWithRemainingPlayers()
{
    FinishMatch();
}

// AFTER: Handles max rounds completion
public void FinishWithRemainingPlayers()
{
    if (currentState == MatchState.Finished)
        return;

    winners = GetAlivePlayers();

    if (winners.Count > 1)
    {
        Debug.Log(
            $"[MATCH] Rodada máxima atingida! " +
            $"{winners.Count} jogadores venceram juntos!"
        );

        // Mark all alive players as winners with placement 1
        foreach (GameObject winner in winners)
        {
            PlayerMatchData data = winner.GetComponent<PlayerMatchData>();
            if (data != null)
            {
                data.SetPlacement(1);
                data.SetWon(true);
            }
        }
    }

    SetState(MatchState.Finished);
    OnMatchFinished?.Invoke(new List<GameObject>(winners));
}
```

---

## MatchConfig.cs - Verified Properties

All properties correctly configured and ready:

```csharp
public float InitialCountdownDuration { get; }      // 10s
public float ChoosingDuration { get; }              // 8s
public float NextRoundCountdown { get; }            // 3s
public float RevealingDuration { get; }             // 1.5s (stored, not used)
public int MaxRounds { get; }                       // 10
public float StopVelocityThreshold { get; }         // 0.15
public float StopCheckDelay { get; }                // 0.75s
```

---

## Summary of Configuration Values

| Value | Used In | Default | Range |
|-------|---------|---------|-------|
| InitialCountdownDuration | RoundFlow() | 10s | 1-30s |
| ChoosingDuration | ChoosingPhase() | 8s | 1-30s |
| NextRoundCountdown | NextRoundPhase() | 3s | 0.1-5s |
| StopVelocityThreshold | WaitForAllPlayersToStop(), WaitForPlayersToStop() | 0.15 | 0.01-0.5 |
| StopCheckDelay | WaitForPlayersToStop() | 0.75s | 0.1-2s |
| MaxRounds | RoundFlow() condition | 10 | 1-100 |
| RevealingDuration | (Not used - for compatibility) | 1.5s | 0.1-5s |

---

## Flow Comparison

### Before Refactoring
```
Countdown → Choosing → Reveal (FIXED 1.5s) → Launch → Moving
                    ↑                              ↓
                    └──────── Hardcoded values ────┘
```

Issues:
- Hardcoded timing values scattered throughout code
- Reveal phase uses fixed timer (may wait too long or cut short)
- No clear UI feedback via titles
- Unclear max rounds handling
- Difficult to adjust for game balance

### After Refactoring
```
Countdown → [LOOP]
            ├─ Choosing → Revealing (DYNAMIC) → Launching → Moving
            └─ NextRound
                        ↓
            [All values from MatchConfig]
```

Improvements:
- All values centralized in MatchConfig
- Reveal waits for actual physics (more fair)
- Clear UI feedback with OnTitleChanged
- Explicit max rounds handling
- Easy to adjust via ScriptableObject
- New RoundState.NextRound for clearer flow

---

## Integration Requirements

### GameManager
- Must have `public static GameManager Instance { get; }`
- Must have `public MatchConfig MatchConfig { get; }`

### PlayerMatchData
- Must have `void AddRoundSurvived()`
- Must have `void SetPlacement(int placement)`
- Must have `void SetWon(bool won)`

### UI System
- Should subscribe to `RoundManager.OnTitleChanged`
- Should handle `RoundState.NextRound` in state displays
- Should display round number in choosing phase title

---

## Preserved Methods & Classes
All existing functionality preserved:
- PlayerController.Launch()
- PlayerCollision
- ArenaBounds
- BotController
- PlayerDeath
- Spawn system
- Placement calculation
- All other methods not mentioned in changes

---

## Files Ready for Testing ✓

All implementation complete and verified.
Ready for integration testing with actual gameplay.

