using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Filters the authoritative Economy V2 inventory into cosmetic entitlement IDs.</summary>
public sealed class CosmeticInventory : MonoBehaviour
{
    [SerializeField] private EconomyV2Service inventory;
    private readonly HashSet<string> ownedIds = new(StringComparer.Ordinal);
    public event Action Changed;
    public bool Owns(string itemId) => !string.IsNullOrWhiteSpace(itemId) && ownedIds.Contains(itemId);
    private void OnEnable()
    {
        if (inventory == null) inventory = ServiceHub.Instance != null ? ServiceHub.Instance.EconomyV2 : EconomyV2Service.Instance;
        if (inventory != null) inventory.InventoryChanged += OnInventoryChanged;
    }
    private void OnDisable()
    {
        if (inventory != null) inventory.InventoryChanged -= OnInventoryChanged;
    }
    public void Refresh()
    {
        if (inventory == null) inventory = ServiceHub.Instance != null ? ServiceHub.Instance.EconomyV2 : EconomyV2Service.Instance;
        inventory?.RefreshInventory();
    }
    private void OnInventoryChanged(IReadOnlyList<PlayFab.EconomyModels.InventoryItem> items, int _)
    {
        ownedIds.Clear();
        if (items != null) foreach (PlayFab.EconomyModels.InventoryItem item in items) if (item != null && (item.Amount ?? 0) > 0 && !string.IsNullOrWhiteSpace(item.Id)) ownedIds.Add(item.Id);
        Changed?.Invoke();
    }
}
