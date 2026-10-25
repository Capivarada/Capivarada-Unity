using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Skin-box shop UI. Legacy equipment categories are deliberately not part of this flow.</summary>
public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopBoxCard boxCardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private Button boxesTabButton;
    [SerializeField] private Button pointsTabButton;
    [SerializeField] private GameObject boxesTabPanel;
    [SerializeField] private GameObject pointsTabPanel;
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TMP_Text detailsNameText;
    [SerializeField] private TMP_Text detailsPriceText;
    [SerializeField] private Image detailsRarityImage;
    [SerializeField] private Image detailsIconImage;
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private Button detailsPurchaseButton;
    [SerializeField] private Button detailsCloseButton;
    [SerializeField] private Transform rarityContent;
    [SerializeField] private ShopRaritySection raritySectionPrefab;
    [SerializeField] private BoxOpeningUI boxOpeningUI;
    private WardrobeBox selectedBox;
    private bool runtimePurchaseBinding;

    private void Awake()
    {
        if (detailsCloseButton == null && detailsPanel != null)
            foreach (Button button in detailsPanel.GetComponentsInChildren<Button>(true))
                if (button.name == "Button.Close") { detailsCloseButton = button; break; }
        if (boxesTabButton != null) boxesTabButton.onClick.AddListener(ShowBoxes);
        if (pointsTabButton != null) pointsTabButton.onClick.AddListener(ShowPoints);
        if (detailsPurchaseButton != null && !HasPersistentPurchaseBinding())
        {
            detailsPurchaseButton.onClick.AddListener(PurchaseSelected);
            runtimePurchaseBinding = true;
        }
        if (detailsCloseButton != null) detailsCloseButton.onClick.AddListener(HideDetails);
    }
    private void OnEnable()
    {
        if (shopManager != null) shopManager.OnRewardGranted += HandleRewardGranted;
        if (detailsPurchaseButton != null && !HasPersistentPurchaseBinding() && !runtimePurchaseBinding)
        {
            detailsPurchaseButton.onClick.AddListener(PurchaseSelected);
            runtimePurchaseBinding = true;
        }
        ShowBoxes();
    }
    private void OnDisable() { if (shopManager != null) shopManager.OnRewardGranted -= HandleRewardGranted; }
    public void Configure(ShopManager manager, ShopBoxCard prefab, Transform boxesContent)
    {
        shopManager = manager;
        boxCardPrefab = prefab;
        content = boxesContent;
    }
    public Button DetailsPurchaseButton => detailsPurchaseButton;
    public void ShowBoxes()
    {
        selectedBox = null;
        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (boxesTabPanel != null) boxesTabPanel.SetActive(true);
        if (pointsTabPanel != null) pointsTabPanel.SetActive(false);
        if (boxesTabButton != null) boxesTabButton.gameObject.SetActive(true);
        if (pointsTabButton != null) pointsTabButton.gameObject.SetActive(true);
        RefreshBoxes();
    }
    public void ShowPoints() { if (boxesTabPanel != null) boxesTabPanel.SetActive(false); if (pointsTabPanel != null) pointsTabPanel.SetActive(true); }

    private void RefreshBoxes()
    {
        if (content == null || boxCardPrefab == null || shopManager == null) return;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        foreach (WardrobeBox box in shopManager.AvailableBoxes
                     .Where(box => box != null && box.IsAvailable)
                     .OrderBy(box => (int)box.VisualRarity)
                     .ThenBy(box => box.BoxName))
            Instantiate(boxCardPrefab, content).Initialize(shopManager, box, ShowDetails);
    }
    private void ShowDetails(WardrobeBox box)
    {
        selectedBox = box;
        if (box != null) Debug.Log("[Shop] Caixa selecionada: " + box.BoxId);

        // The details panel is inactive after closing. Destroy its old cards
        // before activating it again, otherwise their Preview3D cameras wake
        // up for one frame alongside the newly created cards.
        ClearRaritySections();

        if (detailsPanel != null)
        {
            detailsPanel.SetActive(box != null);
            if (boxesTabPanel != null) boxesTabPanel.SetActive(box == null);
            if (pointsTabPanel != null) pointsTabPanel.SetActive(false);
            if (boxesTabButton != null) boxesTabButton.gameObject.SetActive(box == null);
            if (pointsTabButton != null) pointsTabButton.gameObject.SetActive(box == null);
        }
        if (box == null) return;
        if (detailsNameText != null) detailsNameText.text = box.BoxName;
        if (detailsPriceText != null) detailsPriceText.text = box.Price.ToString();
        if (detailsIconImage != null)
        {
            detailsIconImage.sprite = box.Sprite;
            detailsIconImage.enabled = box.Sprite != null;
            detailsIconImage.preserveAspect = true;
            detailsIconImage.color = raritySettings != null
                ? raritySettings.GetColor(box.VisualRarity)
                : Color.white;
        }
        if (detailsRarityImage != null)
            detailsRarityImage.color = raritySettings != null
                ? raritySettings.GetColor(box.VisualRarity)
                : Color.white;
        if (rarityContent == null || raritySectionPrefab == null) return;
        foreach (RarityDrop drop in box.DropRates)
            if (drop != null && drop.chance > 0f)
                Instantiate(raritySectionPrefab, rarityContent).Initialize(
                    drop.rarity,
                    drop.chance,
                    shopManager.Catalog);
    } 

    private void ClearRaritySections()
    {
        if (rarityContent == null) return;
        for (int i = rarityContent.childCount - 1; i >= 0; i--)
        {
            GameObject section = rarityContent.GetChild(i).gameObject;
            // Prevent an old preview camera from rendering again before its
            // end-of-frame destruction completes.
            section.SetActive(false);
            Destroy(section);
        }
    }
    
    public void HideDetails()
    {
        ShowDetails(null);
    }
    public void PurchaseSelected()
    {
        Debug.Log("[Shop] Botão de compra acionado.");
        if (shopManager == null || selectedBox == null)
        {
            Debug.LogWarning("[Shop] Selecione uma caixa antes de comprar.");
            return;
        }
        shopManager.PurchaseBox(selectedBox);
    }
    private void HandleRewardGranted(WardrobeBoxReward reward)
    {
        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (boxOpeningUI != null)
            boxOpeningUI.Play(reward, () => { if (selectedBox != null) ShowDetails(selectedBox); else ShowBoxes(); });
    }
    private void OnDestroy()
    {
        if (boxesTabButton != null) boxesTabButton.onClick.RemoveListener(ShowBoxes);
        if (pointsTabButton != null) pointsTabButton.onClick.RemoveListener(ShowPoints);
        if (detailsCloseButton != null) detailsCloseButton.onClick.RemoveListener(HideDetails);
        if (runtimePurchaseBinding && detailsPurchaseButton != null)
            detailsPurchaseButton.onClick.RemoveListener(PurchaseSelected);
    }

    public void ConfigureDetailsCloseButton(Button closeButton) => detailsCloseButton = closeButton;
    public void ConfigureDetailsPresentation(Image rarityImage, Image iconImage, RaritySettings settings)
    {
        detailsRarityImage = rarityImage;
        detailsIconImage = iconImage;
        raritySettings = settings;
    }

    private bool HasPersistentPurchaseBinding()
    {
        if (detailsPurchaseButton == null) return false;
        for (int i = 0; i < detailsPurchaseButton.onClick.GetPersistentEventCount(); i++)
            if (detailsPurchaseButton.onClick.GetPersistentTarget(i) == this &&
                detailsPurchaseButton.onClick.GetPersistentMethodName(i) == nameof(PurchaseSelected)) return true;
        return false;
    }
}
