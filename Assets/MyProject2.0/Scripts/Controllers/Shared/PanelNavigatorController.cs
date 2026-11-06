using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PanelNavigatorController : MonoBehaviour
{
    [Serializable] private sealed class PanelEntry { public string id; public GameObject panel; }
    [SerializeField] private List<PanelEntry> panels = new();
    [SerializeField] private string initialPanelId;
    [SerializeField] private bool hideOthersOnStart = true;
    [SerializeField] private bool rememberHistory = true;
    public string CurrentPanelId { get; private set; }
    public event Action<string> PanelOpened;
    private readonly Stack<string> history = new();
    private void Start() { if (hideOthersOnStart) ShowInternal(initialPanelId); }
    public void Show(string panelId)
    {
        if (rememberHistory && !string.IsNullOrEmpty(CurrentPanelId) && CurrentPanelId != panelId) history.Push(CurrentPanelId);
        ShowInternal(panelId);
    }
    public void Back()
    {
        if (history.Count == 0) { if (!string.IsNullOrEmpty(initialPanelId)) ShowInternal(initialPanelId); return; }
        ShowInternal(history.Pop());
    }
    public void ClearHistory() => history.Clear();
    private void ShowInternal(string panelId)
    {
        foreach (PanelEntry entry in panels) { if (entry?.panel != null) entry.panel.SetActive(string.Equals(entry.id, panelId, StringComparison.Ordinal)); }
        CurrentPanelId = panelId; PanelOpened?.Invoke(panelId);
    }
    public void HideAll() { foreach (PanelEntry entry in panels) if (entry?.panel != null) entry.panel.SetActive(false); CurrentPanelId = null; }
}
