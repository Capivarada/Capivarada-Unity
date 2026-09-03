using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ImpulseIndicator impulseIndicator;

    [SerializeField]
    private Rigidbody rb;

    [Header("Charge")]
    [SerializeField]
    private int minCharge = 0;

    [SerializeField]
    private int maxCharge = 10;

    [Header("Impulse")]
    [SerializeField]
    private float impulseForce = 2.5f;

    [Header("Rotation")]
    [SerializeField]
    private float rotationSpeed = 10f;

    private bool hasLaunched;

    private int currentCharge;

    public bool HasLaunched =>
        hasLaunched;

    public int CurrentCharge =>
        currentCharge;

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
    }

    private void Start()
    {
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

        impulseIndicator.transform.rotation =
            Quaternion.LookRotation(
                direction
            );
    }

    public Vector3 GetAimDirection()
    {
        if (impulseIndicator == null)
        {
            return transform.forward;
        }

        return
            impulseIndicator
                .transform
                .forward;
    }
    
    public void SetIndicatorVisible(bool visible)
    {
        if (impulseIndicator == null)
            return;

        impulseIndicator.gameObject.SetActive(
            visible
        );
    }

    #endregion

    #region CHARGE

    public void IncreaseCharge()
    {
        if (hasLaunched)
            return;

        if (currentCharge >= maxCharge)
            return;

        currentCharge++;

        UpdateIndicator();
    }

    public void DecreaseCharge()
    {
        if (hasLaunched)
            return;

        if (currentCharge <= minCharge)
            return;

        currentCharge--;

        UpdateIndicator();
    }

    public void SetCharge(
        int charge
    )
    {
        if (hasLaunched)
            return;

        currentCharge =
            Mathf.Clamp(
                charge,
                minCharge,
                maxCharge
            );

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

        hasLaunched = true;

        Vector3 direction =
            GetAimDirection();

        direction.y = 0f;

        direction.Normalize();

        float force =
            impulseForce *
            currentCharge;

        rb.AddForce(
            direction * force,
            ForceMode.Impulse
        );
    }

    #endregion

    #region ROUND

    public void ResetRound()
    {
        hasLaunched = false;

        currentCharge = 0;

        ShowIndicator();

        UpdateIndicator();
    }

    public void HideIndicator()
    {
        if (impulseIndicator != null)
        {
            impulseIndicator
                .gameObject
                .SetActive(false);
        }
    }

    public void ShowIndicator()
    {
        if (impulseIndicator != null)
        {
            impulseIndicator
                .gameObject
                .SetActive(true);
        }
    }

    #endregion

    #region ROTATION

    private void HandleRotation()
    {
        if (!hasLaunched)
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
                -rotationSpeed *
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
    }
}