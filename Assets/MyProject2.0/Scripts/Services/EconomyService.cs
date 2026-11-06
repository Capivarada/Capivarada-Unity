using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;

public sealed class EconomyService : MonoBehaviour
{
        public readonly struct BoxPurchaseResult
        {
            public readonly string SkinId;
            public readonly string RarityId;
            public readonly bool IsDuplicate;
            public readonly int DuplicatePoints;

            public BoxPurchaseResult(string skinId, string rarityId, bool isDuplicate, int duplicatePoints)
            {
                SkinId = skinId;
                RarityId = rarityId;
                IsDuplicate = isDuplicate;
                DuplicatePoints = duplicatePoints;
            }
        }
        public static EconomyService Instance { get; private set; }
        [SerializeField] private PlayFabAuthManager authentication;
        public event Action<int> PointsChanged;
        public event Action<IReadOnlyList<string>, string> InventoryChanged;
        public event Action<string> Failed;
        private readonly List<string> ownedSkinIds = new();
        public int Points { get; private set; }
        public IReadOnlyList<string> OwnedSkinIds => ownedSkinIds;
        public string EquippedSkinId { get; private set; }
        private void Awake() { if (Instance == null) Instance = this; else if (Instance != this) Destroy(this); }
        public void Refresh() => Execute("GetPlayerEconomy", null, result => { Points = Int(result, "points"); EquippedSkinId = Text(result, "equippedSkinId"); ReplaceInventory(result); Publish(); });
        public void EquipSkin(string skinId, Action completed, Action<string> failed) => Execute("EquipSkin", new { skinId }, result => { Points = Int(result, "points"); EquippedSkinId = Text(result, "equippedSkinId"); ReplaceInventory(result); Publish(); completed?.Invoke(); }, failed);
        public void PurchaseBox(string boxId, string requestId, Action<BoxPurchaseResult> completed, Action<string> failed) => Execute("PurchaseSkinBox", new { boxId, requestId }, result =>
        {
            Points = Int(result, "points");
            ReplaceInventory(result);
            Publish();
            completed?.Invoke(new BoxPurchaseResult(
                Text(result, "skinId"),
                Text(result, "rarity"),
                Bool(result, "isDuplicate"),
                Int(result, "duplicatePoints")));
        }, failed);
        private void Execute(string function, object parameter, Action<JsonObject> completed, Action<string> failed = null)
        {
            if (authentication == null || !authentication.IsLoggedIn || authentication.IsGuest) { Report("É necessário entrar com uma conta para acessar a economia.", failed); return; }
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = function, FunctionParameter = parameter, GeneratePlayStreamEvent = true }, response => { JsonObject result = response.FunctionResult as JsonObject; if (response.Error != null || result == null || !Ok(result)) { string detail = response.Error != null ? $"{response.Error.Error}: {response.Error.Message}" : result != null ? Text(result, "error") : "Resposta inválida do servidor."; Debug.LogError($"[Economy/{function}] {detail}", this); Report(detail, failed); return; } completed?.Invoke(result); }, error => Report(error.ErrorMessage, failed));
        }
        private void ReplaceInventory(JsonObject result) { if (!result.TryGetValue("ownedSkinIds", out object raw) || raw is not IEnumerable values) return; ownedSkinIds.Clear(); foreach (object value in values) if (value != null && !ownedSkinIds.Contains(value.ToString())) ownedSkinIds.Add(value.ToString()); }
        private void Publish() { PointsChanged?.Invoke(Points); InventoryChanged?.Invoke(ownedSkinIds, EquippedSkinId); }
        private void Report(string message, Action<string> callback) { Failed?.Invoke(message); callback?.Invoke(message); }
        private static string Text(JsonObject result, string key) => result.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
        private static int Int(JsonObject result, string key) { try { return result.TryGetValue(key, out object value) ? Convert.ToInt32(value) : 0; } catch { return 0; } }
        private static bool Bool(JsonObject result, string key) { try { return result.TryGetValue(key, out object value) && Convert.ToBoolean(value); } catch { return false; } }
        private static bool Ok(JsonObject result) { try { return result.TryGetValue("success", out object value) && Convert.ToBoolean(value); } catch { return false; } }
}
