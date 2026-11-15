using Fusion;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private float lastCollisionTime = -1f;
    
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    public void HandleHit(Collider other)
    {
        Rigidbody otherRb = other != null ? other.attachedRigidbody : null;
        if (otherRb == null && other != null) otherRb = other.GetComponentInParent<Rigidbody>();

        if (otherRb == null || otherRb == rb || otherRb.GetComponent<PlayerController>() == null)
            return;

        if (!AreFusionParticipants(otherRb) && gameObject.GetEntityId() > otherRb.gameObject.GetEntityId())
            return;

        ProcessImpact(otherRb);
    }

    // The solid body collision is the reliable fallback for high-speed hits;
    // the trigger hitbox can be skipped by a discrete physics step.
    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null) HandleHit(collision.collider);
    }

    private void ProcessImpact(Rigidbody otherRb)
    {
        PlayerController player = GetComponent<PlayerController>();
        PlayerConfig config = player != null ? player.Configuration : null;
        if (player != null && player.animator != null) player.animator.SetTrigger("Attack");
        
        if (config == null || rb == null || otherRb == null)
            return;

        bool fusionCollision = AreFusionParticipants(otherRb);
        if (fusionCollision && !HasFusionCollisionAuthority(otherRb))
            return;

        if (Time.time - lastCollisionTime < config.CollisionCooldown)
            return;
        
        PlayerDeath deathA =
            rb.GetComponent<PlayerDeath>();

        PlayerDeath deathB =
            otherRb.GetComponent<PlayerDeath>();
        
        lastCollisionTime = Time.time;
        
        Vector3 direction =
            otherRb.position - rb.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        Vector3 velocityA = rb.linearVelocity;
        Vector3 velocityB = otherRb.linearVelocity;

        velocityA.y = 0f;
        velocityB.y = 0f;

        // Velocidade de cada player na direção do outro.
        float speedA =
            Vector3.Dot(velocityA, direction);

        float speedB =
            Vector3.Dot(velocityB, direction);

        
        RegisterAttacker(
            otherRb.gameObject,
            speedA,
            speedB
        );
        RegisterFusionAttacker(otherRb, speedA, speedB);
        // Velocidade relativa.
        float relativeSpeed =
            speedA - speedB;

        if (relativeSpeed < config.MinimumImpactVelocity)
            return;

        float impact =
            Mathf.Abs(relativeSpeed);

        // Quanto cada player contribuiu para o impacto.
        float contributionA =
            Mathf.Max(0f, speedA);

        float contributionB =
            Mathf.Max(0f, -speedB);

        float totalContribution =
            contributionA + contributionB;

        if (totalContribution <= 0f)
            return;

        // Proporção da força de cada jogador.
        float forceA =
            contributionA / totalContribution;

        float forceB =
            contributionB / totalContribution;

        // Cada um recebe uma resposta proporcional
        // à contribuição do outro.
        float response = Mathf.Min(
            impact * config.KnockbackMultiplier,
            config.MaxKnockbackVelocity);
        float knockbackA = response * forceB;
        float knockbackB = response * forceA;

        if (fusionCollision)
        {
            DispatchFusionResponse(rb, -direction, knockbackA);
            DispatchFusionResponse(otherRb, direction, knockbackB);
        }
        else
        {
            ApplyResponse(rb, -direction, knockbackA);
            ApplyResponse(otherRb, direction, knockbackB);
        }

        Debug.Log(
            $"IMPACTO\n" +
            $"A: {speedA:F2}\n" +
            $"B: {speedB:F2}\n" +
            $"Impacto: {impact:F2}\n" +
            $"Knockback A: {knockbackA:F2}\n" +
            $"Knockback B: {knockbackB:F2}"
        );
    }

    private void ApplyResponse(
        Rigidbody target,
        Vector3 direction,
        float force)
    {
        Vector3 velocity =
            target.linearVelocity;

        // Remove only velocity aimed against the push. Keeping velocity that
        // already points outward prevents players from feeling like bricks.
        float alongPush = Vector3.Dot(new Vector3(velocity.x, 0f, velocity.z), direction);
        if (alongPush < 0f)
            velocity -= direction * alongPush;

        target.linearVelocity = velocity;

        // Aplica a nova velocidade de saída.
        target.AddForce(
            direction * force,
            ForceMode.VelocityChange
        );
    }

    private bool AreFusionParticipants(Rigidbody other)
    {
        return GetComponent<NetworkObject>() != null && other.GetComponent<NetworkObject>() != null &&
               (GetComponent<FusionNetworkPlayer>() != null || GetComponent<FusionNetworkBot>() != null) &&
               (other.GetComponent<FusionNetworkPlayer>() != null || other.GetComponent<FusionNetworkBot>() != null);
    }

    private bool HasFusionCollisionAuthority(Rigidbody other)
    {
        NetworkObject selfObject = GetComponent<NetworkObject>();
        NetworkObject otherObject = other.GetComponent<NetworkObject>();
        return selfObject != null && otherObject != null && selfObject.HasStateAuthority && selfObject.Id.Raw < otherObject.Id.Raw;
    }

    private static void DispatchFusionResponse(Rigidbody target, Vector3 direction, float force)
    {
        if (force <= 0f) return;
        FusionNetworkPlayer player = target.GetComponent<FusionNetworkPlayer>();
        if (player != null)
        {
            player.RequestCollisionResponse(direction, force);
            return;
        }

        FusionNetworkBot bot = target.GetComponent<FusionNetworkBot>();
        if (bot != null) bot.RequestCollisionResponse(direction, force);
    }
    
    private void RegisterAttacker(
        GameObject otherPlayer,
        float speedA,
        float speedB
    )
    {
        // Eu estava indo em direção ao outro.
        if (speedA > 0f)
        {
            PlayerDeath otherDeath =
                otherPlayer.GetComponent<PlayerDeath>();

            if (otherDeath != null)
            {
                otherDeath.SetLastAttacker(
                    gameObject
                );
            }
        }

        // O outro estava indo em minha direção.
        if (speedB < 0f)
        {
            PlayerDeath myDeath =
                GetComponent<PlayerDeath>();

            if (myDeath != null)
            {
                myDeath.SetLastAttacker(
                    otherPlayer
                );
            }
        }
    }

    // Legacy PlayerDeath is disabled in Fusion. Store the attacker on the
    // authoritative NetworkObject so the server can award an elimination when
    // that target later crosses MatchConfig.DeathHeight.
    private void RegisterFusionAttacker(Rigidbody otherPlayer, float speedA, float speedB)
    {
        if (!AreFusionParticipants(otherPlayer)) return;
        NetworkObject self = GetComponent<NetworkObject>();
        NetworkObject other = otherPlayer.GetComponent<NetworkObject>();
        if (self == null || other == null) return;

        if (speedA > 0f) RegisterFusionAttackerOn(otherPlayer.gameObject, self);
        if (speedB < 0f) RegisterFusionAttackerOn(gameObject, other);
    }

    private static void RegisterFusionAttackerOn(GameObject target, NetworkObject attacker)
    {
        target.GetComponent<FusionNetworkPlayer>()?.RegisterAttacker(attacker);
        target.GetComponent<FusionNetworkBot>()?.RegisterAttacker(attacker);
    }
}
