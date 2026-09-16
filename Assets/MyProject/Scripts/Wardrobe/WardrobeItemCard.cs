using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeItemCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Button equipButton;
    [SerializeField] private WardrobeItemPreview preview;
    [SerializeField] private Transform previewRoot;

    private WardrobeManager manager;
    private WardrobeItem item;
    private WardrobeSkin skin;
    private WardrobeCategory category;

    public void Initialize(
        WardrobeManager wardrobeManager,
        WardrobeItem wardrobeItem,
        WardrobeSkin wardrobeSkin,
        GameObject capybaraPreviewPrefab)
    {
        manager = wardrobeManager;
        item = wardrobeItem;
        skin = wardrobeSkin;

        if (manager == null || (item == null && skin == null))
        {
            Debug.LogWarning($"[{nameof(WardrobeItemCard)}] Card data is incomplete on {name}.");
            return;
        }

        category = wardrobeItem != null
            ? wardrobeItem.Category
            : WardrobeCategory.Cores;

        if (nameText != null)
            nameText.text = wardrobeItem != null ? wardrobeItem.ItemName : wardrobeSkin.SkinName;

        if (rarityText != null)
            rarityText.text = wardrobeItem != null ? wardrobeItem.Rarity : wardrobeSkin.Rarity;

        if (preview != null)
        {
            if (wardrobeItem != null)
                preview.ShowItem(wardrobeItem, previewRoot);
            else
                preview.ShowSkin(wardrobeSkin, capybaraPreviewPrefab, previewRoot);
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

    private WardrobeItem GetEquippedItem()
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

    private void OnDestroy()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(HandleEquipClicked);
    }
}
