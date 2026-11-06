using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.EconomyModels;
using UnityEngine;

/// <summary>
/// Leitura nativa do Economy v2 para o inventário da conta autenticada.
/// Escritas sensíveis, como recompensas de caixa, continuam sob autoridade do backend.
/// Este é um serviço de runtime e deve permanecer na assembly Assembly-CSharp.
/// </summary>
public class InventoryService : MonoBehaviour
{
    [SerializeField] private string pointsCurrencyId = "PA";

    public static InventoryService Instance { get; private set; }
    public event Action<IReadOnlyList<InventoryItem>, int> Changed;
    public event Action<string> Failed;

    private readonly List<InventoryItem> items = new();

    public IReadOnlyList<InventoryItem> Items => items;
    public int Points { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Refresh()
    {
        items.Clear();
        LoadPage(null);
    }

    public bool Owns(string itemId)
    {
        return items.Exists(item => string.Equals(item.Id, itemId, StringComparison.Ordinal) && (item.Amount ?? 0) > 0);
    }

    private void LoadPage(string continuationToken)
    {
        PlayFabEconomyAPI.GetInventoryItems(new GetInventoryItemsRequest
        {
            CollectionId = "default",
            Count = 50,
            ContinuationToken = continuationToken
        }, result =>
        {
            if (result.Items != null)
            {
                items.AddRange(result.Items);
            }

            if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
            {
                LoadPage(result.ContinuationToken);
                return;
            }

            Points = 0;
            foreach (InventoryItem item in items)
            {
                if (string.Equals(item.Id, pointsCurrencyId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Type, "currency", StringComparison.OrdinalIgnoreCase))
                {
                    Points += item.Amount ?? 0;
                }
            }

            Changed?.Invoke(items, Points);
        }, error => Failed?.Invoke(error.ErrorMessage));
    }
}
