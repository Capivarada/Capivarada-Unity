using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct WardrobeBoxReward
{
    public WardrobeBoxReward(Rarity rarity, WardrobeContent item)
    {
        Rarity = rarity;
        Item = item;
    }

    public Rarity Rarity { get; }
    public WardrobeContent Item { get; }
}

public class ShopManager : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private List<WardrobeBox> availableBoxes = new();
    [SerializeField] private int localCurrency;

    public IReadOnlyList<WardrobeBox> AvailableBoxes => availableBoxes;
    public event Action<WardrobeBoxReward> OnRewardGranted;

    public int LocalCurrency => localCurrency;

    public bool TryPurchaseBox(WardrobeBox box, out WardrobeBoxReward reward)
    {
        reward = default;

        if (box == null || !availableBoxes.Contains(box) || localCurrency < box.Price)
            return false;

        if (!TryResolveReward(box, out reward))
            return false;

        localCurrency -= box.Price;
        GrantAndNotify(reward);
        return true;
    }

    public bool TryOpenBox(WardrobeBox box, out WardrobeBoxReward reward)
    {
        reward = default;

        if (box == null || !availableBoxes.Contains(box))
            return false;

        if (!TryResolveReward(box, out reward))
            return false;

        GrantAndNotify(reward);
        return true;
    }

    private bool TryResolveReward(WardrobeBox box, out WardrobeBoxReward reward)
    {
        reward = default;

        if (!TrySelectDrop(box.DropRates, out RarityDrop drop))
            return false;

        if (!TrySelectItem(drop, box.Type, out WardrobeContent item))
            return false;

        reward = new WardrobeBoxReward(drop.rarity, item);
        return true;
    }

    private void GrantAndNotify(WardrobeBoxReward reward)
    {
        if (inventoryManager != null)
            inventoryManager.Grant(reward.Item);

        OnRewardGranted?.Invoke(reward);
    }

    private static bool TrySelectDrop(
        IReadOnlyList<RarityDrop> drops,
        out RarityDrop selected)
    {
        selected = null;
        float totalChance = 0f;

        foreach (RarityDrop drop in drops)
        {
            if (drop != null && drop.chance > 0f)
                totalChance += drop.chance;
        }

        if (totalChance <= 0f)
            return false;

        float roll = UnityEngine.Random.value * totalChance;
        foreach (RarityDrop drop in drops)
        {
            if (drop == null || drop.chance <= 0f)
                continue;

            roll -= drop.chance;
            if (roll <= 0f)
            {
                selected = drop;
                return true;
            }
        }

        return false;
    }

    private bool TrySelectItem(
        RarityDrop drop,
        WardrobeContentType type,
        out WardrobeContent selected)
    {
        selected = null;
        List<WardrobeContent> validItems = new();

        foreach (WardrobeContent item in drop.items)
        {
            if (item == null || !MatchesType(item, type) ||
                (inventoryManager != null && !inventoryManager.CanGrant(item)))
                continue;

            validItems.Add(item);
        }

        if (validItems.Count == 0)
            return false;

        selected = validItems[UnityEngine.Random.Range(0, validItems.Count)];
        return true;
    }

    private static bool MatchesType(WardrobeContent item, WardrobeContentType type)
    {
        return type switch
        {
            WardrobeContentType.Skin => item is WardrobeSkin,
            WardrobeContentType.Head => item is WardrobeHead,
            WardrobeContentType.Cloth => item is WardrobeCloth,
            WardrobeContentType.Accessory => item is WardrobeAccessory,
            _ => false
        };
    }
}
