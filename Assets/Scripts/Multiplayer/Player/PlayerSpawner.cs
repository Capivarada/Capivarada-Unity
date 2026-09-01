using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private NetworkRunner runner;

    [Header("Player")] [SerializeField] private NetworkPrefabRef playerPrefab;

    [Header("Spawn Points")] [SerializeField]
    private Transform[] spawnPoints;

    private bool playerSpawned;

    private void Start()
    {
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        Debug.Log(
            $"[PlayerSpawner] Runner: {runner}"
        );

        TrySpawnLocalPlayer();
    }

    private void Update()
    {
        if (playerSpawned)
            return;

        TrySpawnLocalPlayer();
    }

    private void TrySpawnLocalPlayer()
    {
        if (runner == null)
            return;

        if (!runner.IsRunning)
            return;

        if (!runner.IsConnectedToServer)
            return;

        if (!runner.LocalPlayer.IsValid)
            return;

        Debug.Log(
            $"[PlayerSpawner] Runner pronto! " +
            $"LocalPlayer: {runner.LocalPlayer}"
        );

        SpawnPlayer(runner.LocalPlayer);
    }

    private void SpawnPlayer(PlayerRef player)
    {
        if (playerSpawned)
            return;

        if (!playerPrefab.IsValid)
        {
            Debug.LogError(
                "[Fusion] Player Prefab inválido."
            );

            return;
        }

        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "[Fusion] Nenhum Spawn Point configurado."
            );

            return;
        }

        Transform spawnPoint =
            spawnPoints[
                Random.Range(0, spawnPoints.Length)
            ];

        NetworkObject playerObject =
            runner.Spawn(
                playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player
            );

        if (playerObject == null)
        {
            Debug.LogError(
                "[Fusion] Runner.Spawn retornou null."
            );

            return;
        }

        runner.SetPlayerObject(
            player,
            playerObject
        );

        playerSpawned = true;

        Debug.Log(
            $"[Fusion] PLAYER SPAWNADO!\n" +
            $"PlayerRef: {player}\n" +
            $"Objeto: {playerObject.name}\n" +
            $"Spawn: {spawnPoint.name}"
        );
    }

    private void SetNickname()
    {
    }
}