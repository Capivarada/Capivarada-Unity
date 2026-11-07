using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ImpulseIndicator impulseIndicator;
    [SerializeField] public Animator animator;

    [SerializeField]
    private Rigidbody rb;
    [SerializeField] private PlayerConfig playerConfig;

    private const float ChargeStep = 0.5f;
    private float currentCharge;
    private bool hasLaunched;
    private Vector3 aimDirection;
    private Coroutine revealRotation;
    private PlayerCustomizationApplier customizationApplier;
    private Renderer skinRenderer;

    public event Action<float> OnChargeChanged;

    public bool IsRevealingDirection => revealRotation != null;

    public bool HasLaunched =>
        hasLaunched;

    public float CurrentCharge =>
        currentCharge;

    public PlayerConfig Configuration => ResolveConfiguration();

    public void SetConfiguration(PlayerConfig configuration)
    {
        if (configuration != null)
            playerConfig = configuration;
    }

    /// <summary>Applies replicated choice state without exposing mutable fields.</summary>
    public void ApplyNetworkSelection(Vector3 direction, float charge)
    {
        SetAimDirection(direction);
        SetCharge(charge);
    }

    /// <summary>Network bridge entry point used once per synchronized round.</summary>
    public void LaunchFromNetwork() => Launch();

    private void Awake()
    {
        if (impulseIndicator == null)
        {
            impulseIndicator =
                GetComponentInChildren<
                    ImpulseIndicator
                >();
        }

        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }

        customizationApplier = GetComponentInChildren<PlayerCustomizationApplier>(true);
        skinRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
    }

    private void Start()
    {
        aimDirection = transform.forward;
        UpdateIndicator();
    }

    private void Update()
    {
        HandleRotation();
    }

    #region AIM

    public void SetAimDirection(
        Vector3 direction
    )
    {
        if (hasLaunched)
            return;

        direction.y = 0f;

        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            return;
        }

        direction.Normalize();

        // Keep the arrow in front of the capybara while choosing. The player
        // rotates only during the "Direções Reveladas" phase.
        aimDirection = direction;
    }

    public Vector3 GetAimDirection()
    {
        return aimDirection.sqrMagnitude > 0.001f ? aimDirection : transform.forward;
    }

    #endregion

    #region CHARGE

    public void IncreaseCharge()
    {
        if (hasLaunched)
            return;

        PlayerConfig config = ResolveConfiguration();
        if (config == null)
            return;

        if (currentCharge >= config.MaxCharge)
            return;

        currentCharge = Mathf.Min(config.MaxCharge, currentCharge + ChargeStep);

        UpdateIndicator();
    }

    public void DecreaseCharge()
    {
        if (hasLaunched)
            return;

        PlayerConfig config = ResolveConfiguration();
        if (config == null)
            return;

        if (currentCharge <= config.MinCharge)
            return;

        currentCharge = Mathf.Max(config.MinCharge, currentCharge - ChargeStep);

        UpdateIndicator();
    }

    public void SetCharge(
        float charge
    )
    {
        if (hasLaunched)
            return;

        PlayerConfig config = ResolveConfiguration();
        if (config == null)
            return;

        currentCharge = Mathf.Clamp(charge, config.MinCharge, config.MaxCharge);
        currentCharge = Mathf.Round(currentCharge / ChargeStep) * ChargeStep;

        UpdateIndicator();
    }

    #endregion

    #region IMPULSE

    public void Launch()
    {
        if (hasLaunched)
            return;

        if (currentCharge <= 0)
            return;

        SetMovementAnimation(currentCharge);
            
            
        hasLaunched = true;

        PlayerConfig config = ResolveConfiguration();
        if (config == null)
            return;

        Vector3 direction =
            GetAimDirection();

        direction.y = 0f;

        direction.Normalize();

        float force =
            config.ImpulseForce * 
            currentCharge;

        if (rb == null)
            return;

        rb.AddForce(
            direction * force,
            ForceMode.Impulse
        );
        
       
    }

    #endregion

    #region ROUND

    public void ResetRound()
    {
        if (revealRotation != null) StopCoroutine(revealRotation);
        revealRotation = null;
        hasLaunched = false;
        StopMovementAnimation();

        currentCharge = 0f;
        HideIndicator();

        UpdateIndicator();
    }

    /// <summary>Stops movement states whenever the capybara is not moving.</summary>
    public void StopMovementAnimation()
    {
        if (animator == null) return;
        animator.SetBool("Walk", false);
        animator.SetBool("Run", false);
        animator.SetBool("Trot", false);
    }

    private void SetMovementAnimation(float charge)
    {
        StopMovementAnimation();
        if (animator == null) return;

        if (charge <= 2f) animator.SetBool("Walk", true);
        else if (charge <= 4f) animator.SetBool("Run", true);
        else animator.SetBool("Trot", true);
    }

    public void HideIndicator()
    {
        if (impulseIndicator != null)
        {
            impulseIndicator.HideArrow();
        }
    }

    public void ShowIndicator()
    {
        if (impulseIndicator != null)
        {
            impulseIndicator.SetTint(GetSkinColor());
            impulseIndicator.ShowArrow();
        }
    }

    private Color GetSkinColor()
    {
        if (customizationApplier == null)
            customizationApplier = GetComponentInChildren<PlayerCustomizationApplier>(true);
        WardrobeSkin skin = customizationApplier != null ? customizationApplier.AppliedSkin : null;
        SkinColorData skinColor = skin != null ? skin.GetColorData(SkinColorSlot.Skin) : null;
        if (skinColor != null) return skinColor.color;

        if (skinRenderer != null && skinRenderer.sharedMaterials.Length > 0)
        {
            Material material = skinRenderer.sharedMaterials[0];
            if (material != null && material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material != null && material.HasProperty("_Color")) return material.GetColor("_Color");
        }

        return Color.white;
    }

    public void BeginRevealRotation(float duration)
    {
        if (revealRotation != null) StopCoroutine(revealRotation);
        revealRotation = StartCoroutine(RotateToAimDirection(duration));
    }

    private System.Collections.IEnumerator RotateToAimDirection(float duration)
    {
        Vector3 direction = GetAimDirection();
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            revealRotation = null;
            yield break;
        }

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        transform.rotation = targetRotation;
        revealRotation = null;
    }

    #endregion

    #region ROTATION

    private void HandleRotation()
    {
        if (!hasLaunched)
            return;
        PlayerConfig config = ResolveConfiguration();
        if (config == null)
            return;

        if (rb == null)
            return;

        Vector3 velocity =
            rb.linearVelocity;

        velocity.y = 0f;

        if (
            velocity.sqrMagnitude <=
            0.01f
        )
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                velocity.normalized
            );

        float rotationAmount =
            1f -
            Mathf.Exp(
                -config.RotationSpeed *
                Time.deltaTime
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationAmount
            );
    }

    #endregion

    private void UpdateIndicator()
    {
        if (impulseIndicator == null)
            return;

        impulseIndicator.SetCharge(
            currentCharge
        );

        OnChargeChanged?.Invoke(currentCharge);
    }

    private PlayerConfig ResolveConfiguration()
    {
        if (playerConfig != null)
            return playerConfig;

        FusionArenaMatchController fusionMatch = FindFirstObjectByType<FusionArenaMatchController>();
        if (fusionMatch != null && fusionMatch.PlayerConfig != null)
        {
            playerConfig = fusionMatch.PlayerConfig;
            return playerConfig;
        }

        if (GameManager.Instance != null && GameManager.Instance.PlayerConfig != null)
        {
            playerConfig = GameManager.Instance.PlayerConfig;
            return playerConfig;
        }

        // Keeps network prototypes playable before the scene balance asset is
        // assigned. The authored PlayerConfig still overrides this fallback.
        playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
        Debug.LogWarning("[PlayerController] PlayerConfig não configurado; usando os valores padrão temporários.", this);
        return playerConfig;
    }
}
