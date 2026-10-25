using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBoxCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image boxImage;
    [SerializeField] private Image shapeImage;
    [SerializeField] private RaritySettings raritySettings;

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
        if (boxImage != null)
        {
            boxImage.sprite = box != null ? box.Sprite : null;
            // Box sprites are authored white so the same icon inherits the
            // selected box rarity everywhere it is displayed.
            boxImage.color = box != null && raritySettings != null
                ? raritySettings.GetColor(box.VisualRarity)
                : Color.white;
            boxImage.preserveAspect = true;
        }
        if (shapeImage != null)
            shapeImage.color = box != null && raritySettings != null ? raritySettings.GetColor(box.VisualRarity) : Color.white;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelected);
            selectButton.onClick.AddListener(HandleSelected);
        }

    }

    private void HandleSelected()
    {
        onSelected?.Invoke(box);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandleSelected);
    }
}
