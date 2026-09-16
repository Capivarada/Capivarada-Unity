using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Shop")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopBoxCard boxCardPrefab;
    [SerializeField] private Transform content;

    [Header("Category buttons")]
    [SerializeField] private Button colorsButton;
    [SerializeField] private Button headButton;
    [SerializeField] private Button clothingButton;
    [SerializeField] private Button accessoriesButton;

    [Header("Box details")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TMP_Text detailsNameText;
    [SerializeField] private TMP_Text detailsPriceText;
    [SerializeField] private Button detailsPurchaseButton;
    [SerializeField] private Transform rarityContent;
    [SerializeField] private ShopRaritySection raritySectionPrefab;
    [SerializeField] private BoxOpeningUI boxOpeningUI;

    private WardrobeContentType currentType = WardrobeContentType.Skin;
    private WardrobeBox selectedBox;

    private void Awake()
    {
        BindCategoryButton(colorsButton, WardrobeContentType.Skin);
        BindCategoryButton(headButton, WardrobeContentType.Head);
        BindCategoryButton(clothingButton, WardrobeContentType.Cloth);
        BindCategoryButton(accessoriesButton, WardrobeContentType.Accessory);

        if (detailsPurchaseButton != null)
        {
            detailsPurchaseButton.onClick.RemoveListener(PurchaseSelected);
            detailsPurchaseButton.onClick.AddListener(PurchaseSelected);
        }
    }

    private void OnEnable()
    {
        if (shopManager != null)
            shopManager.OnRewardGranted += HandleRewardGranted;
        RefreshBoxes();
    }

    private void OnDisable()
    {
        if (shopManager != null)
            shopManager.OnRewardGranted -= HandleRewardGranted;
    }

    public void SelectCategory(WardrobeContentType type)
    {
        currentType = type;
        RefreshBoxes();
    }

    private void RefreshBoxes()
    {
        if (content == null || boxCardPrefab == null || shopManager == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        foreach (WardrobeBox box in shopManager.AvailableBoxes)
        {
            if (box == null || box.Type != currentType)
                continue;

            ShopBoxCard card = Instantiate(boxCardPrefab, content);
            card.Initialize(shopManager, box, ShowDetails);
        }
    }

    private void ShowDetails(WardrobeBox box)
    {
        selectedBox = box;
        if (detailsPanel != null)
            detailsPanel.SetActive(box != null);
        if (box == null)
            return;

        if (detailsNameText != null)
            detailsNameText.text = box.BoxName;
        if (detailsPriceText != null)
            detailsPriceText.text = box.Price.ToString();

        RebuildRaritySections(box);
    }

    private void RebuildRaritySections(WardrobeBox box)
    {
        if (rarityContent == null || raritySectionPrefab == null)
            return;

        for (int i = rarityContent.childCount - 1; i >= 0; i--)
            Destroy(rarityContent.GetChild(i).gameObject);

        foreach (RarityDrop drop in box.DropRates)
        {
            if (drop == null || drop.chance <= 0f)
                continue;

            ShopRaritySection section = Instantiate(raritySectionPrefab, rarityContent);
            section.Initialize(drop);
        }
    }

    private void PurchaseSelected()
    {
        if (shopManager != null && selectedBox != null)
            shopManager.TryPurchaseBox(selectedBox, out _);
    }

    private void HandleRewardGranted(WardrobeBoxReward reward)
    {
        if (boxOpeningUI != null)
            boxOpeningUI.Play(reward);
    }

    private void BindCategoryButton(Button button, WardrobeContentType type)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectCategory(type));
    }

    private void OnDestroy()
    {
        if (detailsPurchaseButton != null)
            detailsPurchaseButton.onClick.RemoveListener(PurchaseSelected);
    }
}
