using UnityEngine;

public class WardrobeItemPreview : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 35f;

    private GameObject previewObject;

    public void ShowItem(WardrobeItem item, Transform root)
    {
        Clear();

        if (item == null || root == null)
            return;

        GameObject prefab = item.PreviewPrefab != null
            ? item.PreviewPrefab
            : item.EquippedPrefab;

        if (prefab != null)
            previewObject = Instantiate(prefab, root);

        ResetTransform();
    }

    public void ShowSkin(
        WardrobeSkin skin,
        GameObject capybaraPrefab,
        Transform root)
    {
        Clear();

        if (skin == null || capybaraPrefab == null || root == null)
            return;

        previewObject = Instantiate(capybaraPrefab, root);
        PlayerCustomizationApplier applier =
            previewObject.GetComponent<PlayerCustomizationApplier>();

        if (applier == null)
            applier = previewObject.AddComponent<PlayerCustomizationApplier>();

        applier.Apply(new PlayerCustomization { Skin = skin });
        ResetTransform();
    }

    private void Update()
    {
        if (previewObject != null)
        {
            previewObject.transform.Rotate(
                Vector3.up,
                rotationSpeed * Time.deltaTime,
                Space.World
            );
        }
    }

    private void ResetTransform()
    {
        if (previewObject == null)
            return;

        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localScale = Vector3.one;
    }

    private void Clear()
    {
        if (previewObject != null)
            Destroy(previewObject);

        previewObject = null;
    }

    private void OnDestroy()
    {
        Clear();
    }
}
