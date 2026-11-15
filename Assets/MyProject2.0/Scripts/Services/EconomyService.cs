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
        public event Action<string> Failed;
        public int Points { get; private set; }
        private void Awake() { if (Instance == null) Instance = this; else if (Instance != this) Destroy(this); }
        public void Refresh() => Report("Economia legada desativada. Use EconomyV2Service.", null);
        public void EquipSkin(string skinId, Action completed, Action<string> failed) => Report("Skins legadas não são mais suportadas.", failed);
        public void PurchaseBox(string boxId, string requestId, Action<BoxPurchaseResult> completed, Action<string> failed) => Execute("PurchaseSkinBox", new { boxId, requestId }, result =>
        {
            completed?.Invoke(default);
        }, failed);
        private void Execute(string function, object parameter, Action<JsonObject> completed, Action<string> failed = null)
        {
            // The old skin/points economy was CloudScript based. Economy V2
            // owns inventory now; retain this serialized component only as a
            // safe compatibility facade for older scenes.
            Report("Economia legada desativada. Use EconomyV2Service.", failed);
        }
        private void Publish() { PointsChanged?.Invoke(Points); }
        private void Report(string message, Action<string> callback) { Failed?.Invoke(message); callback?.Invoke(message); }
        private static string Text(JsonObject result, string key) => result.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
        private static int Int(JsonObject result, string key) { try { return result.TryGetValue(key, out object value) ? Convert.ToInt32(value) : 0; } catch { return 0; } }
        private static bool Bool(JsonObject result, string key) { try { return result.TryGetValue(key, out object value) && Convert.ToBoolean(value); } catch { return false; } }
        private static bool Ok(JsonObject result) { try { return result.TryGetValue("success", out object value) && Convert.ToBoolean(value); } catch { return false; } }
}
