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
    [SerializeField] private Preview3D preview3D;

    private WardrobeManager manager;
    private WardrobeEquipment item;
    private WardrobeSkin skin;
    private WardrobeCategory category;
    private bool isNoneOption;

    public void Initialize(
        WardrobeManager wardrobeManager,
        WardrobeContent wardrobeContent,
        GameObject capybaraPreviewPrefab)
    {
        manager = wardrobeManager;
        skin = wardrobeContent as WardrobeSkin;
        item = wardrobeContent as WardrobeEquipment;
        category = item != null ? GetCategory(item) : WardrobeCategory.Cores;
        isNoneOption = false;

        if (manager == null || wardrobeContent == null || (item == null && skin == null))
        {
            Debug.LogWarning($"[{nameof(WardrobeItemCard)}] Card data is incomplete on {name}.");
            return;
        }

        if (nameText != null)
            nameText.text = wardrobeContent.ContentName;

        if (rarityText != null)
        {
            rarityText.text = wardrobeContent.ContentRarity.ToString();
            if (raritySettings != null)
                rarityText.color = raritySettings.GetColor(wardrobeContent.ContentRarity);
        }

        ShowPreview(capybaraPreviewPrefab);

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(HandleEquipClicked);
            equipButton.onClick.AddListener(HandleEquipClicked);
        }

        RefreshState();
    }

    public void InitializeNone(WardrobeManager wardrobeManager, WardrobeCategory targetCategory, GameObject capybaraPreviewPrefab)
    {
        manager = wardrobeManager;
        skin = null;
        item = null;
        category = targetCategory;
        isNoneOption = true;

        if (nameText != null)
            nameText.text = "NENHUM";

        if (rarityText != null)
            rarityText.text = string.Empty;

        ShowPreview(capybaraPreviewPrefab);

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

        bool equipped = isNoneOption ? IsNoneEquipped() : (skin != null ? manager.CurrentCustomization.Skin == skin : GetEquippedItem() == item);
        buttonText.text = equipped ? "EQUIPADO" : "EQUIPAR";
    }

    private void HandleEquipClicked()
    {
        if (manager == null)
            return;

        if (isNoneOption)
        {
            if (category == WardrobeCategory.Cores)
                manager.EquipDefaultSkin();
            else
                manager.Unequip(category);
            return;
        }

        if (skin != null)
            manager.EquipSkin(skin);
        else
            manager.EquipItem(item);
    }

    private void ShowPreview(GameObject capybaraPreviewPrefab)
    {
        if (preview3D == null)
        {
            Debug.LogWarning($"[{nameof(WardrobeItemCard)}] Preview3D is not assigned on {name}.");
            return;
        }

        if (isNoneOption)
        {
            preview3D.Clear();
            return;
        }

        if (skin != null)
        {
            GameObject instance = capybaraPreviewPrefab != null ? capybaraPreviewPrefab : WardrobePreviewUtility.GetCapybaraPreviewPrefab();
            if (instance == null)
            {
                preview3D.Clear();
                return;
            }

            preview3D.Show(instance, previewObject =>
            {
                if (previewObject == null)
                    return;

                PlayerCustomizationApplier applier = previewObject.GetComponent<PlayerCustomizationApplier>();
                if (applier == null)
                    applier = previewObject.AddComponent<PlayerCustomizationApplier>();

                applier.Apply(new PlayerCustomization { Skin = skin });
            });
            return;
        }

        if (item != null)
        {
            GameObject previewPrefab = WardrobePreviewUtility.GetContentPreviewPrefab(item);
            if (previewPrefab == null)
            {
                preview3D.Clear();
                return;
            }

            preview3D.Show(previewPrefab);
            return;
        }

        preview3D.Clear();
    }

    private bool IsNoneEquipped()
    {
        if (manager == null)
            return false;

        PlayerCustomization customization = manager.CurrentCustomization;
        if (customization == null)
            return false;

        switch (category)
        {
            case WardrobeCategory.Cores:
                return manager.IsDefaultSkin(customization.Skin);
            case WardrobeCategory.Cabeca:
                return customization.Head == null;
            case WardrobeCategory.Acessorios:
                return customization.Accessory == null;
            case WardrobeCategory.Roupas:
                return customization.Clothing == null;
            default:
                return false;
        }
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
