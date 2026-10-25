using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable card for a single skin; it never changes the equipped skin directly.</summary>
public class WardrobeItemCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private Button equipButton;
    [SerializeField] private Image shapeItem;

    private Preview3D preview3D;
    private WardrobeManager manager;
    private WardrobeSkin skin;

    public void Initialize(WardrobeManager wardrobeManager, WardrobeSkin wardrobeSkin)
    {
        manager = wardrobeManager;
        skin = wardrobeSkin;
        if (manager == null || skin == null)
        {
            Debug.LogWarning($"[{nameof(WardrobeItemCard)}] Skin data is incomplete on {name}.");
            return;
        }

        if (nameText != null)
            nameText.text = skin.SkinName;

        if (rarityText != null)
        {
            rarityText.text = skin.Rarity.ToString();
            if (raritySettings != null)
                shapeItem.color = raritySettings.GetColor(skin.Rarity);
        }

        if (equipButton != null)
        {
            equipButton.interactable = true;
            equipButton.onClick.RemoveListener(HandleSelectClicked);
            equipButton.onClick.AddListener(HandleSelectClicked);
        }

        ShowPreview();
        RefreshState();
    }

    public void InitializePreview(WardrobeSkin wardrobeSkin)
    {
        manager = null;
        skin = wardrobeSkin;
        if (skin == null) return;
        if (nameText != null) nameText.text = skin.SkinName;
        if (rarityText != null)
        {
            rarityText.text = skin.Rarity.ToString();
            if (raritySettings != null) shapeItem.color = raritySettings.GetColor(skin.Rarity);
        }
        // In ItemPrefab the button is on the card root. Deactivating its
        // GameObject therefore deactivated the entire preview card.
        if (equipButton != null) equipButton.interactable = false;
        ShowPreview();
    }

    public void RefreshState()
    {
        if ( manager == null || skin == null)
            return;

        bool selected = manager.SelectedSkin == skin;
        bool equipped = manager.CurrentCustomization != null && manager.CurrentCustomization.Skin == skin;
    }

    private void HandleSelectClicked()
    {
        if (manager != null && !manager.SelectSkin(skin))
            Debug.LogWarning($"[Wardrobe] A skin {skin?.SkinId} não pertence ao inventário local.");
    }


    private void ShowPreview()
    {
        if (preview3D == null)
            preview3D = GetComponentInChildren<Preview3D>(true);

        if (preview3D == null)
        {
            Debug.LogWarning($"[{nameof(WardrobeItemCard)}] Preview3D is not assigned on {name}.");
            return;
        }

        GameObject previewPrefab = WardrobePreviewUtility.GetCapybaraPreviewPrefab();
        if (previewPrefab == null)
        {
            preview3D.Clear();
            return;
        }

        preview3D.Show(previewPrefab, previewObject =>
        {
            PlayerCustomizationApplier applier = previewObject.GetComponent<PlayerCustomizationApplier>();
            if (applier != null)
                applier.ApplySkinOnly(skin);
        });
    }

    private void OnDestroy()
    {
        if (equipButton != null) equipButton.onClick.RemoveListener(HandleSelectClicked);
    }
}
