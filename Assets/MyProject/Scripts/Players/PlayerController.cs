using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ImpulseIndicator impulseIndicator;
    //[SerializeField] public Animator animator;

    [SerializeField]
    private Rigidbody rb;

    private int currentCharge;
    private bool hasLaunched;

    public event Action<int> OnChargeChanged;

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

    #endregion

    #region CHARGE

    public void IncreaseCharge()
    {
        if (hasLaunched)
            return;

        PlayerConfig config = GameManager.Instance.PlayerConfig;
        if (config == null)
            return;

        if (currentCharge >= config.MaxCharge)
            return;

        currentCharge++;

        UpdateIndicator();
    }

    public void DecreaseCharge()
    {
        if (hasLaunched)
            return;

        PlayerConfig config = GameManager.Instance.PlayerConfig;
        if (config == null)
            return;

        if (currentCharge <= config.MinCharge)
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

        PlayerConfig config = GameManager.Instance.PlayerConfig;
        if (config == null)
            return;

        currentCharge =
            Mathf.Clamp(
                charge,
                (int)config.MinCharge,
                (int)config.MaxCharge
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

        PlayerConfig config = GameManager.Instance.PlayerConfig;
        if (config == null)
            return;

        Vector3 direction =
            GetAimDirection();

        direction.y = 0f;

        direction.Normalize();

        float force =
            config.ImpulseForce *
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
        //animator.SetBool("Run", false);
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
        PlayerConfig config = GameManager.Instance.PlayerConfig;
        if (config == null)
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
}
