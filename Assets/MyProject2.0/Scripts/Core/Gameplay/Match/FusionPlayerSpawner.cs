using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Spawns local objects in Shared tests and every connected player object in a
/// dedicated server session. The latter is the only path used for persistence.
/// </summary>
public sealed class FusionPlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private bool localPlayerSpawned;
    private readonly Dictionary<PlayerRef, NetworkObject> serverPlayers = new();

    public override void Spawned()
    {
        if (IsDedicatedServer())
        {
            foreach (PlayerRef player in Runner.ActivePlayers) SpawnServerPlayerIfNeeded(player);
            return;
        }

        // Scene callbacks can have happened before this scene object spawned.
        // Explicitly inspect the local player so joining an existing room works.
        SpawnLocalPlayerIfNeeded(Runner.LocalPlayer);
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (IsDedicatedServer())
        {
            SpawnServerPlayerIfNeeded(player);
            return;
        }
        if (player == Runner.LocalPlayer) SpawnLocalPlayerIfNeeded(player);
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!IsDedicatedServer() || !serverPlayers.TryGetValue(player, out NetworkObject spawned)) return;
        serverPlayers.Remove(player);
        if (spawned != null) Runner.Despawn(spawned);
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

    private void SpawnServerPlayerIfNeeded(PlayerRef player)
    {
        if (playerPrefab == null || Runner == null || serverPlayers.ContainsKey(player)) return;
        Transform point = GetSpawnPoint(Mathf.Max(0, player.PlayerId - 1));
        Vector3 position = point != null ? point.position : Vector3.zero;
        Quaternion rotation = point != null ? point.rotation : Quaternion.identity;
        NetworkObject spawned = Runner.Spawn(playerPrefab, position, rotation, player);
        if (spawned != null) serverPlayers[player] = spawned;
    }

    private bool IsDedicatedServer() => Runner != null && Runner.GameMode == GameMode.Server && Runner.IsServer;

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[Mathf.Abs(index) % spawnPoints.Length];
    }
}
