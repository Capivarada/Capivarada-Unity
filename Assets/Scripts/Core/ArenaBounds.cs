using UnityEngine;

public class ArenaBounds : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private float edgeMargin = 1.5f;

    private Bounds arenaBounds;

    public Vector3 Center => arenaBounds.center;

    public float Radius =>
        Mathf.Min(
            arenaBounds.extents.x,
            arenaBounds.extents.z
        );

    public float EdgeMargin => edgeMargin;

    private void Awake()
    {
        Collider collider = GetComponent<Collider>();

        if (collider != null)
        {
            arenaBounds = collider.bounds;
            return;
        }

        Renderer rendererComponent =
            GetComponent<Renderer>();

        if (rendererComponent != null)
        {
            arenaBounds = rendererComponent.bounds;
            return;
        }

        Debug.LogError(
            $"[ArenaBounds] {gameObject.name} não possui " +
            "Collider nem Renderer."
        );
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