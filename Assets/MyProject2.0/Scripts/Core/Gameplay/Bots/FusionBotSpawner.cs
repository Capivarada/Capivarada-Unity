using Fusion;
using UnityEngine;

/// <summary>
/// Fills vacant slots after the shared match gate starts. Only the scene state
/// authority creates bots, so bots exist once and replicate to every client.
/// </summary>
public sealed class FusionBotSpawner : NetworkBehaviour
{
    [SerializeField] private FusionArenaMatchController match;
    [SerializeField] private NetworkObject botPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private int spawnedBots;
    private bool configurationReported;

    public override void Spawned()
    {
        if (match == null) match = GetComponent<FusionArenaMatchController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || match == null || !match.IsMatchReady) return;
        if (botPrefab == null)
        {
            if (!configurationReported)
            {
                configurationReported = true;
                Debug.LogError("[FusionBotSpawner] Configure o prefab de bot NetworkObject no GameManager.", this);
            }
            return;
        }

        if (spawnedBots >= match.CurrentBotSlots) return;

        int botIndex = spawnedBots++;
        Transform point = GetSpawnPoint(botIndex);
        Vector3 position = point != null ? point.position : Vector3.zero;
        Quaternion rotation = point != null ? point.rotation : Quaternion.identity;
        Runner.Spawn(botPrefab, position, rotation, null, (runner, bot) =>
        {
            FusionNetworkBot bridge = bot.GetComponent<FusionNetworkBot>();
            if (bridge != null) bridge.SetSeed(botIndex + 1);
        });
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[Mathf.Abs(index) % spawnPoints.Length];
    }
}
