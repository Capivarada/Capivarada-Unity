using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private float lastCollisionTime = -1f;
    
    private static readonly HashSet<ulong> processedCollisions = new();

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    public void HandleHit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Rigidbody otherRb =
            other.GetComponentInParent<Rigidbody>();

        if (otherRb == null || otherRb == rb) 
            return;

        if (gameObject.GetEntityId() > otherRb.gameObject.GetEntityId())
            return;

        ProcessImpact(otherRb);
    }

    private void ProcessImpact(Rigidbody otherRb)
    {
        PlayerConfig config = GameManager.Instance.PlayerConfig;
        PlayerController player = gameObject.GetComponent<PlayerController>();
        
        player.animator.SetTrigger("Attack");
        
        if (config == null)
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
        float knockbackA =
            impact * forceB * config.KnockbackMultiplier;

        float knockbackB =
            impact * forceA * config.KnockbackMultiplier;

        ApplyResponse(
            rb,
            -direction,
            knockbackA
        );

        ApplyResponse(
            otherRb,
            direction,
            knockbackB
        );
        
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
        // Remove a velocidade que está entrando
        // na colisão.
        Vector3 velocity =
            target.linearVelocity;

        velocity -=
            Vector3.Project(
                velocity,
                direction
            );

        target.linearVelocity = velocity;

        // Aplica a nova velocidade de saída.
        target.AddForce(
            direction * force,
            ForceMode.VelocityChange
        );
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
}
