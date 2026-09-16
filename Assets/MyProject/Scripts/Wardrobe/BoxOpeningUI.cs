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
    [SerializeField] private WardrobeItemPreview itemPreview;
    [SerializeField] private Transform itemPreviewRoot;
    [SerializeField] private GameObject capybaraPreviewPrefab;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Button closeButton;
    [SerializeField] private float duration = 2.5f;

    private Coroutine openingRoutine;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        Close();
    }

    public void Play(WardrobeBoxReward reward)
    {
        if (openingRoutine != null)
            StopCoroutine(openingRoutine);

        gameObject.SetActive(true);
        openingRoutine = StartCoroutine(PlayRoutine(reward));
    }

    private IEnumerator PlayRoutine(WardrobeBoxReward reward)
    {
        Color rarityColor = raritySettings != null
            ? raritySettings.GetColor(reward.Rarity)
            : Color.white;

        if (boxBackground != null)
            boxBackground.color = rarityColor;
        if (rarityText != null)
            rarityText.text = reward.Rarity.ToString();
        if (itemNameText != null)
            itemNameText.text = reward.Item != null ? reward.Item.ContentName : string.Empty;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (boxVisual != null)
            boxVisual.localScale = Vector3.one * 0.2f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (boxVisual != null)
                boxVisual.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, progress);
            yield return null;
        }

        ShowRewardPreview(reward.Item);
        openingRoutine = null;
    }

    private void ShowRewardPreview(WardrobeContent item)
    {
        if (itemPreview == null || item == null)
            return;

        if (item is WardrobeSkin skin)
            itemPreview.ShowSkin(skin, capybaraPreviewPrefab, itemPreviewRoot);
        else if (item is WardrobeEquipment equipment)
            itemPreview.ShowContent(equipment, itemPreviewRoot);
    }

    private void Close()
    {
        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
            openingRoutine = null;
        }

        if (itemPreview != null)
            itemPreview.ClearPreview();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }
}
