using System;
using System.Collections.Generic;
using UnityEngine;

public class WardrobeManager : MonoBehaviour
{
    public static WardrobeManager Instance { get; private set; }

    [Header("Configured content")]
    [SerializeField] private WardrobeSkin defaultSkin;
    [SerializeField] private List<WardrobeSkin> availableSkins = new();
    [SerializeField] private List<WardrobeItem> availableItems = new();

    [Header("Owned content")]
    [SerializeField] private List<WardrobeSkin> ownedSkins = new();
    [SerializeField] private List<WardrobeItem> ownedItems = new();

    [Header("Bot content")]
    [SerializeField] private List<WardrobeSkin> botSkins = new();
    [SerializeField] private List<WardrobeItem> botItems = new();

    public event Action<PlayerCustomization> OnCustomizationChanged;

    public PlayerCustomization CurrentCustomization { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentCustomization = new PlayerCustomization
        {
            Skin = defaultSkin != null
                ? defaultSkin
                : GetConfiguredSkins().Count > 0
                    ? GetConfiguredSkins()[0]
                    : null
        };
    }

    public IReadOnlyList<WardrobeSkin> GetOwnedSkins()
    {
        return ownedSkins.Count > 0 ? ownedSkins : availableSkins;
    }

    public IReadOnlyList<WardrobeItem> GetOwnedItems()
    {
        return ownedItems.Count > 0 ? ownedItems : availableItems;
    }

    public IReadOnlyList<WardrobeSkin> GetConfiguredSkins()
    {
        return availableSkins;
    }

    public IReadOnlyList<WardrobeItem> GetConfiguredItems()
    {
        return availableItems;
    }

    public IEnumerable<WardrobeItem> GetItems(WardrobeCategory category)
    {
        foreach (WardrobeItem item in GetOwnedItems())
        {
            if (item == null)
                continue;

            if (category == WardrobeCategory.Todos || item.Category == category)
                yield return item;
        }
    }

    public void EquipSkin(WardrobeSkin skin)
    {
        if (skin == null || !Contains(GetOwnedSkins(), skin))
            return;

        CurrentCustomization.Skin = skin;
        NotifyCustomizationChanged();
    }

    public void EquipItem(WardrobeItem item)
    {
        if (item == null || !Contains(GetOwnedItems(), item))
            return;

        switch (item.Category)
        {
            case WardrobeCategory.Cabeca:
                CurrentCustomization.Head = item;
                break;
            case WardrobeCategory.Acessorios:
                CurrentCustomization.Accessory = item;
                break;
            case WardrobeCategory.Roupas:
                CurrentCustomization.Clothing = item;
                break;
            default:
                return;
        }

        NotifyCustomizationChanged();
    }

    public void Unequip(WardrobeCategory category)
    {
        switch (category)
        {
            case WardrobeCategory.Cabeca:
                CurrentCustomization.Head = null;
                break;
            case WardrobeCategory.Acessorios:
                CurrentCustomization.Accessory = null;
                break;
            case WardrobeCategory.Roupas:
                CurrentCustomization.Clothing = null;
                break;
            default:
                return;
        }

        NotifyCustomizationChanged();
    }

    public PlayerCustomization CreateRandomBotCustomization()
    {
        IReadOnlyList<WardrobeSkin> skins =
            botSkins.Count > 0 ? botSkins : GetOwnedSkins();
        IReadOnlyList<WardrobeItem> items =
            botItems.Count > 0 ? botItems : GetOwnedItems();

        PlayerCustomization customization = new()
        {
            Skin = Pick(skins, CurrentCustomization.Skin)
        };

        customization.Head = PickItem(items, WardrobeCategory.Cabeca);
        customization.Accessory = PickItem(items, WardrobeCategory.Acessorios);
        customization.Clothing = PickItem(items, WardrobeCategory.Roupas);
        return customization;
    }

    private void NotifyCustomizationChanged()
    {
        OnCustomizationChanged?.Invoke(CurrentCustomization);
    }

    private static bool Contains<T>(IReadOnlyList<T> values, T value)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(values[i], value))
                return true;
        }

        return false;
    }

    private static WardrobeSkin Pick(
        IReadOnlyList<WardrobeSkin> values,
        WardrobeSkin fallback)
    {
        List<WardrobeSkin> valid = new();
        foreach (WardrobeSkin value in values)
        {
            if (value != null)
                valid.Add(value);
        }

        return valid.Count == 0
            ? fallback
            : valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    private static WardrobeItem PickItem(
        IReadOnlyList<WardrobeItem> values,
        WardrobeCategory category)
    {
        List<WardrobeItem> valid = new();
        foreach (WardrobeItem value in values)
        {
            if (value != null && value.Category == category)
                valid.Add(value);
        }

        if (valid.Count == 0 || UnityEngine.Random.value < 0.25f)
            return null;

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }
}
