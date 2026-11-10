using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ImpulseIndicator impulseIndicator;
    [SerializeField] public Animator animator;
    [SerializeField] private Transform visualRoot;

    [SerializeField]
    private Rigidbody rb;
    [SerializeField] private PlayerConfig playerConfig;

    private const float ChargeStep = 0.5f;
    private float currentCharge;
    private bool hasLaunched;
    private Vector3 aimDirection;
    private Coroutine revealRotation;
    private Coroutine visualRevealRotation;
    private CapybaraAppearanceApplier appearanceApplier;
    private Renderer skinRenderer;
    private Quaternion visualRootBaseLocalRotation;
    private bool hasVisualPreview;

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

        appearanceApplier = GetComponentInChildren<CapybaraAppearanceApplier>(true);
        skinRenderer = GetComponentInChildren<Renderer>(true);
        if (visualRoot == null && skinRenderer != null)
        {
            visualRoot = skinRenderer.transform;
            while (visualRoot.parent != null && visualRoot.parent != transform)
                visualRoot = visualRoot.parent;
        }
        if (visualRoot != null)
        {
            visualRootBaseLocalRotation = visualRoot.localRotation;

            // NetworkPlayer stays still until reveal, but the capybara and
            // arrow must preview as one local presentation hierarchy.
            if (impulseIndicator != null && impulseIndicator.transform.parent != visualRoot)
            {
                impulseIndicator.transform.SetParent(visualRoot, true);
                impulseIndicator.RebasePresentationPose();
            }
        }
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

        // The shared visual root owns the private choosing presentation. It
        // intentionally does not rotate the NetworkPlayer before reveal.
        aimDirection = direction;
        impulseIndicator?.SetAimDirection(aimDirection);
    }

    public Vector3 GetAimDirection()
    {
        return aimDirection.sqrMagnitude > 0.001f ? aimDirection : transform.forward;
    }

    /// <summary>
    /// Presents the local selection through the common visual root. Since the
    /// indicator is a child of that root, it stays attached to the capybara
    /// without leaking the selected direction through NetworkTransform.
    /// </summary>
    public void PreviewLocalAimRotation()
    {
        if (hasLaunched || visualRoot == null) return;
        Vector3 direction = GetAimDirection();
        if (direction.sqrMagnitude <= 0.001f) return;
        visualRoot.localRotation = GetVisualAimRotation(direction);
        hasVisualPreview = true;
    }

    public void ClearLocalAimPreview()
    {
        if (visualRevealRotation != null)
        {
            StopCoroutine(visualRevealRotation);
            visualRevealRotation = null;
        }

        if (!hasVisualPreview || visualRoot == null) return;
        visualRoot.localRotation = visualRootBaseLocalRotation;
        hasVisualPreview = false;
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
        ClearLocalAimPreview();
        StopMovementAnimation();

        currentCharge = 0f;
        impulseIndicator?.ClearAimDirection();
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
            RefreshIndicatorTint();
            impulseIndicator.ShowArrow();
        }
    }

    /// <summary>Refreshes the visual indicator after a cosmetic loadout changes.</summary>
    public void RefreshIndicatorTint()
    {
        impulseIndicator?.SetTint(GetSkinColor());
    }

    private Color GetSkinColor()
    {
        if (appearanceApplier == null)
            appearanceApplier = GetComponentInChildren<CapybaraAppearanceApplier>(true);
        if (appearanceApplier != null)
            return appearanceApplier.GetAppliedColor(CapybaraColorSlot.Body);

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

    /// <summary>
    /// Starts the public network reveal on the local presentation hierarchy.
    /// The NetworkPlayer root deliberately remains still: its CameraTarget is
    /// a direct child and rotating that root makes Cinemachine feel locked for
    /// the local owner. AimDirection is already replicated, so every peer can
    /// show the same visual rotation without moving the camera target.
    /// </summary>
    public void BeginRevealPresentation(float duration, bool _)
    {
        if (visualRoot == null) return;
        if (visualRevealRotation != null) StopCoroutine(visualRevealRotation);
        visualRevealRotation = StartCoroutine(RotateVisualToAimDirection(duration));
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

    private System.Collections.IEnumerator RotateVisualToAimDirection(float duration)
    {
        Vector3 direction = GetAimDirection();
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            visualRevealRotation = null;
            yield break;
        }

        Quaternion startRotation = visualRoot.localRotation;
        Quaternion targetRotation = GetVisualAimRotation(direction);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        hasVisualPreview = true;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            visualRoot.localRotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        visualRoot.localRotation = targetRotation;
        visualRevealRotation = null;
    }

    private Quaternion GetVisualAimRotation(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return visualRootBaseLocalRotation;

        // The imported capybara mesh has its own model-orientation offset.
        // Applying a yaw on top of its authored local rotation preserves that
        // offset while visibly facing the selected world direction.
        float yaw = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);
        return Quaternion.AngleAxis(yaw, Vector3.up) * visualRootBaseLocalRotation;
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
