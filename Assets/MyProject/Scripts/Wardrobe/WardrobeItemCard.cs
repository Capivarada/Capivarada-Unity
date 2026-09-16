using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeItemCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private Button equipButton;
    [SerializeField] private WardrobeItemPreview preview;
    [SerializeField] private Transform previewRoot;

    private WardrobeManager manager;
    private WardrobeEquipment item;
    private WardrobeSkin skin;
    private WardrobeCategory category;

    public void Initialize(
        WardrobeManager wardrobeManager,
        WardrobeContent wardrobeContent,
        GameObject capybaraPreviewPrefab)
    {
        manager = wardrobeManager;
        skin = wardrobeContent as WardrobeSkin;
        item = wardrobeContent as WardrobeEquipment;

        if (manager == null || wardrobeContent == null || (item == null && skin == null))
        {
            Debug.LogWarning($"[{nameof(WardrobeItemCard)}] Card data is incomplete on {name}.");
            return;
        }

        category = item != null
            ? GetCategory(item)
            : WardrobeCategory.Cores;

        if (nameText != null)
            nameText.text = wardrobeContent.ContentName;

        if (rarityText != null)
        {
            rarityText.text = wardrobeContent.ContentRarity.ToString();
            if (raritySettings != null)
                rarityText.color = raritySettings.GetColor(wardrobeContent.ContentRarity);
        }

        if (preview != null)
        {
            if (item != null)
                preview.ShowContent(item, previewRoot);
            else
                preview.ShowSkin(skin, capybaraPreviewPrefab, previewRoot);
        }

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(HandleEquipClicked);
            equipButton.onClick.AddListener(HandleEquipClicked);
        }

        RefreshState();
    }

    public void RefreshState()
    {
        if (buttonText == null || manager == null)
            return;

        bool equipped = skin != null
            ? manager.CurrentCustomization.Skin == skin
            : GetEquippedItem() == item;

        buttonText.text = equipped ? "EQUIPADO" : "EQUIPAR";
    }

    private void HandleEquipClicked()
    {
        if (manager == null)
            return;

        if (skin != null)
            manager.EquipSkin(skin);
        else
            manager.EquipItem(item);
    }

    private WardrobeEquipment GetEquippedItem()
    {
        PlayerCustomization customization = manager.CurrentCustomization;
        return category switch
        {
            WardrobeCategory.Cabeca => customization.Head,
            WardrobeCategory.Acessorios => customization.Accessory,
            WardrobeCategory.Roupas => customization.Clothing,
            _ => null
        };
    }

    private static WardrobeCategory GetCategory(WardrobeEquipment content)
    {
        return content switch
        {
            WardrobeHead => WardrobeCategory.Cabeca,
            WardrobeAccessory => WardrobeCategory.Acessorios,
            WardrobeCloth => WardrobeCategory.Roupas,
            _ => WardrobeCategory.Todos
        };
    }

    private void OnDestroy()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(HandleEquipClicked);
    }
}
