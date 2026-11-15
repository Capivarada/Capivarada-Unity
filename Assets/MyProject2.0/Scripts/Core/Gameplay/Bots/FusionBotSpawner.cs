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
        Transform point = GetFreeSpawnPoint(botIndex);
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

    private Transform GetFreeSpawnPoint(int botIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        // PlayerRef values are not contiguous (the first remote client is
        // commonly Player:2), therefore CurrentHumanSlots is not a spawn-slot
        // index. Pick the first point not occupied by an existing human/bot.
        const float minimumSeparation = 1.5f;
        float squaredSeparation = minimumSeparation * minimumSeparation;
        for (int offset = 0; offset < spawnPoints.Length; offset++)
        {
            Transform candidate = GetSpawnPoint(botIndex + offset);
            if (candidate == null) continue;
            bool occupied = false;
            foreach (FusionNetworkPlayer player in FindObjectsByType<FusionNetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if ((player.transform.position - candidate.position).sqrMagnitude < squaredSeparation) { occupied = true; break; }
            if (!occupied)
                foreach (FusionNetworkBot bot in FindObjectsByType<FusionNetworkBot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if ((bot.transform.position - candidate.position).sqrMagnitude < squaredSeparation) { occupied = true; break; }
            if (!occupied) return candidate;
        }

        Debug.LogWarning("[FusionBotSpawner] Todos os pontos de spawn estão ocupados; usando o próximo ponto disponível.", this);
        return GetSpawnPoint(botIndex);
    }
}
