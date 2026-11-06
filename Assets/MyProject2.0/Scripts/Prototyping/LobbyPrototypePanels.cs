using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Temporary code-built panels for validating Friends and cosmetic color persistence. Delete after final UI prefabs are wired.</summary>
public static class LobbyPrototypePanels
{
    private const string LobbyScene = "Lobby_Scene";
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        if (subscribed) return;
        subscribed = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        CreateFor(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode _) => CreateFor(scene);
    private static void CreateFor(Scene scene)
    {
        if (!string.Equals(scene.name, LobbyScene, StringComparison.Ordinal) || UnityEngine.Object.FindFirstObjectByType<FriendsPrototypePanel>() != null) return;
        CreatePanel<FriendsPrototypePanel>("FriendsPrototypePanel", new Vector2(20f, -90f), new Vector2(500f, 720f));
        CreatePanel<WardrobePrototypePanel>("WardrobePrototypePanel", new Vector2(540f, -90f), new Vector2(500f, 720f));
    }

    private static T CreatePanel<T>(string name, Vector2 position, Vector2 size) where T : Component
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(T));
        Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f);
        RectTransform rect = root.GetComponent<RectTransform>(); rect.anchorMin = Vector2.up; rect.anchorMax = Vector2.up; rect.pivot = Vector2.up; rect.anchoredPosition = position; rect.sizeDelta = size;
        return root.GetComponent<T>();
    }
}

public abstract class PrototypePanelBase : MonoBehaviour
{
    protected readonly List<GameObject> DynamicRows = new();
    protected TMP_FontAsset Font => TMP_Settings.defaultFontAsset;

    protected RectTransform Box(Transform parent, string name, Color color, float minHeight = 0f)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)); item.transform.SetParent(parent, false);
        item.GetComponent<Image>().color = color; item.GetComponent<LayoutElement>().minHeight = minHeight;
        return item.GetComponent<RectTransform>();
    }

    protected void Vertical(Transform target, int padding = 12, int spacing = 8)
    {
        VerticalLayoutGroup layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding); layout.spacing = spacing;
        layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
    }

    protected TMP_Text Text(Transform parent, string value, int size = 22, Color? color = null, float minHeight = 0f)
    {
        GameObject item = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement)); item.transform.SetParent(parent, false);
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>(); text.font = Font; text.text = value; text.fontSize = size; text.color = color ?? new Color(.95f, .92f, .88f); text.enableWordWrapping = true;
        item.GetComponent<LayoutElement>().minHeight = minHeight > 0f ? minHeight : size + 12f; return text;
    }

    protected Button Button(Transform parent, string label, Action clicked, Color? color = null, float minHeight = 46f)
    {
        RectTransform box = Box(parent, "Button_" + label, color ?? new Color(.95f, .28f, .10f), minHeight); Button button = box.gameObject.AddComponent<Button>();
        if (clicked != null) button.onClick.AddListener(() => clicked()); TMP_Text text = Text(box, label, 18, Color.white, minHeight - 8f); text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    protected TMP_InputField Input(Transform parent, string placeholder)
    {
        RectTransform root = Box(parent, "Input", new Color(.08f, .075f, .08f), 48f); TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic = root.GetComponent<Image>();
        RectTransform viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>(); viewport.SetParent(root, false); viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = new Vector2(12f, 4f); viewport.offsetMax = new Vector2(-12f, -4f);
        TextMeshProUGUI text = Text(viewport, string.Empty, 18, Color.white).GetComponent<TextMeshProUGUI>(); text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
        TextMeshProUGUI hint = Text(viewport, placeholder, 18, new Color(.55f, .52f, .55f)).GetComponent<TextMeshProUGUI>(); hint.rectTransform.anchorMin = Vector2.zero; hint.rectTransform.anchorMax = Vector2.one; hint.rectTransform.offsetMin = Vector2.zero; hint.rectTransform.offsetMax = Vector2.zero;
        input.textViewport = viewport; input.textComponent = text; input.placeholder = hint; return input;
    }

    protected void ClearRows() { foreach (GameObject row in DynamicRows) if (row != null) Destroy(row); DynamicRows.Clear(); }
}

