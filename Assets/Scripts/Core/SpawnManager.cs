using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<Transform> spawnPoints = new();

    private readonly HashSet<int> occupiedSpawns = new();

    public Transform GetAvailableSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[SpawnManager] Nenhum spawn point disponível.");
            return null;
        }

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (!occupiedSpawns.Contains(i))
            {
                occupiedSpawns.Add(i);
                Debug.Log($"[SpawnManager] Spawn point {i} alocado.");
                return spawnPoints[i];
            }
        }

        Debug.LogWarning("[SpawnManager] Todos os spawn points estão ocupados!");
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    public void ReleaseSpawnPoint(Transform spawnPoint)
    {
        int index = spawnPoints.IndexOf(spawnPoint);

        if (index >= 0 && occupiedSpawns.Contains(index))
        {
            occupiedSpawns.Remove(index);
            Debug.Log($"[SpawnManager] Spawn point {index} liberado.");
        }
    }

    public int GetTotalSpawnPoints()
    {
        return spawnPoints.Count;
    }

    public void ResetSpawns()
    {
        occupiedSpawns.Clear();
        Debug.Log("[SpawnManager] Todos os spawns foram resetados.");
    }
}
