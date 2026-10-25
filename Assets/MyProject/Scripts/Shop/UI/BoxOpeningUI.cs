using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoxOpeningUI : MonoBehaviour
{
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform boxVisual;
    [SerializeField] private Image boxBackground;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Button closeButton;
    [SerializeField] private WardrobeItemCard rewardItemPrefab;
    [SerializeField] private Transform rewardItemRoot;
    [SerializeField] private float duration = 5f;

    private Coroutine openingRoutine;
    private Preview3D itemPreview;
    private GameObject displayPanel;
    private WardrobeItemCard rewardItemCard;
    private System.Action closedCallback;

    private void Awake()
    {
        displayPanel = FindDisplayPanel();
        itemPreview = GetComponentInChildren<Preview3D>(true);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (displayPanel != null && displayPanel != gameObject) displayPanel.SetActive(false);
        else if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void Play(WardrobeBoxReward reward, System.Action onClosed = null)
    {
        if (openingRoutine != null)
            StopCoroutine(openingRoutine);

        closedCallback = onClosed;
        gameObject.SetActive(true);
        if (displayPanel != null) displayPanel.SetActive(true);
        openingRoutine = StartCoroutine(PlayRoutine(reward));
    }

    public void ConfigureRewardPresentation(WardrobeItemCard itemPrefab, Transform itemRoot)
    {
        rewardItemPrefab = itemPrefab;
        rewardItemRoot = itemRoot;
    }

    private IEnumerator PlayRoutine(WardrobeBoxReward reward)
    {
        Color rarityColor = raritySettings != null
            ? raritySettings.GetColor(reward.Rarity)
            : Color.white;

        if (boxBackground != null) boxBackground.color = Color.white;
        if (rarityText != null) rarityText.text = string.Empty;
        if (itemNameText != null) itemNameText.text = string.Empty;
        ClearRewardCard();
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (boxVisual != null)
            boxVisual.localScale = Vector3.one * 0.2f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // Deterministic reveal only: reward was already confirmed by CloudScript.
            if (boxBackground != null)
            {
                float interval = Mathf.Lerp(0.08f, 0.75f, progress * progress);
                int phase = Mathf.FloorToInt(elapsed / interval);
                boxBackground.color = progress > 0.92f ? rarityColor : CycleColor(phase);
            }
            if (boxVisual != null)
                boxVisual.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, progress);
            yield return null;
        }

        RevealReward(reward);
        openingRoutine = null;
    }

    private Color CycleColor(int phase)
    {
        if (raritySettings == null || raritySettings.Values == null || raritySettings.Values.Length == 0) return Color.white;
        return raritySettings.Values[Mathf.Abs(phase) % raritySettings.Values.Length].color;
    }

    private void RevealReward(WardrobeBoxReward reward)
    {
        // The item card already shows the skin name, rarity and colour. Keep this
        // label exclusively for the duplicate-point conversion.
        if (rarityText != null) rarityText.text = string.Empty;
        if (itemNameText != null)
            itemNameText.text = reward.IsDuplicate
                ? $"Duplicata: +{reward.DuplicatePoints} pontos"
                : string.Empty;

        if (rewardItemPrefab != null && rewardItemRoot != null && reward.Skin != null)
        {
            rewardItemCard = Instantiate(rewardItemPrefab, rewardItemRoot);
            if (rewardItemCard.transform is RectTransform cardRect)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
            }
            rewardItemCard.InitializePreview(reward.Skin);
            return;
        }
        ShowRewardPreview(reward.Skin);
    }

    private void ShowRewardPreview(WardrobeSkin skin)
    {
        if (itemPreview == null)
            return;

        if (skin == null)
        {
            itemPreview.Clear();
            return;
        }

        GameObject instance = WardrobePreviewUtility.GetCapybaraPreviewPrefab();
            if (instance == null)
            {
                itemPreview.Clear();
                return;
            }

            itemPreview.Show(instance, previewObject =>
            {
                if (previewObject == null)
                    return;

                PlayerCustomizationApplier applier = previewObject.GetComponent<PlayerCustomizationApplier>();
                if (applier != null) applier.ApplySkinOnly(skin);
                else Debug.LogWarning("Preview da capivara não possui PlayerCustomizationApplier.");
            }, skin.SkinId, skin.Rarity);
    }

    private void Close()
    {
        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
            openingRoutine = null;
        }

        if (itemPreview != null)
            itemPreview.Clear();
        ClearRewardCard();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (displayPanel != null && displayPanel != gameObject) displayPanel.SetActive(false);
        else gameObject.SetActive(false);
        System.Action callback = closedCallback;
        closedCallback = null;
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private GameObject FindDisplayPanel()
    {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            if (child != transform && child.name == "Panel") return child.gameObject;
        return gameObject;
    }

    private void ClearRewardCard()
    {
        if (rewardItemCard != null) Destroy(rewardItemCard.gameObject);
        rewardItemCard = null;
    }
}
