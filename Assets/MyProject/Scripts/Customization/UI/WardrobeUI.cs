using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Skin-only wardrobe: selection is pending until SaveSelectedSkin succeeds.</summary>
public class WardrobeUI : MonoBehaviour
{
    [SerializeField] private WardrobeManager wardrobeManager;
    [SerializeField] private PlayerCustomizationApplier mainPreview;
    [SerializeField] private WardrobeItemCard itemCardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private Button saveSkinButton;
    private WardrobeManager subscribedManager;

    private void Awake() { if (saveSkinButton != null) saveSkinButton.onClick.AddListener(SaveSelectedSkin); }
    private void OnEnable()
    {
        // The wardrobe service survives scene changes, whereas this serialized
        // scene reference can point to the manager instance Unity just destroyed.
        if (WardrobeManager.Instance != null) wardrobeManager = WardrobeManager.Instance;
        if (wardrobeManager == null) { Debug.LogError("[WardrobeUI] WardrobeManager não configurado.", this); return; }
        wardrobeManager.EnsureInitialized();
        subscribedManager = wardrobeManager;
        subscribedManager.OnCustomizationChanged += HandleCustomizationChanged;
        subscribedManager.OnSkinSelectionChanged += HandleSkinSelectionChanged;
        subscribedManager.OnInventoryChanged += RefreshCards;
        mainPreview?.ApplySkinOnly(wardrobeManager.ActiveSkin);
        RefreshCards();
    }
    private void OnDisable()
    {
        if (subscribedManager == null) return;
        subscribedManager.OnCustomizationChanged -= HandleCustomizationChanged;
        subscribedManager.OnSkinSelectionChanged -= HandleSkinSelectionChanged;
        subscribedManager.OnInventoryChanged -= RefreshCards;
        subscribedManager = null;
    }
    private void RefreshCards()
    {
        if (content == null || itemCardPrefab == null || wardrobeManager == null) return;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        List<WardrobeSkin> skins = wardrobeManager.GetOwnedSkins().Where(skin => skin != null)
            .OrderBy(skin => skin == wardrobeManager.Catalog?.DefaultSkin ? 0 : 1)
            .ThenBy(skin => (int)skin.Rarity).ThenBy(skin => skin.SkinName).ThenBy(skin => skin.SkinId).ToList();
        foreach (WardrobeSkin skin in skins) Instantiate(itemCardPrefab, content).Initialize(wardrobeManager, skin);
    }
    private void HandleSkinSelectionChanged(WardrobeSkin skin)
    {
        mainPreview?.ApplySkinOnly(skin);
        RefreshCardStates();
    }
    private void HandleCustomizationChanged(PlayerCustomization _)
    {
        // Inventory changes are handled by OnInventoryChanged. Rebuilding here
        // as well recreated every RenderTexture twice after a save.
        RefreshCardStates();
    }
    private void RefreshCardStates()
    {
        if (content == null) return;
        foreach (WardrobeItemCard card in content.GetComponentsInChildren<WardrobeItemCard>()) card.RefreshState();
    }
    public void SaveSelectedSkin()
    {
        if (wardrobeManager == null) return;
        if (saveSkinButton != null) saveSkinButton.interactable = false;
        wardrobeManager.SaveSelectedSkin(
            () => { if (saveSkinButton != null) saveSkinButton.interactable = true; },
            error => { Debug.LogWarning("[Wardrobe] " + error); if (saveSkinButton != null) saveSkinButton.interactable = true; });
    }
    public void Configure(WardrobeManager manager, PlayerCustomizationApplier preview, WardrobeItemCard prefab, Transform cardsContent, Button saveButton)
    {
        wardrobeManager = manager;
        mainPreview = preview;
        itemCardPrefab = prefab;
        content = cardsContent;
        saveSkinButton = saveButton;
    }
    private void OnDestroy() { if (saveSkinButton != null) saveSkinButton.onClick.RemoveListener(SaveSelectedSkin); }
}
