using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps exactly one PersistentTabButton visually selected at a time.
/// Place this component on the parent object that owns one tab set.
/// </summary>
public sealed class PersistentTabGroup : MonoBehaviour
{
    [SerializeField] private PersistentTabButton initiallySelected;
    private readonly List<PersistentTabButton> buttons = new();

    private void Start()
    {
        if (initiallySelected != null) Select(initiallySelected);
    }

    public void Register(PersistentTabButton button)
    {
        if (button != null && !buttons.Contains(button)) buttons.Add(button);
    }

    public void Unregister(PersistentTabButton button) => buttons.Remove(button);

    public void Select(PersistentTabButton selected)
    {
        foreach (PersistentTabButton button in buttons)
            if (button != null) button.SetSelected(button == selected);
    }
}
