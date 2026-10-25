using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;

public enum PurchaseResolution { Confirmed, NotFound, TransportError, ServerError }

/// <summary>Authoritative economy gateway. Local state is a projection only, never a balance source.</summary>
public sealed class PlayFabEconomyService : MonoBehaviour
{
    public event Action<int> OnBalanceChanged;
    public event Action<IReadOnlyList<string>, string> OnInventoryLoaded;
    public int PointsBalance { get; private set; }
    public IReadOnlyList<string> OwnedSkinIds => ownedSkinIds;
    public string EquippedSkinId { get; private set; }

    private readonly List<string> ownedSkinIds = new();
    private bool mutationInFlight;
    private int stateRevision;

    public void Refresh(Action<string> onError = null)
    {
        // Capture the revision at dispatch time. A response that arrives after a
        // mutation must never replace the newer local projection.
        int requestedRevision = stateRevision;
        Execute("GetPlayerEconomy", null, false, result => ApplyEconomy(result, requestedRevision), onError);
    }

    public void EquipSkin(string skinId, Action onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(skinId)) { onError?.Invoke("Skin inválida."); return; }
        Execute("EquipSkin", new { skinId }, true, result =>
        {
            EquippedSkinId = GetString(result, "equippedSkinId");
            ReplaceOwnedSkinIdsIfPresent(result);
            PointsBalance = GetInt(result, "points");
            stateRevision++;
            OnBalanceChanged?.Invoke(PointsBalance);
            OnInventoryLoaded?.Invoke(ownedSkinIds, EquippedSkinId);
            onSuccess?.Invoke();
        }, onError);
    }

    public void PurchaseBox(string boxId, string requestId, Action<BoxPurchaseResult> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(boxId) || string.IsNullOrWhiteSpace(requestId)) { onError?.Invoke("Pedido de compra inválido."); return; }
        Execute("PurchaseSkinBox", new { boxId, requestId }, true, result => onSuccess?.Invoke(ApplyPurchase(result)), onError);
    }

    /// <summary>Reconciles a prior request ID. Never starts a new purchase.</summary>
    public void ResolvePurchase(string requestId, Action<PurchaseResolution, BoxPurchaseResult, string> completed)
    {
        if (string.IsNullOrWhiteSpace(requestId)) { completed?.Invoke(PurchaseResolution.NotFound, null, null); return; }
        if (!CanUseEconomy(out string authError)) { completed?.Invoke(PurchaseResolution.ServerError, null, authError); return; }
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = "GetPurchaseResult", FunctionParameter = new { requestId }, GeneratePlayStreamEvent = true }, response =>
        {
            if (response.Error != null) { completed?.Invoke(PurchaseResolution.ServerError, null, response.Error.Message); return; }
            if (response.FunctionResult is not JsonObject result || !GetBool(result, "success")) { completed?.Invoke(PurchaseResolution.ServerError, null, "Resposta inválida do CloudScript."); return; }
            if (!GetBool(result, "found")) { completed?.Invoke(PurchaseResolution.NotFound, null, null); return; }
            completed?.Invoke(PurchaseResolution.Confirmed, ApplyPurchase(result), null);
        }, error => completed?.Invoke(PurchaseResolution.TransportError, null, error.ErrorMessage));
    }

    private BoxPurchaseResult ApplyPurchase(JsonObject result)
    {
        var purchase = new BoxPurchaseResult
        {
            RequestId = GetString(result, "requestId"), SkinId = GetString(result, "skinId"),
            Rarity = GetString(result, "rarity"), IsDuplicate = GetBool(result, "isDuplicate"),
            DuplicatePoints = GetInt(result, "duplicatePoints"), PointsBalance = GetInt(result, "points")
        };
        PointsBalance = purchase.PointsBalance;
        if (!purchase.IsDuplicate && !string.IsNullOrWhiteSpace(purchase.SkinId) && !ownedSkinIds.Contains(purchase.SkinId)) ownedSkinIds.Add(purchase.SkinId);
        stateRevision++;
        OnBalanceChanged?.Invoke(PointsBalance);
        // A confirmed purchase changes the authoritative inventory projection.
        // Notify the wardrobe immediately instead of waiting for a later refresh.
        OnInventoryLoaded?.Invoke(ownedSkinIds, EquippedSkinId);
        return purchase;
    }

    private void ApplyEconomy(JsonObject result, int revision)
    {
        // A read that started before a confirmed mutation must not overwrite the newer projection.
        if (revision != stateRevision) return;
        PointsBalance = GetInt(result, "points");
        EquippedSkinId = GetString(result, "equippedSkinId");
        ReplaceOwnedSkinIdsIfPresent(result);
        stateRevision++;
        OnBalanceChanged?.Invoke(PointsBalance);
        OnInventoryLoaded?.Invoke(ownedSkinIds, EquippedSkinId);
    }

    private void Execute(string functionName, object parameter, bool mutating, Action<JsonObject> onSuccess, Action<string> onError)
    {
        if (mutating && mutationInFlight) { onError?.Invoke("Outra operação de economia ainda está pendente."); return; }
        if (!CanUseEconomy(out string authError)) { onError?.Invoke(authError); return; }
        if (mutating) mutationInFlight = true;
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = functionName, FunctionParameter = parameter, GeneratePlayStreamEvent = true }, response =>
        {
            if (mutating) mutationInFlight = false;
            if (response.Error != null) { onError?.Invoke(response.Error.Message); return; }
            if (response.FunctionResult is JsonObject data && GetBool(data, "success")) onSuccess?.Invoke(data);
            else onError?.Invoke("Resposta inválida do CloudScript.");
        }, error => { if (mutating) mutationInFlight = false; onError?.Invoke(error.ErrorMessage); });
    }

    private static bool CanUseEconomy(out string error)
    {
        if (PlayFabAuthManager.Instance == null || !PlayFabAuthManager.Instance.IsLoggedIn || PlayFabAuthManager.Instance.IsGuest)
        { error = "É necessário estar autenticado para usar a economia."; return false; }
        error = null;
        return true;
    }

    private static IEnumerable GetArray(JsonObject value, string key) => value.TryGetValue(key, out object raw) && raw is IEnumerable items ? items : Array.Empty<object>();
    private void ReplaceOwnedSkinIdsIfPresent(JsonObject result)
    {
        if (result == null || !result.TryGetValue("ownedSkinIds", out object raw) || raw is not IEnumerable values) return;
        ownedSkinIds.Clear();
        foreach (object value in values)
            if (value != null && !ownedSkinIds.Contains(value.ToString())) ownedSkinIds.Add(value.ToString());
    }
    private static string GetString(JsonObject value, string key) => value.TryGetValue(key, out object raw) && raw != null ? raw.ToString() : string.Empty;
    private static int GetInt(JsonObject value, string key) { try { return value.TryGetValue(key, out object raw) && raw != null ? Convert.ToInt32(raw) : 0; } catch { return 0; } }
    private static bool GetBool(JsonObject value, string key) { try { return value.TryGetValue(key, out object raw) && raw != null && Convert.ToBoolean(raw); } catch { return false; } }
}

public sealed class BoxPurchaseResult
{
    public string RequestId;
    public string SkinId;
    public string Rarity;
    public bool IsDuplicate;
    public int DuplicatePoints;
    public int PointsBalance;
}
