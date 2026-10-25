using UnityEngine;

public class ImpulseIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cubePrefab;

    [Header("Indicator")]
    private GameObject arrow;
    private Vector3 originalIndicatorScale;
    private Vector3 originalIndicatorPosition;
    private const float ForwardAdvancePerForce = 0.2f;

    private float currentCharge;

    public float CurrentCharge => currentCharge;

    private void Awake()
    {
        originalIndicatorScale = transform.localScale;
        originalIndicatorPosition = transform.localPosition;
        HideArrow();
    }

    public void SetCharge(float charge)
    {
        currentCharge = Mathf.Clamp(charge, 0f, 5f);

        UpdateArrowScale();
        if (currentCharge == 0)
            HideArrow();
    }

    public void ShowArrow()
    {
        if (currentCharge <= 0 || cubePrefab == null) return;
        if (arrow == null)
        {
            arrow = Instantiate(cubePrefab, transform);
        }
        UpdateArrowScale();
        arrow.SetActive(true);
    }

    public void HideArrow()
    {
        if (arrow != null) arrow.SetActive(false);
    }

    private void UpdateArrowScale()
    {
        // Only the forward axis grows: force 1 = (1,1,1), force 2 =
        // (1,1,2), etc., while preserving prefab-authored X/Y scale.
        Vector3 scale = originalIndicatorScale;
        scale.z = originalIndicatorScale.z * currentCharge;
        transform.localScale = scale;

        // Keep the authored position (for example z = 1.35 at force 1) and
        // move forward by 0.2 for each subsequent force point.
        Vector3 position = originalIndicatorPosition;
        position.z += Mathf.Max(0f, currentCharge - 1f) * ForwardAdvancePerForce;
        transform.localPosition = position;
    }
}
