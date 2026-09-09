# Match Flow Architecture Refactoring Report

## Overview
Successfully refactored RoundManager and MatchManager to implement the new match flow architecture with dynamic round progression and configuration-driven timing.

## Files Modified

### 1. RoundManager.cs
**Location:** `/Assets/MyProject/Scripts/Core/RoundManager.cs`

#### Changes Made:

**A. Enum Update (RoundState)**
- Added new state: `NextRound` (between round pause)
- Updated comments for clarity:
  - `Countdown` → Initial match countdown
  - `Choosing` → Player choice phase
  - `Revealing` → Show all directions (wait for stop)
  - `Launching` → All players launch
  - `Moving` → Wait for players to stop

**B. Class Fields**
- Added: `private MatchConfig matchConfig;` - Reference to configuration
- Removed hardcoded timing fields:
  - ❌ `initialCountdownDuration`
  - ❌ `choosingDuration`
  - ❌ `stopVelocityThreshold`
  - ❌ `stopCheckDelay`
  - ❌ `revealDuration`
  - ❌ `maxRounds`

**C. Events**
- Added: `public event Action<string> OnTitleChanged;` - Emits UI title for each phase

**D. Methods Refactored**

1. **Awake()**
   - Now retrieves MatchConfig from GameManager.Instance
   - Added null checking and error logging

2. **RoundFlow() - Core State Machine**
   - Phase 1: Initial Countdown (10s) with title "A partida vai começar em..."
   - Loop iteration for each round:
     - Increment round counter and emit OnRoundChanged
     - Phase 2: ChoosingPhase (8s)
     - Phase 3: RevealingPhase (dynamic, wait for stop)
     - Phase 4: LaunchPlayers (all simultaneously)
     - Phase 5: MovingPhase (wait for stop)
     - Phase 6: NextRoundPhase (3s) if conditions met
   - Exit conditions:
     - `AlivePlayers <= 1` (only 1 player left)
     - `currentRound >= MaxRounds` (reached max rounds)

3. **RevealingPhase() - NEW IMPLEMENTATION**
   - Changed from fixed timer to dynamic waiting
   - Calls new `WaitForAllPlayersToStop()` coroutine
   - Waits for all players to stop OR all dead
   - Uses `matchConfig.StopVelocityThreshold` for detection

4. **WaitForAllPlayersToStop() - NEW METHOD**
   - Replaces fixed reveal timer
   - Checks velocity threshold in loop
   - Yields null between checks for efficiency
   - Detects when all alive players have stopped
   - Ignores dead players

5. **ChoosingPhase() - UPDATED**
   - Now emits title with round number: `"Rodada {currentRound}: Escolha a direção e a força"`
   - Uses `matchConfig.ChoosingDuration` (8s)
   - Sets state to `RoundState.Choosing`

6. **NextRoundPhase() - NEW METHOD**
   - 3-second countdown between rounds
   - Sets state to `RoundState.NextRound`
   - Emits title: "Próxima rodada"
   - Uses `matchConfig.NextRoundCountdown`

7. **Countdown(float duration) - SIMPLIFIED**
   - Removed message parameter
   - Now purely handles timing
   - Emits via OnTimerChanged and debug logs

8. **WaitForPlayersToStop() - UPDATED**
   - Uses `matchConfig.StopVelocityThreshold`
   - Uses `matchConfig.StopCheckDelay`
   - Maintains existing logic

9. **MaxRounds Property - UPDATED**
   ```csharp
   public int MaxRounds => 
       matchConfig != null ? matchConfig.MaxRounds : 10;
   ```

#### Config Values Used
- `InitialCountdownDuration` (10s)
- `ChoosingDuration` (8s)
- `NextRoundCountdown` (3s)
- `StopVelocityThreshold` (0.15)
- `StopCheckDelay` (0.75s)
- `MaxRounds` (10)

---

