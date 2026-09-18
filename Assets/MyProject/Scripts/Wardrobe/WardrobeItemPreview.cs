using UnityEngine;
using UnityEngine.UI;

public class WardrobeItemPreview : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    [SerializeField] private Preview3D preview3D;

    public void Bind(RawImage rawImage, Transform root = null)
    {
        targetImage = rawImage;
        EnsurePreview3D();
        if (preview3D != null)
            preview3D.TargetImage = targetImage;
    }

    public void ShowItem(WardrobeItem item, Transform root = null)
    {
        if (item == null)
        {
            ClearPreview();
            return;
        }

        ShowContent(item.PreviewPrefab != null ? item.PreviewPrefab : item.EquippedPrefab, root);
    }

    public void ShowContent(WardrobeEquipment item, Transform root = null)
    {
        if (item == null)
        {
            ClearPreview();
            return;
        }

        ShowContent(WardrobePreviewUtility.GetContentPreviewPrefab(item), root);
    }

    public void ShowContent(GameObject prefab, Transform root = null)
    {
        EnsurePreview3D();
        if (preview3D == null)
            return;

        preview3D.TargetImage = targetImage;
        preview3D.Show(prefab);
    }

    public void ShowSkin(WardrobeSkin skin, GameObject capybaraPrefab, Transform root = null)
    {
        EnsurePreview3D();
        if (preview3D == null || skin == null || capybaraPrefab == null)
        {
            ClearPreview();
            return;
        }

        preview3D.TargetImage = targetImage;
        preview3D.Show(capybaraPrefab, previewObject =>
        {
            if (previewObject == null)
                return;

            PlayerCustomizationApplier applier = previewObject.GetComponent<PlayerCustomizationApplier>();
            if (applier == null)
                applier = previewObject.AddComponent<PlayerCustomizationApplier>();

            applier.Apply(new PlayerCustomization { Skin = skin });
        });
    }

    public void ShowCustomizationPreview(
        PlayerCustomization customization,
        WardrobeCategory category,
        GameObject capybaraPrefab,
        Transform root = null)
    {
        EnsurePreview3D();
        if (preview3D == null || capybaraPrefab == null)
        {
            ClearPreview();
            return;
        }

        PlayerCustomization mutated = customization == null ? new PlayerCustomization() : customization.Clone();
        if (mutated != null)
        {
            switch (category)
            {
                case WardrobeCategory.Cores:
                    mutated.Skin = WardrobeManager.Instance != null && WardrobeManager.Instance.DefaultSkin != null
                        ? WardrobeManager.Instance.DefaultSkin
                        : mutated.Skin;
                    break;
                case WardrobeCategory.Cabeca:
                    mutated.Head = null;
                    break;
                case WardrobeCategory.Acessorios:
                    mutated.Accessory = null;
                    break;
                case WardrobeCategory.Roupas:
                    mutated.Clothing = null;
                    break;
            }
        }

        preview3D.TargetImage = targetImage;
        preview3D.Show(capybaraPrefab, previewObject =>
        {
            if (previewObject == null)
                return;

            PlayerCustomizationApplier applier = previewObject.GetComponent<PlayerCustomizationApplier>();
            if (applier == null)
                applier = previewObject.AddComponent<PlayerCustomizationApplier>();

            applier.Apply(mutated);
        });
    }

    public void ClearPreview()
    {
        if (preview3D != null)
            preview3D.Clear();
    }

    private void EnsurePreview3D()
    {
        if (preview3D != null)
            return;

        preview3D = GetComponent<Preview3D>();
        if (preview3D == null)
            preview3D = gameObject.GetComponent<Preview3D>();

        if (preview3D == null)
        {
            Debug.LogWarning("[WardrobeItemPreview] Preview3D reference is missing; assign it in the Inspector.");
            return;
        }

        preview3D.TargetImage = targetImage;
    }

    private void OnDestroy()
    {
        if (preview3D != null)
            preview3D.Clear();
    }
}
