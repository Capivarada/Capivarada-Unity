using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct WardrobeBoxReward
{
    public WardrobeBoxReward(Rarity rarity, WardrobeSkin skin, bool duplicate, int points) { Rarity = rarity; Skin = skin; IsDuplicate = duplicate; DuplicatePoints = points; }
    public Rarity Rarity { get; } public WardrobeSkin Skin { get; } public bool IsDuplicate { get; } public int DuplicatePoints { get; }
}

public class ShopManager : MonoBehaviour
{
    [SerializeField] private SkinCatalog skinCatalog;
    [SerializeField] private List<WardrobeBox> availableBoxes = new();
    [SerializeField] private PlayFabEconomyService economyService;
    public IReadOnlyList<WardrobeBox> AvailableBoxes => availableBoxes;
    public SkinCatalog Catalog => skinCatalog;
    public event Action<WardrobeBoxReward> OnRewardGranted;
    private bool purchasePending;
    private string pendingRequestId;
    private string PendingKey => PlayFabAuthManager.Instance != null && PlayFabAuthManager.Instance.IsLoggedIn && !PlayFabAuthManager.Instance.IsGuest
        ? "Capivarada.PendingPurchase." + PlayFabAuthManager.Instance.PlayFabId
        : null;

    private void OnEnable()
    {
        if (PlayFabAuthManager.Instance != null) PlayFabAuthManager.Instance.OnLoginSuccess += BeginAuthenticatedSession;
    }
    private void Start()
    {
        if (economyService == null && WardrobeManager.Instance != null)
            economyService = WardrobeManager.Instance.EconomyService;
        BeginAuthenticatedSession();
    }

    public void Configure(SkinCatalog catalog, IEnumerable<WardrobeBox> boxes)
    {
        skinCatalog = catalog;
        availableBoxes = boxes == null ? new List<WardrobeBox>() : new List<WardrobeBox>(boxes);
        if (economyService == null && WardrobeManager.Instance != null)
            economyService = WardrobeManager.Instance.EconomyService;
    }
    private void OnDisable()
    {
        if (PlayFabAuthManager.Instance != null) PlayFabAuthManager.Instance.OnLoginSuccess -= BeginAuthenticatedSession;
    }
    private void BeginAuthenticatedSession()
    {
        if (string.IsNullOrEmpty(PendingKey)) return;
        pendingRequestId = PlayerPrefs.GetString(PendingKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(pendingRequestId)) RecoverPendingPurchase();
    }
    public void PurchaseBox(WardrobeBox box)
    {
        if (purchasePending) { Debug.LogWarning("[Shop] Uma compra já está sendo processada."); return; }
        if (economyService == null) { Debug.LogError("[Shop] PlayFabEconomyService não configurado.", this); return; }
        if (box == null || !availableBoxes.Contains(box) || !box.IsAvailable) { Debug.LogWarning("[Shop] Caixa inválida ou indisponível."); return; }
        if (!string.IsNullOrWhiteSpace(pendingRequestId)) { RecoverPendingPurchase(); return; }
        if (!box.Validate(skinCatalog, out string error)) { Debug.LogWarning("[Shop] " + error); return; }
        if (string.IsNullOrEmpty(PendingKey)) { Debug.LogWarning("[Shop] Faça login com uma conta não visitante para comprar caixas."); return; }
        pendingRequestId = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString(PendingKey, pendingRequestId); PlayerPrefs.Save(); purchasePending = true;
        economyService.PurchaseBox(box.BoxId, pendingRequestId, HandleConfirmedPurchase, error =>
        {
            purchasePending = false;
            Debug.LogError("[Shop] PurchaseSkinBox foi recusada pelo PlayFab: " + error);
            // A consulta mantém a proteção contra uma resposta de rede perdida.
            // When it returns NotFound, RecoverPendingPurchase clears the local key.
            RecoverPendingPurchase();
        });
    }
    private void RecoverPendingPurchase()
    {
        if (purchasePending || string.IsNullOrWhiteSpace(pendingRequestId) || economyService == null) return;
        purchasePending = true;
        economyService.ResolvePurchase(pendingRequestId, (state, result, error) =>
        {
            purchasePending = false;
            if (state == PurchaseResolution.Confirmed) HandleConfirmedPurchase(result);
            else if (state == PurchaseResolution.NotFound)
            {
                // CloudScript never accepted this request. Keeping its ID locally
                // blocked every later purchase even though no charge was made.
                ClearPendingPurchase();
                Debug.LogWarning("[Shop] A compra pendente não foi encontrada no servidor; a tentativa local foi liberada.");
            }
            else Debug.LogWarning($"[Shop] Ainda não foi possível confirmar a compra anterior ({state}). Nenhuma nova cobrança será iniciada. {error}");
        });
    }
    private void HandleConfirmedPurchase(BoxPurchaseResult result)
    {
        purchasePending = false;
        ClearPendingPurchase();
        Debug.Log($"[Shop] Compra confirmada pelo servidor: skin={result.SkinId}, raridade={result.Rarity}, duplicata={result.IsDuplicate}, conversão={result.DuplicatePoints}.");
        WardrobeSkin skin = skinCatalog != null ? skinCatalog.FindById(result.SkinId) : null;
        if (skin == null) { Debug.LogWarning("[Shop] O servidor retornou uma skin ausente no catálogo."); return; }
        if (!result.IsDuplicate) WardrobeManager.Instance?.ApplyConfirmedSkin(skin);
        if (!Enum.TryParse(result.Rarity, out Rarity rarity)) rarity = skin.Rarity;
        OnRewardGranted?.Invoke(new WardrobeBoxReward(rarity, skin, result.IsDuplicate, result.DuplicatePoints));
    }

    private void ClearPendingPurchase()
    {
        if (!string.IsNullOrEmpty(PendingKey))
        {
            PlayerPrefs.DeleteKey(PendingKey);
            PlayerPrefs.Save();
        }
        pendingRequestId = null;
    }
}
