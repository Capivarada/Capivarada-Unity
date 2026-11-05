using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.EconomyModels;
using UnityEngine;

/// <summary>
/// Read-only client access to Economy v2. Box opening never happens here: it is sent to the Azure Function so PA, drops and duplicate conversion stay authoritative.
/// </summary>
public sealed class EconomyV2Service : MonoBehaviour
{
    [Serializable] public class BoxOpenRequest { public string itemId; public string idempotencyId; }
    [Serializable] public class BoxOpenResult { public string itemId; public string skinId; public string rarity; public bool duplicate; public int duplicatePoints; public int remainingPoints; }
    [SerializeField] private CapivaradaBackendService backend;
    [Tooltip("Optional Economy V2 Store ID. Leave empty until the store exists in PlayFab.")]
    [SerializeField] private string storeId = "";

    public static EconomyV2Service Instance { get; private set; }
    public IReadOnlyList<CatalogItem> StoreItems => storeItems;
    public IReadOnlyList<InventoryItem> InventoryItems => inventoryItems;
    public int Points { get; private set; }
    public event Action<IReadOnlyList<CatalogItem>> CatalogChanged;
    public event Action<IReadOnlyList<InventoryItem>, int> InventoryChanged;
    public event Action<string> Failed;
    private readonly List<CatalogItem> storeItems = new();
    private readonly List<InventoryItem> inventoryItems = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        DontDestroyOnLoad(gameObject);
    }

    public void RefreshStore()
    {
        storeItems.Clear();
        SearchPage(null);
    }

    /// <summary>Loads the authenticated player's Economy V2 inventory and PA balance.</summary>
    public void RefreshInventory()
    {
        inventoryItems.Clear();
        LoadInventoryPage(null);
    }

    public void OpenBox(string itemId, Action<BoxOpenResult> succeeded, Action<string> failed = null)
    {
        if (string.IsNullOrWhiteSpace(itemId)) { (failed ?? Failed)?.Invoke("A caixa não possui Item ID V2."); return; }
        if (backend == null) { (failed ?? Failed)?.Invoke("CapivaradaBackendService não foi configurado em Services."); return; }
        backend.Post<BoxOpenRequest, BoxOpenResult>("economy/boxes/open", new BoxOpenRequest { itemId = itemId, idempotencyId = Guid.NewGuid().ToString("N") }, succeeded, failed ?? Failed);
    }

    private void SearchPage(string token)
    {
        PlayFabEconomyAPI.SearchItems(new SearchItemsRequest
        {
            Count = 50,
            ContinuationToken = token,
            Store = string.IsNullOrWhiteSpace(storeId) ? null : new StoreReference { Id = storeId }
        }, result =>
        {
            if (result.Items != null)
                foreach (CatalogItem item in result.Items)
                    if (IsBox(item)) storeItems.Add(item);
            if (!string.IsNullOrWhiteSpace(result.ContinuationToken)) { SearchPage(result.ContinuationToken); return; }
            CatalogChanged?.Invoke(storeItems);
        }, error => Failed?.Invoke(error.ErrorMessage));
    }

    private void LoadInventoryPage(string continuationToken)
    {
        PlayFabEconomyAPI.GetInventoryItems(new GetInventoryItemsRequest
        {
            CollectionId = "default",
            Count = 50,
            ContinuationToken = continuationToken
        }, result =>
        {
            if (result.Items != null)
                inventoryItems.AddRange(result.Items);

            if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
            {
                LoadInventoryPage(result.ContinuationToken);
                return;
            }

            Points = 0;
            foreach (InventoryItem item in inventoryItems)
            {
                if (string.Equals(item.Id, "PA", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Type, "currency", StringComparison.OrdinalIgnoreCase))
                    Points += item.Amount ?? 0;
            }

            InventoryChanged?.Invoke(inventoryItems, Points);
        }, error => Failed?.Invoke(error.ErrorMessage));
    }

    private static bool IsBox(CatalogItem item)
    {
        if (item?.Tags == null) return false;
        return item.Tags.Exists(tag => string.Equals(tag, "capivarada-box", StringComparison.OrdinalIgnoreCase));
    }
}