public sealed class FriendsPrototypePanel : PrototypePanelBase
{
    private FriendsService friends; private Transform list; private TMP_InputField input; private TMP_Text feedback; private Button friendsTab; private Button requestsTab; private bool showingRequests;
    private void Start()
    {
        Build(); friends = ServiceHub.Instance != null ? ServiceHub.Instance.Friends : FriendsService.Instance;
        if (friends == null) { SetFeedback("FriendsService não foi encontrado em Services."); return; }
        friends.Changed += Refresh; friends.Failed += SetFeedback; friends.Refresh();
    }
    private void OnDestroy() { if (friends != null) { friends.Changed -= Refresh; friends.Failed -= SetFeedback; } }
    private void Build()
    {
        RectTransform panel = Box(transform, "Panel", new Color(.12f, .105f, .12f, .97f)); panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = Vector2.zero; panel.offsetMax = Vector2.zero; Vertical(panel, 18, 10);
        Text(panel, "AMIGOS · TESTE", 30, new Color(1f, .38f, .16f), 42f);
        RectTransform tabs = Box(panel, "Tabs", Color.clear, 46f); tabs.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 8;
        friendsTab = Button(tabs, "Amigos", () => { showingRequests = false; Refresh(); }, new Color(.28f, .24f, .26f)); requestsTab = Button(tabs, "Solicitações", () => { showingRequests = true; Refresh(); }, new Color(.28f, .24f, .26f));
        input = Input(panel, "Apelido ou PlayFab ID"); Button(panel, "Enviar solicitação", Send); feedback = Text(panel, "Conectando ao serviço...", 15, new Color(.8f, .75f, .7f), 26f);
        list = Box(panel, "List", new Color(.07f, .06f, .07f)).transform; Vertical(list, 10, 6);
    }
    private void Send() { if (friends == null || input == null) return; friends.Send(input.text, () => { input.text = string.Empty; SetFeedback("Solicitação enviada."); }); }
    private void Refresh()
    {
        if (friends == null || list == null) return; ClearRows(); friendsTab.GetComponent<Image>().color = showingRequests ? new Color(.28f, .24f, .26f) : new Color(.95f, .28f, .10f); requestsTab.GetComponent<Image>().color = showingRequests ? new Color(.95f, .28f, .10f) : new Color(.28f, .24f, .26f);
        if (!showingRequests) { SetFeedback($"{friends.Friends.Count} amigo(s)."); foreach (FriendsService.FriendSummary item in friends.Friends) AddFriend(item); return; }
        SetFeedback($"{friends.Incoming.Count} recebida(s), {friends.Outgoing.Count} enviada(s)."); foreach (FriendsService.FriendRequestSummary item in friends.Incoming) AddRequest(item, true); foreach (FriendsService.FriendRequestSummary item in friends.Outgoing) AddRequest(item, false);
    }
    private void AddFriend(FriendsService.FriendSummary item) { RectTransform row = Box(list, "Friend", new Color(.16f, .14f, .16f), 70f); row.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 8; Text(row, $"{item.nickname}\n{item.presence}", 17, Color.white, 62f); Button(row, item.canInvite ? "Convidar" : "Perfil", null, new Color(.28f, .24f, .26f), 52f); DynamicRows.Add(row.gameObject); }
    private void AddRequest(FriendsService.FriendRequestSummary item, bool incoming)
    {
        RectTransform row = Box(list, "Request", new Color(.16f, .14f, .16f), 76f); row.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 8; Text(row, $"{item.nickname}\nNível {item.level:00}", 17, Color.white, 66f);
        if (incoming) { Button(row, "Aceitar", () => friends.Accept(item.requestId), null, 54f); Button(row, "Recusar", () => friends.Reject(item.requestId), new Color(.28f, .24f, .26f), 54f); } else Button(row, "Cancelar", () => friends.Cancel(item.requestId), new Color(.28f, .24f, .26f), 54f); DynamicRows.Add(row.gameObject);
    }
    private void SetFeedback(string message) { if (feedback != null) feedback.text = message; }
}