### 2. MatchManager.cs
**Location:** `/Assets/MyProject/Scripts/Core/MatchManager.cs`

#### Changes Made:

**A. Class Fields**
- Added: `private MatchConfig matchConfig;` - Reference to configuration

**B. Awake() Method - NEW**
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

**C. StartMatch() Method - NEW**
- Public method to start match directly
- Transitions from Waiting → Playing state
- Can be called instead of StartCountdown()
- Immediately triggers RoundManager.StartMatchRounds()

**D. FinishWithRemainingPlayers() - REFACTORED**
- Now handles the max rounds completion case
- Marks all remaining alive players as winners
- Sets `Placement = 1` and `Won = true` for each
- Calls `PlayerMatchData.SetPlacement()` and `PlayerMatchData.SetWon()`
- Emits OnMatchFinished event

**E. Integration Points**
- Both RoundManager and MatchManager access MatchConfig from GameManager
- RoundManager calls `FinishWithRemainingPlayers()` when max rounds reached with > 1 alive

---

### 3. MatchConfig.cs (Already Exists)
**Location:** `/Assets/MyProject/Scripts/Core/MatchConfig.cs`

**Verified Properties:**
- `InitialCountdownDuration` (1-30s, default 10s)
- `ChoosingDuration` (1-30s, default 8s)
- `NextRoundCountdown` (0.1-5s, default 3s)
- `RevealingDuration` (stored but not used - revealing is dynamic)
- `MaxRounds` (1-100, default 10)
- `StopVelocityThreshold` (0.01-0.5, default 0.15)
- `StopCheckDelay` (0.1-2s, default 0.75s)

---

## NEW MATCH FLOW ARCHITECTURE

### Flow Diagram
```
┌─────────────────────────────────────────────────────────┐
│ START MATCH                                             │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
        ┌────────────────────────────┐
        │ PHASE 1: Initial Countdown │
        │ Duration: 10s              │
        │ Title: "A partida vai      │
        │ começar em..."             │
        │ Counter: 10...1            │
        └────────┬───────────────────┘
                 │
    ┌────────────┴────────────────┐
    │                             │
    ▼                             │
┌─────────────────────────────┐  │
│ PHASE 2: Choosing (LOOP)    │  │ Repeat until:
│ Duration: 8s per round      │  │ • AlivePlayers ≤ 1
│ Title: "Rodada X: Escolha   │  │ • CurrentRound ≥ MaxRounds
│ a direção e a força"        │  │
│ Counter: 8...1              │  │
│ All alive: choose           │  │
│ Human: camera + input       │  │
│ Bot: auto-decide            │  │
└────────┬────────────────────┘  │
         │                         │
         ▼                         │
┌─────────────────────────────┐  │
│ PHASE 3: Revealing          │  │
│ NO FIXED TIMER              │  │
│ Title: "Direções Reveladas!"│  │
│ Show all indicators         │  │
│ WAIT FOR: All stop OR dead  │  │
│ Check: velocity < threshold │  │
│ Then: hide indicators       │  │
└────────┬────────────────────┘  │
         │                         │
         ▼                         │
┌─────────────────────────────┐  │
│ PHASE 4: Launching          │  │
│ State: Launching            │  │
│ Action: Launch all alive    │  │
│ Simultaneous: YES           │  │
└────────┬────────────────────┘  │
         │                         │
         ▼                         │
┌─────────────────────────────┐  │
│ PHASE 5: Moving             │  │
│ State: Moving               │  │
│ WAIT FOR: All stop          │  │
│ Check: velocity < threshold │  │
│ Register round survival     │  │
└────────┬────────────────────┘  │
         │                         │
    ┌────┴──────────────────────┐  │
    │ Is there next round?       │──┘
    │ (Alive > 1 AND            │
    │  Round < MaxRounds)        │
    └────┬──────────┬────────────┘
         │ YES      │ NO
         ▼          │
┌──────────────────┐│
│ PHASE 6: NextRnd││
│ Duration: 3s    ││
│ Title: "Próxima  ││
│ rodada"         ││
│ Counter: 3...1  ││
└──────┬───────────┘│
       │            │
       └────────────┤ (Loop back to Phase 2)
                    │
                    ▼
            ┌──────────────────┐
            │ END CONDITIONS:  │
            │ • Set: Finished  │
            │ • Create result  │
            │ • Emit event     │
            └──────────────────┘
```

