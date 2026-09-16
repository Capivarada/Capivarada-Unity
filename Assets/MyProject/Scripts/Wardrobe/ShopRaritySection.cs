using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopRaritySection : MonoBehaviour
{
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text chanceText;
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject itemsRoot;
    [SerializeField] private TMP_Text itemTextPrefab;

    private RarityDrop drop;
    private bool expanded;

    public void Initialize(RarityDrop rarityDrop)
    {
        drop = rarityDrop;
        expanded = false;

        if (rarityText != null)
        {
            rarityText.text = drop != null ? drop.rarity.ToString() : string.Empty;
            if (drop != null && raritySettings != null)
                rarityText.color = raritySettings.GetColor(drop.rarity);
        }
        if (chanceText != null)
            chanceText.text = drop != null ? $"{drop.chance:0.##}%" : string.Empty;

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(Toggle);
            toggleButton.onClick.AddListener(Toggle);
        }

        RebuildItems();
        SetExpanded(false);
    }

    private void Toggle()
    {
        SetExpanded(!expanded);
    }

    private void SetExpanded(bool value)
    {
        expanded = value;
        if (itemsRoot != null)
            itemsRoot.SetActive(expanded);
    }

    private void RebuildItems()
    {
        if (itemsRoot == null || itemTextPrefab == null || drop == null)
            return;

        for (int i = itemsRoot.transform.childCount - 1; i >= 0; i--)
            Destroy(itemsRoot.transform.GetChild(i).gameObject);

        foreach (WardrobeContent item in drop.items)
        {
            if (item == null)
                continue;

            TMP_Text text = Instantiate(itemTextPrefab, itemsRoot.transform);
            text.text = item.ContentName;
        }
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(Toggle);
    }
}
