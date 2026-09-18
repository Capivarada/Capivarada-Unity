using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBoxCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Preview3D preview3D;

    private ShopManager shopManager;
    private WardrobeBox box;
    private Action<WardrobeBox> onSelected;

    public void Initialize(
        ShopManager manager,
        WardrobeBox wardrobeBox,
        Action<WardrobeBox> selectedCallback)
    {
        shopManager = manager;
        box = wardrobeBox;
        onSelected = selectedCallback;

        if (nameText != null)
            nameText.text = box != null ? box.BoxName : string.Empty;
        if (priceText != null)
            priceText.text = box != null ? box.Price.ToString() : string.Empty;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelected);
            selectButton.onClick.AddListener(HandleSelected);
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(HandlePurchase);
            purchaseButton.onClick.AddListener(HandlePurchase);
        }

        ShowPreview();
    }

    private void HandleSelected()
    {
        onSelected?.Invoke(box);
    }

    private void HandlePurchase()
    {
        onSelected?.Invoke(box);
        if (shopManager != null)
            shopManager.TryPurchaseBox(box, out _);
    }

    private void ShowPreview()
    {
        if (preview3D == null)
        {
            Debug.LogWarning($"[{nameof(ShopBoxCard)}] Preview3D is not assigned on {name}.");
            return;
        }

        if (previewImage != null)
            preview3D.TargetImage = previewImage;

        if (box == null || box.Prefab == null)
        {
            preview3D.Clear();
            return;
        }

        preview3D.Show(box.Prefab);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandleSelected);
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(HandlePurchase);

        if (preview3D != null)
            preview3D.Clear();
    }
}