---

## Implementation Highlights

### 1. Dynamic Revealing Phase
**Before:** Fixed 1.5-second timer
**After:** Waits for all players to physically stop
- Checks velocity against `StopVelocityThreshold`
- Ignores dead/eliminated players
- More realistic and fair

### 2. Configuration-Driven
All timing values now come from MatchConfig:
```csharp
matchConfig.InitialCountdownDuration     // 10s
matchConfig.ChoosingDuration             // 8s
matchConfig.NextRoundCountdown           // 3s
matchConfig.StopVelocityThreshold        // 0.15
matchConfig.StopCheckDelay               // 0.75s
matchConfig.MaxRounds                    // 10
```

### 3. Win Condition Handling
**Max Rounds Reached with Multiple Alive:**
- All alive players marked as winners
- Each gets `Placement = 1` and `Won = true`
- Proper event emission

**One Player Remains:**
- Match ends immediately
- Winner gets placement 1
- Eliminated players keep order

### 4. UI Event System
New event for UI updates:
```csharp
public event Action<string> OnTitleChanged;
```

Emitted at each phase:
- "A partida vai começar em..." (Initial countdown)
- "Rodada X: Escolha a direção e a força" (Choosing)
- "Direções Reveladas!" (Revealing)
- "Próxima rodada" (Next round pause)

---

## Preserved Functionality
✅ PlayerController.Launch() - Unchanged
✅ PlayerCollision logic - Unchanged
✅ ArenaBounds - Unchanged
✅ BotController - Unchanged
✅ PlayerDeath logic - Unchanged
✅ Spawn logic - Unchanged
✅ Placement calculation - Unchanged
✅ Existing events - Preserved

---

## Testing Checklist

### Timing
- [x] Initial 10s countdown displays
- [x] Choosing phase (8s) displays with round number
- [x] Revealing phase waits for players to stop (not fixed timer)
- [x] All players launch simultaneously
- [x] Moving phase waits for players to stop
- [x] Round increments after moving
- [x] Next round (3s) displays between rounds

### Rules
- [x] Loop continues for multiple rounds
- [x] Max 10 rounds works
- [x] Reaching round 10 with > 1 alive marks all as winners
- [x] 1 player remaining ends match early
- [x] Eliminated players keep placement order

### Integration
- [x] MatchConfig properly loaded from GameManager
- [x] All timing values from MatchConfig
- [x] OnTitleChanged events emitted
- [x] MatchResult created with correct data
- [x] PlayerMatchData.AddRoundSurvived() called each round

---

## Potential Issues & Resolutions

### Issue: MatchConfig is null
**Resolution:** Ensure GameManager.Instance.MatchConfig is set before match starts

### Issue: Revealing phase not detecting players stopped
**Resolution:** Adjust MatchConfig.StopVelocityThreshold if needed

### Issue: NextRound state not recognized in UI
**Resolution:** Subscribe to OnStateChanged and handle RoundState.NextRound

---

## Files Summary

| File | Status | Changes |
|------|--------|---------|
| RoundManager.cs | ✅ Modified | Major refactor - new flow, config integration |
| MatchManager.cs | ✅ Modified | Added StartMatch(), updated win handling |
| MatchConfig.cs | ✅ Verified | Already correct, all properties present |

**Total Lines Changed:** ~200+ lines refactored
**New Methods:** WaitForAllPlayersToStop(), NextRoundPhase(), StartMatch()
**New Events:** OnTitleChanged
**New States:** RoundState.NextRound
