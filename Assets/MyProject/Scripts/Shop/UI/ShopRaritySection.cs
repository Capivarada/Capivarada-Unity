using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopRaritySection : MonoBehaviour
{
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image toggleImage;
    [SerializeField] private GameObject itemsRoot;
    [SerializeField] private WardrobeItemCard skinCardPrefab;
    private bool expanded;

    public void Initialize(Rarity rarity, float chance, SkinCatalog catalog)
    {
        if (rarityText != null) { rarityText.text = $"{rarity} - {chance:0.##}%"; if (raritySettings != null) rarityText.color = raritySettings.GetColor(rarity); }
        if (toggleImage != null && raritySettings != null)
            toggleImage.color = raritySettings.GetColor(rarity);
        if (toggleButton != null) { toggleButton.onClick.RemoveListener(Toggle); toggleButton.onClick.AddListener(Toggle); }
        if (itemsRoot != null && skinCardPrefab != null && catalog != null)
        {
            for (int i = itemsRoot.transform.childCount - 1; i >= 0; i--) Destroy(itemsRoot.transform.GetChild(i).gameObject);
            foreach (WardrobeSkin skin in catalog.GetEligibleForBox(rarity))
                if (skin != null) Instantiate(skinCardPrefab, itemsRoot.transform).InitializePreview(skin);
        }
        // The contents of a box must be immediately inspectable. The section
        // header still toggles this group afterwards if the player wants to fold it.
        SetExpanded(true);
    }
    private void Toggle() => SetExpanded(!expanded);

    private void SetExpanded(bool value)
    {
        expanded = value;
        if (itemsRoot != null)
            itemsRoot.SetActive(value);
        if (toggleImage != null)
            toggleImage.rectTransform.localEulerAngles = new Vector3( 0f, 0f, value ? 180f : -90f);
    }
    private void OnDestroy() { if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle); }
}
