using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI endpoint for a cell in the shared capybara-preview atlas.</summary>
public sealed class Preview3D : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    private Preview3DAtlas.Slot slot;

    public void Show(GameObject prefab) => Show(prefab, null, null, Rarity.Comum);

    public void Show(GameObject prefab, Action<GameObject> configure) => Show(prefab, configure, null, Rarity.Comum);

    public void Show(GameObject prefab, Action<GameObject> configure, string stableKey, Rarity rarity)
    {
        Clear();
        if (prefab == null) return;
        if (targetImage == null) targetImage = GetComponentInChildren<RawImage>(true);
        if (targetImage == null) { Debug.LogError("[Preview3D] RawImage não configurada.", this); return; }
        slot = Preview3DAtlas.Instance.Register(prefab, configure, targetImage, stableKey, rarity);
    }

    public void Clear()
    {
        if (slot != null) Preview3DAtlas.Instance.Unregister(slot);
        slot = null;
        if (targetImage != null) { targetImage.texture = null; targetImage.enabled = false; }
    }

    private void Awake() { if (targetImage == null) targetImage = GetComponentInChildren<RawImage>(true); }
    private void OnDestroy() => Clear();
}
