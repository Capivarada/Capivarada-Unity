using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaBounds : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private float edgeMargin = 1.5f;
    [SerializeField, Min(0.1f)] private float shrinkPerRound = 2.5f;
    [SerializeField, Min(0f)] private float playerEdgeInset = 0.65f;

    private Bounds arenaBounds;
    private Transform arenaTransform;
    private Vector3 initialArenaScale;
    private bool isInitialized;

    public Vector3 Center => arenaBounds.center;

    public float Radius =>
        Mathf.Min(
            arenaBounds.extents.x,
            arenaBounds.extents.z
        );

    public float EdgeMargin => edgeMargin;
    public Vector3 InitialArenaScale => initialArenaScale;
    public Vector3 CurrentArenaScale => arenaTransform != null ? arenaTransform.localScale : initialArenaScale;

    private void Awake()
    {
        // This child is the deliberate, invisible boundary marker. Its mesh
        // excludes the empty area of the visual parent arena, so never use
        // the parent's renderer bounds as the gameplay limit.
        MeshFilter markerMesh = GetComponent<MeshFilter>();
        if (markerMesh == null || markerMesh.sharedMesh == null)
        {
            Debug.LogError($"[ArenaBounds] {gameObject.name} precisa de um MeshFilter com o plano que delimita a arena.");
            return;
        }

        // The parent is still the visual/physical arena object that shrinks.
        arenaTransform = transform.parent != null ? transform.parent : transform;

        initialArenaScale = arenaTransform.localScale;
        arenaBounds = GetMarkerWorldBounds(markerMesh.sharedMesh.bounds);
        isInitialized = true;
    }

    public void ResetArena()
    {
        if (!isInitialized) return;
        SetArenaScale(initialArenaScale);
    }

    /// <summary>Animates the arena to the requested round size and carries players at the edge with it.</summary>
    public IEnumerator ShrinkForRound(int roundNumber, float duration, IEnumerable<GameObject> players)
    {
        if (!isInitialized || roundNumber <= 1)
            yield break;

        Vector3 startScale = arenaTransform.localScale;
        float reduction = shrinkPerRound * (roundNumber - 1);
        Vector3 targetScale = new Vector3(
            Mathf.Max(0.1f, initialArenaScale.x - reduction),
            initialArenaScale.y,
            Mathf.Max(0.1f, initialArenaScale.z - reduction));

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            SetArenaScale(Vector3.Lerp(startScale, targetScale, Mathf.Clamp01(elapsed / safeDuration)));
            KeepPlayersInside(players);
            yield return null;
        }

        SetArenaScale(targetScale);
        KeepPlayersInside(players);
    }

    public Vector3 GetScaleForRound(int roundNumber, float reductionPerRound)
    {
        float reduction = Mathf.Max(0f, reductionPerRound) * Mathf.Max(0, roundNumber - 1);
        return new Vector3(
            Mathf.Max(0.1f, initialArenaScale.x - reduction),
            initialArenaScale.y,
            Mathf.Max(0.1f, initialArenaScale.z - reduction));
    }

    /// <summary>Applies a replicated Fusion arena scale on every peer.</summary>
    public void ApplyNetworkScale(Vector3 scale)
    {
        if (!isInitialized) return;
        SetArenaScale(scale);
    }

    /// <summary>
    /// Applies the replicated scale and mirrors the practice-mode behavior by
    /// keeping living participants inside the newly reduced arena.
    /// </summary>
    public void ApplyNetworkScale(Vector3 scale, IEnumerable<GameObject> players)
    {
        if (!isInitialized) return;
        SetArenaScale(scale);
        KeepPlayersInside(players);
    }

    private void SetArenaScale(Vector3 scale)
    {
        arenaTransform.localScale = scale;
        MeshFilter markerMesh = GetComponent<MeshFilter>();
        if (markerMesh != null && markerMesh.sharedMesh != null)
            arenaBounds = GetMarkerWorldBounds(markerMesh.sharedMesh.bounds);
    }

    private Bounds GetMarkerWorldBounds(Bounds localBounds)
    {
        Vector3 center = transform.TransformPoint(localBounds.center);
        Vector3 extents = localBounds.extents;
        Bounds worldBounds = new Bounds(center, Vector3.zero);

        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
            worldBounds.Encapsulate(transform.TransformPoint(localBounds.center + Vector3.Scale(extents, new Vector3(x, y, z))));

        return worldBounds;
    }

    private void KeepPlayersInside(IEnumerable<GameObject> players)
    {
        if (players == null) return;

        float minX = arenaBounds.min.x + playerEdgeInset;
        float maxX = arenaBounds.max.x - playerEdgeInset;
        float minZ = arenaBounds.min.z + playerEdgeInset;
        float maxZ = arenaBounds.max.z - playerEdgeInset;

        foreach (GameObject player in players)
        {
            if (player == null || !player.activeInHierarchy) continue;

            Vector3 position = player.transform.position;
            Vector3 clamped = new Vector3(
                Mathf.Clamp(position.x, minX, maxX),
                position.y,
                Mathf.Clamp(position.z, minZ, maxZ));
            if ((clamped - position).sqrMagnitude <= 0.000001f) continue;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = clamped;
                // Eliminated network bodies are intentionally kinematic. Unity
                // warns every Render frame when their velocity is assigned.
                if (!body.isKinematic)
                    body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
            }
            else
            {
                player.transform.position = clamped;
            }
        }
    }

    public float GetDistanceFromEdge(Vector3 position)
    {
        float distanceX =
            arenaBounds.extents.x -
            Mathf.Abs(
                position.x -
                arenaBounds.center.x
            );

        float distanceZ =
            arenaBounds.extents.z -
            Mathf.Abs(
                position.z -
                arenaBounds.center.z
            );

        return Mathf.Min(
            distanceX,
            distanceZ
        );
    }

    public bool IsNearEdge(Vector3 position)
    {
        return GetDistanceFromEdge(position) <= edgeMargin;
    }

    public Vector3 GetDirectionToCenter(Vector3 position)
    {
        Vector3 direction =
            arenaBounds.center -
            position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return Vector3.forward;

        return direction.normalized;
    }
}
