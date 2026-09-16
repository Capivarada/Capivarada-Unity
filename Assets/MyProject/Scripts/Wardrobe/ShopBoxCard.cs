using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBoxCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Transform previewRoot;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button purchaseButton;

    private ShopManager shopManager;
    private WardrobeBox box;
    private GameObject previewObject;
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
        ClearPreview();
        if (box == null || box.Prefab == null || previewRoot == null)
            return;

        previewObject = Instantiate(box.Prefab, previewRoot);
        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (previewObject != null)
            previewObject.transform.Rotate(Vector3.up, 35f * Time.deltaTime, Space.World);
    }

    private void ClearPreview()
    {
        if (previewObject != null)
            Destroy(previewObject);
        previewObject = null;
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandleSelected);
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(HandlePurchase);
        ClearPreview();
    }
}