public sealed class WardrobePrototypePanel : PrototypePanelBase
{
    private PlayerProfileService profile; private CapybaraAppearanceApplier preview; private TMP_Text feedback; private readonly Dictionary<CapybaraColorSlot, CapybaraColorSelection> colors = new();
    private void Start() { Build(); profile = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance; preview = FindFirstObjectByType<CapybaraAppearanceApplier>(); if (profile == null) { SetFeedback("PlayerProfileService não foi encontrado."); return; } profile.Changed += Load; Load(profile.Current); }
    private void OnDestroy() { if (profile != null) profile.Changed -= Load; }
    private void Build()
    {
        RectTransform panel = Box(transform, "Panel", new Color(.12f, .105f, .12f, .97f)); panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = Vector2.zero; panel.offsetMax = Vector2.zero; Vertical(panel, 18, 8);
        Text(panel, "VESTIÁRIO · TESTE", 30, new Color(1f, .38f, .16f), 42f); Text(panel, "Cores livres — o brilho original do material é preservado.", 14, new Color(.8f, .75f, .7f), 34f);
        ColorControls(panel, CapybaraColorSlot.Body, "Corpo"); ColorControls(panel, CapybaraColorSlot.Snout, "Focinho"); ColorControls(panel, CapybaraColorSlot.Ears, "Orelhas"); ColorControls(panel, CapybaraColorSlot.Hooves, "Cascos"); Button(panel, "Salvar personalização", Save); feedback = Text(panel, "Carregando perfil...", 15, new Color(.8f, .75f, .7f), 30f);
    }
    private void ColorControls(Transform parent, CapybaraColorSlot slot, string name)
    {
        RectTransform group = Box(parent, name, new Color(.08f, .075f, .08f)); Vertical(group, 7, 2); Text(group, name, 16, Color.white, 22f);
        Slider(group, "Matiz", .1f, value => Set(slot, value, null)); Slider(group, "Saturação", .5f, value => Set(slot, null, value));
    }
    private void Slider(Transform parent, string name, float initial, Action<float> changed)
    {
        RectTransform row = Box(parent, name, Color.clear, 32f); row.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 8; TMP_Text label = Text(row, name, 14, new Color(.9f, .85f, .8f), 24f); LayoutElement labelLayout = label.gameObject.GetComponent<LayoutElement>(); labelLayout.preferredWidth = 100f;
        GameObject control = new GameObject("Slider", typeof(RectTransform), typeof(UnityEngine.UI.Slider), typeof(LayoutElement)); control.transform.SetParent(row, false); UnityEngine.UI.Slider slider = control.GetComponent<UnityEngine.UI.Slider>(); slider.minValue = 0f; slider.maxValue = 1f; slider.value = initial; slider.onValueChanged.AddListener(value => { label.text = $"{name}: {Mathf.RoundToInt(value * 100f)}"; changed(value); });
    }
    private void Load(PlayerProfileData data)
    {
        if (data == null) return; PlayerCosmeticLoadout loadout = data.cosmetics ??= new PlayerCosmeticLoadout(); foreach (CapybaraColorSlot slot in (CapybaraColorSlot[])Enum.GetValues(typeof(CapybaraColorSlot))) colors[slot] = loadout.Get(slot); Apply(); SetFeedback(preview == null ? "Cores salvas no perfil. Adicione CapybaraAppearanceApplier ao preview 3D para visualizá-las." : "Prévia 3D conectada.");
    }
    private void Set(CapybaraColorSlot slot, float? hue, float? saturation) { colors.TryGetValue(slot, out CapybaraColorSelection current); colors[slot] = new CapybaraColorSelection(hue ?? current.Hue, saturation ?? current.Saturation); Apply(); }
    private void Apply() { if (profile == null) return; PlayerCosmeticLoadout loadout = profile.Current.cosmetics ??= new PlayerCosmeticLoadout(); foreach (KeyValuePair<CapybaraColorSlot, CapybaraColorSelection> item in colors) loadout.Set(item.Key, item.Value); preview?.Apply(loadout, null); }
    private void Save() { if (profile == null) return; Apply(); profile.SaveCosmetics(profile.Current.cosmetics, () => SetFeedback("Personalização salva.")); }
    private void SetFeedback(string message) { if (feedback != null) feedback.text = message; }
}
