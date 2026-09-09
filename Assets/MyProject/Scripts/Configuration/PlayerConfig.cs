using UnityEngine;

/// <summary>
/// Global configuration for all players and bots.
/// Centralized point for gameplay balance parameters.
/// </summary>
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Rivals Arena/Player Config")]
public class PlayerConfig : ScriptableObject
{
    #region Charge

    [Header("Charge")]
    [SerializeField, Tooltip("Minimum charge available to every player.")] [Range(0f, 10f)] private float minCharge = 0f;
    [SerializeField, Tooltip("Maximum charge available to every player.")] [Range(1f, 10f)] private float maxCharge = 10f;

    public float MinCharge => minCharge;
    public float MaxCharge => maxCharge;

    #endregion

    #region Impulse

    [Header("Impulse")]
    [SerializeField, Tooltip("Impulse applied per charge point.")] [Range(0.1f, 10f)] private float impulseForce = 2.5f;

    public float ImpulseForce => impulseForce;

    #endregion

    #region Movement

    [Header("Movement")]
    [SerializeField, Tooltip("How quickly launched players face their velocity.")] [Range(0.1f, 50f)] private float rotationSpeed = 10f;

    public float RotationSpeed => rotationSpeed;

    #endregion

    #region Collision

    [Header("Collision")]
    [SerializeField, Tooltip("Minimum relative speed that produces a player impact.")] [Range(0.1f, 5f)] private float minimumImpactVelocity = 2f;
    [SerializeField, Tooltip("Minimum time between processed impacts for a player.")] [Range(0.01f, 0.5f)] private float collisionCooldown = 0.1f;
    [SerializeField, Tooltip("Multiplier for the collision response received from the opponent.")] [Range(0.1f, 3f)] private float knockbackMultiplier = 1f;
    [SerializeField, Tooltip("Multiplier kept for balancing recoil response.")] [Range(0.1f, 3f)] private float recoilMultiplier = 1f;

    public float MinimumImpactVelocity => minimumImpactVelocity;
    public float CollisionCooldown => collisionCooldown;
    public float KnockbackMultiplier => knockbackMultiplier;
    public float RecoilMultiplier => recoilMultiplier;

    #endregion
}
