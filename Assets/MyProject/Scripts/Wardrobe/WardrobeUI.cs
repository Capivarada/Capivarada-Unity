using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WardrobeManager wardrobeManager;
    [SerializeField] private PlayerCustomizationApplier mainPreview;
    [SerializeField] private GameObject capybaraPreviewPrefab;
    [SerializeField] private WardrobeItemCard itemCardPrefab;
    [SerializeField] private Transform content;

    [Header("Category buttons")]
    [SerializeField] private Button colorsButton;
    [SerializeField] private Button headButton;
    [SerializeField] private Button accessoriesButton;
    [SerializeField] private Button clothingButton;
    [SerializeField] private Button allButton;

    private WardrobeCategory currentCategory = WardrobeCategory.Cores;

    private void Awake()
    {
        if (wardrobeManager == null)
            wardrobeManager = WardrobeManager.Instance;

        BindCategoryButton(colorsButton, WardrobeCategory.Cores);
        BindCategoryButton(headButton, WardrobeCategory.Cabeca);
        BindCategoryButton(accessoriesButton, WardrobeCategory.Acessorios);
        BindCategoryButton(clothingButton, WardrobeCategory.Roupas);
        BindCategoryButton(allButton, WardrobeCategory.Todos);
    }

    private void OnEnable()
    {
        if (wardrobeManager == null)
            wardrobeManager = WardrobeManager.Instance;

        if (wardrobeManager != null)
        {
            wardrobeManager.OnCustomizationChanged += HandleCustomizationChanged;
            ApplyMainPreview();
            RefreshCards();
        }
    }

    private void OnDisable()
    {
        if (wardrobeManager != null)
            wardrobeManager.OnCustomizationChanged -= HandleCustomizationChanged;
    }

    public void SelectCategory(WardrobeCategory category)
    {
        currentCategory = category;
        RefreshCards();
    }

    public void SelectColors() => SelectCategory(WardrobeCategory.Cores);
    public void SelectHead() => SelectCategory(WardrobeCategory.Cabeca);
    public void SelectAccessories() => SelectCategory(WardrobeCategory.Acessorios);
    public void SelectClothing() => SelectCategory(WardrobeCategory.Roupas);
    public void SelectAll() => SelectCategory(WardrobeCategory.Todos);

    private void RefreshCards()
    {
        if (wardrobeManager == null || itemCardPrefab == null || content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        if (currentCategory == WardrobeCategory.Cores)
        {
            foreach (WardrobeSkin skin in wardrobeManager.GetOwnedSkins())
            {
                if (skin == null)
                    continue;

                WardrobeItemCard card = Instantiate(itemCardPrefab, content);
                card.Initialize(
                    wardrobeManager,
                    null,
                    skin,
                    capybaraPreviewPrefab
                );
            }
        }
        else
        {
            foreach (WardrobeItem item in wardrobeManager.GetItems(currentCategory))
            {
                WardrobeItemCard card = Instantiate(itemCardPrefab, content);
                card.Initialize(
                    wardrobeManager,
                    item,
                    null,
                    capybaraPreviewPrefab
                );
            }
        }
    }

    private void HandleCustomizationChanged(PlayerCustomization customization)
    {
        ApplyMainPreview();

        if (content == null)
            return;

        foreach (WardrobeItemCard card in content.GetComponentsInChildren<WardrobeItemCard>())
            card.RefreshState();
    }

    private void ApplyMainPreview()
    {
        if (mainPreview != null && wardrobeManager != null)
            mainPreview.Apply(wardrobeManager.CurrentCustomization);
    }

    private void BindCategoryButton(Button button, WardrobeCategory category)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectCategory(category));
    }
}
