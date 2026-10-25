using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WardrobeManager : MonoBehaviour
{
    public static WardrobeManager Instance { get; private set; }
    [SerializeField] private SkinCatalog skinCatalog;
    [SerializeField] private List<WardrobeSkin> botSkins = new();
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PlayFabEconomyService economyService;
    public event Action<PlayerCustomization> OnCustomizationChanged;
    public event Action<WardrobeSkin> OnSkinSelectionChanged;
    public event Action OnInventoryChanged;
    public PlayerCustomization CurrentCustomization { get; private set; }
    public WardrobeSkin SelectedSkin { get; private set; }
    public SkinCatalog Catalog => skinCatalog;
    public PlayFabEconomyService EconomyService => economyService;
    private bool subscribed;
    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        InitializeState();
    }
    private void OnEnable()
    {
        Subscribe();
    }
    private void Start()
    {
        if (PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsLoggedIn)
            RefreshEconomyAfterLogin();
    }
    private void OnDisable()
    {
        Unsubscribe();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Configures the persistent runtime wardrobe before any lobby UI is opened.</summary>
    public void Configure(SkinCatalog catalog, InventoryManager inventory, PlayFabEconomyService economy)
    {
        if (catalog == null)
        {
            Debug.LogError("[Wardrobe] SkinCatalog não configurado.", this);
            return;
        }

        Unsubscribe();
        skinCatalog = catalog;
        inventoryManager = inventory;
        economyService = economy;
        initialized = false;
        InitializeState();
        if (isActiveAndEnabled) Subscribe();
    }
    public IReadOnlyList<WardrobeSkin> GetOwnedSkins() => inventoryManager != null ? inventoryManager.OwnedSkins : Array.Empty<WardrobeSkin>();
    public WardrobeSkin ActiveSkin => SelectedSkin ?? CurrentCustomization?.Skin ?? skinCatalog?.DefaultSkin;

    /// <summary>Ensures UI can safely open after a domain reload or scene transition.</summary>
    public void EnsureInitialized() => InitializeState();
    public bool SelectSkin(WardrobeSkin skin)
    {
        if (skin == null || !GetOwnedSkins().Contains(skin)) return false;
        SelectedSkin = skin; OnSkinSelectionChanged?.Invoke(skin); return true;
    }
    public void DiscardSelectedSkin()
    {
        SelectedSkin = CurrentCustomization?.Skin ?? skinCatalog?.DefaultSkin;
        OnSkinSelectionChanged?.Invoke(SelectedSkin);
    }
    public void ApplyConfirmedSkin(WardrobeSkin skin)
    {
        if (skin == null) return;
        if (inventoryManager != null && inventoryManager.Grant(skin)) OnInventoryChanged?.Invoke();
    }
    public void SaveSelectedSkin(Action success, Action<string> error)
    {
        if (SelectedSkin == null || economyService == null) { error?.Invoke("Seleção ou economia indisponível."); return; }
        economyService.EquipSkin(SelectedSkin.SkinId, () => { CurrentCustomization.Skin = SelectedSkin; OnCustomizationChanged?.Invoke(CurrentCustomization); success?.Invoke(); }, error);
    }
    public PlayerCustomization CreateRandomBotSkinCustomization()
    {
        IReadOnlyList<WardrobeSkin> candidates = botSkins.Count > 0 ? botSkins : skinCatalog?.AllSkins;
        WardrobeSkin skin = candidates != null && candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : skinCatalog?.DefaultSkin;
        return new PlayerCustomization { Skin = skin };
    }
    private void ApplyServerInventory(IReadOnlyList<string> ids, string equippedId)
    {
        if (skinCatalog == null) return;
        List<WardrobeSkin> owned = skinCatalog.ResolveOwned(ids);
        Debug.Log($"[Wardrobe] Inventário sincronizado: {string.Join(", ", owned.Select(skin => skin.SkinId))}");
        inventoryManager?.ReplaceOwnedSkins(owned, skinCatalog.DefaultSkin);
        OnInventoryChanged?.Invoke();
        CurrentCustomization.Skin = skinCatalog.FindById(equippedId) ?? skinCatalog.DefaultSkin;
        SelectedSkin = CurrentCustomization.Skin; OnCustomizationChanged?.Invoke(CurrentCustomization);
    }
    private void RefreshEconomyAfterLogin()
    {
        if (PlayFabAuthManager.Instance == null || PlayFabAuthManager.Instance.IsGuest || economyService == null) return;
        economyService.Refresh(message => Debug.LogWarning("[Wardrobe] N\u00e3o foi poss\u00edvel carregar o invent\u00e1rio: " + message));
    }

    private void InitializeState()
    {
        if (skinCatalog == null) return;
        if (!initialized)
        {
            inventoryManager?.ReplaceOwnedSkins(Array.Empty<WardrobeSkin>(), skinCatalog.DefaultSkin);
            CurrentCustomization = new PlayerCustomization { Skin = skinCatalog.DefaultSkin };
            SelectedSkin = CurrentCustomization.Skin;
            initialized = true;
            return;
        }

        CurrentCustomization ??= new PlayerCustomization { Skin = skinCatalog.DefaultSkin };
        if (CurrentCustomization.Skin == null) CurrentCustomization.Skin = skinCatalog.DefaultSkin;
        if (SelectedSkin == null) SelectedSkin = CurrentCustomization.Skin;
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (economyService != null) economyService.OnInventoryLoaded += ApplyServerInventory;
        if (PlayFabAuthManager.Instance != null) PlayFabAuthManager.Instance.OnLoginSuccess += RefreshEconomyAfterLogin;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (economyService != null) economyService.OnInventoryLoaded -= ApplyServerInventory;
        if (PlayFabAuthManager.Instance != null) PlayFabAuthManager.Instance.OnLoginSuccess -= RefreshEconomyAfterLogin;
        subscribed = false;
    }
}
