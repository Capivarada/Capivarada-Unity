using Fusion;
using UnityEngine;

/// <summary>
/// Spawns exactly the local peer's player object once the Fusion game scene is
/// available. Each Shared peer owns the state authority for its own player.
/// </summary>
public sealed class FusionPlayerSpawner : NetworkBehaviour, IPlayerJoined
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private bool localPlayerSpawned;

    public override void Spawned()
    {
        // Scene callbacks can have happened before this scene object spawned.
        // Explicitly inspect the local player so joining an existing room works.
        SpawnLocalPlayerIfNeeded(Runner.LocalPlayer);
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer) SpawnLocalPlayerIfNeeded(player);
    }

    private void SpawnLocalPlayerIfNeeded(PlayerRef player)
    {
        if (localPlayerSpawned || playerPrefab == null || Runner == null) return;
        if (player != Runner.LocalPlayer) return;
        localPlayerSpawned = true;

        // PlayerRef IDs begin at 1; use a zero-based deterministic slot.
        Transform point = GetSpawnPoint(Mathf.Max(0, player.PlayerId - 1));
        Vector3 position = point != null ? point.position : Vector3.zero;
        Quaternion rotation = point != null ? point.rotation : Quaternion.identity;
        Runner.Spawn(playerPrefab, position, rotation, player);
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[Mathf.Abs(index) % spawnPoints.Length];
    }
}
