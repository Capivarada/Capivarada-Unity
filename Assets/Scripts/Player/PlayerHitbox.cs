using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private PlayerCollision playerCollision;

    private void Awake()
    {
        if (playerCollision == null)
            playerCollision = GetComponentInParent<PlayerCollision>();
    }

    private void OnTriggerEnter(Collider other)
    {
        playerCollision.HandleHit(other);
    }
}