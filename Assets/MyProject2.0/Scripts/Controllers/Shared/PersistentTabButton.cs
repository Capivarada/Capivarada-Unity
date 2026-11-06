using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PersistentTabButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PersistentTabGroup group;
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private string tabId;
    [SerializeField] private bool selectOnPointerClick = true;
    public string TabId => tabId;
    public event Action<PersistentTabButton> Selected;
    private void Awake() { if (group != null) group.Register(this); }
    private void OnDestroy() { if (group != null) group.Unregister(this); }
    public void OnPointerClick(PointerEventData eventData) { if (selectOnPointerClick) Select(); }
    public void Select() { if (group != null) group.Select(this); else SetSelected(true); }
    internal void SetSelected(bool selected) { if (targetImage != null) targetImage.sprite = selected ? selectedSprite : normalSprite; if (selected) Selected?.Invoke(this); }
    // Used by cards whose selected appearance is driven by saved game state,
    // rather than by a transient pointer/navigation selection.
    public void SetSelectedVisual(bool selected) { if (targetImage != null) targetImage.sprite = selected ? selectedSprite : normalSprite; }
}

