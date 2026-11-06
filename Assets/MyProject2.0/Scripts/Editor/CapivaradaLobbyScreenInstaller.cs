#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time authoring utility that installs the definitive Friends and Wardrobe screens in Lobby_Scene.
/// It intentionally touches only its generated children and the main PanelNavigator entry list.
/// </summary>
public static class CapivaradaLobbyScreenInstaller
{
    private const string ScenePath = "Assets/MyProject2.0/Scenes/Lobby_Scene.unity";
    private const string FriendPrefabFolder = "Assets/MyProject2.0/Prefabs/Friends";
    private const string WardrobePrefabFolder = "Assets/MyProject2.0/Prefabs/Wardrobe";
    private static readonly Color Background = Hex("231F20");
    private static readonly Color Panel = Hex("302C2E");
    private static readonly Color Black = Hex("121112");
    private static readonly Color Border = Hex("554C50");
    private static readonly Color Orange = Hex("F2622A");
    private static readonly Color Cream = Hex("FFF7ED");
    private static readonly Color Muted = Hex("BDB4B5");
    private static readonly Color Online = Hex("7DE591");

    [MenuItem("Capivarada/UI/Install Friends and Wardrobe Screens")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureEventSystem();
        EnsureFolder("Assets/MyProject2.0/Prefabs");
        EnsureFolder(FriendPrefabFolder);
        EnsureFolder(WardrobePrefabFolder);

        PanelNavigatorController navigator = FindSceneObject<PanelNavigatorController>();
        GameObject wardrobeRoot = FindSceneObject("WardrobeUI");
        GameObject friendsShortcut = FindSceneObject("FriendsUI");
        if (navigator == null || wardrobeRoot == null)
        {
            Debug.LogError("Não foi possível localizar PanelNavigatorController ou WardrobeUI em Lobby_Scene.");
            return;
        }

        AvatarCatalog avatars = AssetDatabase.LoadAssetAtPath<AvatarCatalog>("Assets/MyProject2.0/ScriptableObjects/Profile/AvatarCatalog.asset");
        FriendRowController friendRow = CreateFriendRowPrefab(avatars);
        FriendRequestRowController incomingRow = CreateRequestRowPrefab(avatars, true);
        FriendRequestRowController outgoingRow = CreateRequestRowPrefab(avatars, false);
        CosmeticChoiceCard choiceCard = CreateCosmeticChoicePrefab();

        Transform panelParent = wardrobeRoot.transform.parent;
        GameObject friendsPanel = FindChild(panelParent, "FriendsPanel");
        if (friendsPanel != null) UnityEngine.Object.DestroyImmediate(friendsPanel);
        friendsPanel = BuildFriendsPanel(panelParent, navigator, friendRow, incomingRow, outgoingRow);

        BuildWardrobePanel(wardrobeRoot, navigator, choiceCard);
        AddNavigatorEntry(navigator, "Friends", friendsPanel);
        WireFriendsShortcut(friendsShortcut, navigator);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FriendsPanel, WardrobeUI e os prefabs sociais foram instalados em Lobby_Scene.");
    }

    private static GameObject BuildFriendsPanel(Transform parent, PanelNavigatorController navigator, FriendRowController friendPrefab, FriendRequestRowController incomingPrefab, FriendRequestRowController outgoingPrefab)
    {
        GameObject root = Ui("FriendsPanel", parent, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        root.AddComponent<CanvasRenderer>();
        Image backdrop = root.AddComponent<Image>(); backdrop.color = new Color(Background.r, Background.g, Background.b, .98f); backdrop.raycastTarget = false;
        FriendsController controller = root.AddComponent<FriendsController>();

        Button back = Button(root.transform, "Button.Back", "VOLTAR", SecondarySprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -74f), new Vector2(130f, 56f), 18);
        AddBack(back.gameObject, navigator);
        Text(root.transform, "Title", "O BREJO É MELHOR JUNTO.", 52, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -155f), new Vector2(1100f, 70f));
        Badge(root.transform, "Server", "BR · São Paulo", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-145f, -154f), new Vector2(180f, 42f));

        RectTransform left = PanelBox(root.transform, "FriendsArea", new Vector2(0f, 0f), new Vector2(.66f, 1f), new Vector2(44f, 52f), new Vector2(-16f, -220f));
        RectTransform rightTop = PanelBox(root.transform, "RequestsSummary", new Vector2(.66f, .45f), new Vector2(1f, 1f), new Vector2(16f, 28f), new Vector2(-44f, -220f));
        RectTransform rightBottom = PanelBox(root.transform, "AddFriend", new Vector2(.66f, 0f), new Vector2(1f, .43f), new Vector2(16f, 52f), new Vector2(-44f, -28f));

        Button friendsTab = Button(left, "Tab.Friends", "AMIGOS", TabSelectedSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -28f), new Vector2(160f, 60f), 20);
        Button requestsTab = Button(left, "Tab.Requests", "SOLICITAÇÕES", TabSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(198f, -28f), new Vector2(220f, 60f), 20);
        TMP_Text friendsTabText = Text(friendsTab.transform, "Label", "Amigos · 0", 20, Cream, FontStyles.Bold, Stretch(), Vector2.zero, Vector2.zero, Vector2.zero);
        TMP_Text requestsTabText = Text(requestsTab.transform, "Label", "Solicitações · 0", 20, Cream, FontStyles.Bold, Stretch(), Vector2.zero, Vector2.zero, Vector2.zero);
        Text(left, "SearchLabel", "PESQUISAR AMIGOS", 15, Muted, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -105f), new Vector2(360f, 30f));

        GameObject friendsView = Ui("FriendsView", left, new Vector2(0f, 0f), Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        GameObject requestsView = Ui("RequestsView", left, new Vector2(0f, 0f), Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        friendsView.SetActive(true); requestsView.SetActive(false);
        RectTransform friendContent = ScrollContent(friendsView.transform, "FriendsScroll", new Vector2(22f, 20f), new Vector2(-22f, -128f), false);
        GameObject receivedRequestsView = Ui("ReceivedRequests", requestsView.transform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        GameObject sentRequestsView = Ui("SentRequests", requestsView.transform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        Button receivedTab = Button(receivedRequestsView.transform, "Tab.Received", "RECEBIDAS", TabSelectedSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -104f), new Vector2(164f, 48f), 16);
        Button sentTab = Button(receivedRequestsView.transform, "Tab.Sent", "ENVIADAS", TabSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(200f, -104f), new Vector2(150f, 48f), 16);
        Button sentReceivedTab = Button(sentRequestsView.transform, "Tab.Received", "RECEBIDAS", TabSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -104f), new Vector2(164f, 48f), 16);
        Button sentSentTab = Button(sentRequestsView.transform, "Tab.Sent", "ENVIADAS", TabSelectedSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(200f, -104f), new Vector2(150f, 48f), 16);
        RectTransform receivedContent = ScrollContent(receivedRequestsView.transform, "ReceivedScroll", new Vector2(22f, 20f), new Vector2(-22f, -184f), false);
        RectTransform sentContent = ScrollContent(sentRequestsView.transform, "SentScroll", new Vector2(22f, 20f), new Vector2(-22f, -184f), false);
        sentRequestsView.SetActive(false);
        TMP_Text emptyFriends = Text(friendsView.transform, "EmptyFriends", "Ainda não há amigos na lista. Adicione alguém usando o painel ao lado.", 19, Muted, FontStyles.Italic, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -22f), new Vector2(550f, 80f));
        emptyFriends.alignment = TextAlignmentOptions.Center;
        TMP_Text emptyRequests = Text(requestsView.transform, "EmptyRequests", "Nenhuma solicitação pendente.", 19, Muted, FontStyles.Italic, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -22f), new Vector2(550f, 80f));
        emptyRequests.alignment = TextAlignmentOptions.Center;

        Badge(rightTop, "RequestBadge", "SOLICITAÇÕES", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -34f), new Vector2(180f, 40f));
        Text(rightTop, "Heading", "TEM GENTE CHEGANDO.", 30, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -90f), new Vector2(500f, 54f));
        Text(rightTop, "Description", "Veja quem quer entrar na sua lista.", 19, Muted, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -145f), new Vector2(500f, 48f));
        Button openRequests = Button(rightTop, "Button.OpenRequests", "VER SOLICITAÇÕES", PrimarySprite(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(30f, 28f), new Vector2(-30f, 28f), 20);

        Text(rightBottom, "Heading", "ADICIONAR AMIGO", 30, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -46f), new Vector2(450f, 46f));
        Text(rightBottom, "InputLabel", "APELIDO OU ID", 15, Muted, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -102f), new Vector2(300f, 28f));
        TMP_InputField addInput = Input(rightBottom, "AddFriendInput", "Digite um apelido ou ID", new Vector2(30f, -164f), new Vector2(-30f, -112f));
        Button send = Button(rightBottom, "Button.SendRequest", "ENVIAR SOLICITAÇÃO", PrimarySprite(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(30f, 30f), new Vector2(-30f, 88f), 20);
        TMP_Text feedback = Text(rightBottom, "Feedback", string.Empty, 15, Muted, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(30f, 6f), new Vector2(-30f, 28f));

        Assign(controller, "friendRowPrefab", friendPrefab); Assign(controller, "receivedRequestPrefab", incomingPrefab); Assign(controller, "sentRequestPrefab", outgoingPrefab);
        Assign(controller, "friendsContent", friendContent); Assign(controller, "receivedContent", receivedContent); Assign(controller, "sentContent", sentContent);
        Assign(controller, "addFriendInput", addInput); Assign(controller, "friendsTabText", friendsTabText); Assign(controller, "requestsTabText", requestsTabText); Assign(controller, "feedbackText", feedback);
        Assign(controller, "friendsView", friendsView); Assign(controller, "requestsView", requestsView); Assign(controller, "receivedRequestsView", receivedRequestsView); Assign(controller, "sentRequestsView", sentRequestsView); Assign(controller, "requestNotification", rightTop.gameObject); Assign(controller, "emptyFriendsState", emptyFriends.gameObject); Assign(controller, "emptyRequestsState", emptyRequests.gameObject); Assign(controller, "otherProfile", FindSceneObject<OtherProfileController>()); Assign(controller, "sendRequestButton", send);
        // FriendsUI owns its own PanelNavigatorController. Its tabs are deliberately
        // left unbound here so the scene can wire them to that local navigator.
        UnityEventTools.AddPersistentListener(receivedTab.onClick, controller.ShowReceivedRequests);
        UnityEventTools.AddPersistentListener(sentTab.onClick, controller.ShowSentRequests);
        UnityEventTools.AddPersistentListener(sentReceivedTab.onClick, controller.ShowReceivedRequests);
        UnityEventTools.AddPersistentListener(sentSentTab.onClick, controller.ShowSentRequests);
        UnityEventTools.AddPersistentListener(send.onClick, controller.SendRequest);
        return root;
    }

    private static void BuildWardrobePanel(GameObject root, PanelNavigatorController navigator, CosmeticChoiceCard choicePrefab)
    {
        Transform legacy = root.transform.Find("WardrobeView"); if (legacy != null) legacy.gameObject.SetActive(false);
        Transform legacyTexts = root.transform.Find("Texts"); if (legacyTexts != null) legacyTexts.gameObject.SetActive(false);
        Transform legacyBack = root.transform.Find("Button.Back"); if (legacyBack != null) legacyBack.gameObject.SetActive(false);
        GameObject existing = FindChild(root.transform, "WardrobeScreen"); if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        GameObject screen = Ui("WardrobeScreen", root.transform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        Image backdrop = screen.AddComponent<Image>(); backdrop.color = new Color(Background.r, Background.g, Background.b, .98f); backdrop.raycastTarget = false;

        Button back = Button(screen.transform, "Button.Back", "VOLTAR", SecondarySprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -74f), new Vector2(130f, 56f), 18); AddBack(back.gameObject, navigator);
        Text(screen.transform, "Title", "ESTILO DE SOBRA.", 52, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -155f), new Vector2(1050f, 70f));
        Badge(screen.transform, "Server", "BR · São Paulo", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-145f, -154f), new Vector2(180f, 42f));

        RectTransform previewPanel = PanelBox(screen.transform, "CapybaraPreviewPanel", new Vector2(0f, 0f), new Vector2(.36f, 1f), new Vector2(44f, 56f), new Vector2(-16f, -220f));
        Text(previewPanel, "PreviewTitle", "SUA CAPIVARA", 24, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -48f), new Vector2(-24f, 0f));
        RawImage raw = Ui("Preview3D", previewPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero).AddComponent<RawImage>();
        raw.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/MyProject2.0/RenderTextures/MainCapybara.renderTexture"); raw.color = Color.white; raw.raycastTarget = false;
        RectTransform rawRect = raw.rectTransform; rawRect.offsetMin = new Vector2(22f, 65f); rawRect.offsetMax = new Vector2(-22f, -75f);
        Text(previewPanel, "PreviewHint", "Ajuste as cores, olhares e acessórios.\nAs alterações são salvas no seu perfil.", 16, Muted, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(26f, 20f), new Vector2(-26f, 56f));

        RectTransform editor = PanelBox(screen.transform, "CustomizationPanel", new Vector2(.36f, 0f), new Vector2(1f, 1f), new Vector2(16f, 56f), new Vector2(-44f, -220f));
        Text(editor, "Heading", "PERSONALIZE DO SEU JEITO.", 28, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -50f), new Vector2(-28f, 0f));
        Button colors = Button(editor, "Tab.Colors", "CORES", TabSelectedSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -118f), new Vector2(130f, 52f), 17);
        Button expressions = Button(editor, "Tab.Expressions", "OLHARES", TabSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -118f), new Vector2(150f, 52f), 17);
        Button accessories = Button(editor, "Tab.Accessories", "ACESSÓRIOS", TabSprite(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(336f, -118f), new Vector2(185f, 52f), 17);

        GameObject colorsView = Ui("ColorsView", editor, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        GameObject expressionsView = Ui("ExpressionsView", editor, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        GameObject accessoriesView = Ui("AccessoriesView", editor, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        expressionsView.SetActive(false); accessoriesView.SetActive(false);

        Slider[] sliders = new Slider[8];
        string[] labels = { "CORPO", "FOCINHO", "ORELHAS", "CASCOS" };
        for (int i = 0; i < labels.Length; i++)
        {
            int column = i % 2; int row = i / 2;
            RectTransform card = CardBox(colorsView.transform, "Color_" + labels[i], new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f + column * 330f, -180f - row * 190f), new Vector2(330f, 164f));
            Text(card, "Label", labels[i], 19, Cream, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -38f), new Vector2(-18f, 0f));
            Text(card, "HueLabel", "MATIZ", 13, Muted, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -76f), new Vector2(100f, 24f));
            sliders[i * 2] = Slider(card, "Hue", new Vector2(18f, -110f), new Vector2(-18f, -88f));
            Text(card, "SatLabel", "SATURAÇÃO", 13, Muted, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -126f), new Vector2(130f, 24f));
            sliders[i * 2 + 1] = Slider(card, "Saturation", new Vector2(18f, -158f), new Vector2(-18f, -136f));
        }
        Button save = Button(colorsView.transform, "Button.Save", "SALVAR ALTERAÇÕES", PrimarySprite(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 28f), new Vector2(220f, 56f), 18);
        Text(colorsView.transform, "Hint", "As cores preservam o brilho original de cada material.", 16, Muted, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 36f), new Vector2(-270f, 62f));

        Text(expressionsView.transform, "Heading", "ESCOLHA UM OLHAR", 22, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -175f), new Vector2(-28f, 0f));
        RectTransform expressionContent = GridContent(expressionsView.transform, "ExpressionsScroll", new Vector2(28f, 28f), new Vector2(-28f, -205f), new Vector2(160f, 175f));
        Text(expressionsView.transform, "EmptyExpressions", "Cadastre as expressões no CosmeticCatalog para elas aparecerem aqui.", 16, Muted, FontStyles.Italic, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -24f), new Vector2(580f, 42f)).alignment = TextAlignmentOptions.Center;
        Text(accessoriesView.transform, "Heading", "ACESSÓRIOS OBTIDOS", 22, Cream, FontStyles.Bold | FontStyles.Italic, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -175f), new Vector2(-28f, 0f));
        RectTransform accessoryContent = GridContent(accessoriesView.transform, "AccessoriesScroll", new Vector2(28f, 28f), new Vector2(-28f, -205f), new Vector2(160f, 175f));
        Text(accessoriesView.transform, "EmptyAccessories", "Os acessórios obtidos aparecerão aqui.", 16, Muted, FontStyles.Italic, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -24f), new Vector2(580f, 42f)).alignment = TextAlignmentOptions.Center;

        CosmeticInventory inventory = root.GetComponent<CosmeticInventory>() ?? root.AddComponent<CosmeticInventory>();
        WardrobeCosmeticsController cosmetics = root.GetComponent<WardrobeCosmeticsController>() ?? root.AddComponent<WardrobeCosmeticsController>();
        cosmetics.enabled = true;
        Assign(cosmetics, "inventory", inventory); Assign(cosmetics, "bodyHue", sliders[0]); Assign(cosmetics, "bodySaturation", sliders[1]); Assign(cosmetics, "snoutHue", sliders[2]); Assign(cosmetics, "snoutSaturation", sliders[3]); Assign(cosmetics, "earsHue", sliders[4]); Assign(cosmetics, "earsSaturation", sliders[5]); Assign(cosmetics, "hoovesHue", sliders[6]); Assign(cosmetics, "hoovesSaturation", sliders[7]); Assign(cosmetics, "expressionsContent", expressionContent); Assign(cosmetics, "expressionPrefab", choicePrefab); Assign(cosmetics, "accessoriesContent", accessoryContent); Assign(cosmetics, "accessoryPrefab", choicePrefab);
        for (int i = 0; i < sliders.Length; i++) UnityEventTools.AddPersistentListener(sliders[i].onValueChanged, cosmetics.PreviewColors);
        UnityEventTools.AddPersistentListener(save.onClick, cosmetics.SaveColors);

        WardrobePanelController tabs = screen.AddComponent<WardrobePanelController>();
        Assign(tabs, "colorsView", colorsView); Assign(tabs, "expressionsView", expressionsView); Assign(tabs, "accessoriesView", accessoriesView); Assign(tabs, "colorsButton", colors); Assign(tabs, "expressionsButton", expressions); Assign(tabs, "accessoriesButton", accessories);
        UnityEventTools.AddPersistentListener(colors.onClick, tabs.ShowColors); UnityEventTools.AddPersistentListener(expressions.onClick, tabs.ShowExpressions); UnityEventTools.AddPersistentListener(accessories.onClick, tabs.ShowAccessories);
    }

    private static FriendRowController CreateFriendRowPrefab(AvatarCatalog avatars)
    {
        const string path = FriendPrefabFolder + "/FriendRow.prefab";
        GameObject root = Ui("FriendRow", null, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(900f, 92f));
        Image background = root.AddComponent<Image>(); ConfigureImage(background, ItemSprite(), Black);
        LayoutElement layout = root.AddComponent<LayoutElement>(); layout.minHeight = 92f;
        Button profile = root.AddComponent<Button>(); profile.targetGraphic = background;
        Image avatar = CreateImage(root.transform, "Avatar", new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(54f, 0f), new Vector2(58f, 58f), PanelSprite(), Color.white);
        TMP_Text nickname = Text(root.transform, "Nickname", "Jogador", 21, Cream, FontStyles.Bold, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(102f, 14f), new Vector2(390f, 30f));
        TMP_Text status = Text(root.transform, "Status", "Offline", 16, Online, FontStyles.Normal, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(102f, -18f), new Vector2(420f, 26f));
        Button invite = Button(root.transform, "Button.Invite", "CONVIDAR", SecondarySprite(), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-122f, 0f), new Vector2(190f, 52f), 17);
        TMP_Text action = invite.GetComponentInChildren<TMP_Text>();
        FriendRowController controller = root.AddComponent<FriendRowController>(); Assign(controller, "avatarImage", avatar); Assign(controller, "avatarCatalog", avatars); Assign(controller, "nicknameText", nickname); Assign(controller, "statusText", status); Assign(controller, "actionText", action); Assign(controller, "profileButton", profile); Assign(controller, "inviteButton", invite);
        FriendRowController asset = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<FriendRowController>(); UnityEngine.Object.DestroyImmediate(root); return asset;
    }

    private static FriendRequestRowController CreateRequestRowPrefab(AvatarCatalog avatars, bool incoming)
    {
        string path = FriendPrefabFolder + (incoming ? "/FriendRequestReceived.prefab" : "/FriendRequestSent.prefab");
        GameObject root = Ui(incoming ? "FriendRequestReceived" : "FriendRequestSent", null, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(900f, 106f));
        Image background = root.AddComponent<Image>(); ConfigureImage(background, ItemSprite(), Black); LayoutElement layout = root.AddComponent<LayoutElement>(); layout.minHeight = 106f;
        Image avatar = CreateImage(root.transform, "Avatar", new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(54f, 0f), new Vector2(60f, 60f), PanelSprite(), Color.white);
        TMP_Text nickname = Text(root.transform, "Nickname", "Jogador", 21, Cream, FontStyles.Bold, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(105f, 15f), new Vector2(360f, 30f));
        TMP_Text metadata = Text(root.transform, "Metadata", "Nível 01", 16, Muted, FontStyles.Normal, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(105f, -18f), new Vector2(360f, 26f));
        Button reject = Button(root.transform, "Button.Reject", "RECUSAR", SecondarySprite(), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-310f, 0f), new Vector2(170f, 52f), 16);
        Button accept = Button(root.transform, "Button.Accept", "ACEITAR", PrimarySprite(), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-122f, 0f), new Vector2(170f, 52f), 16);
        Button cancel = Button(root.transform, "Button.Cancel", "CANCELAR SOLICITAÇÃO", SecondarySprite(), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-160f, 0f), new Vector2(300f, 52f), 16);
        FriendRequestRowController controller = root.AddComponent<FriendRequestRowController>(); Assign(controller, "avatarImage", avatar); Assign(controller, "avatarCatalog", avatars); Assign(controller, "nicknameText", nickname); Assign(controller, "metadataText", metadata); Assign(controller, "acceptButton", accept); Assign(controller, "rejectButton", reject); Assign(controller, "cancelButton", cancel);
        FriendRequestRowController asset = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<FriendRequestRowController>(); UnityEngine.Object.DestroyImmediate(root); return asset;
    }

    private static CosmeticChoiceCard CreateCosmeticChoicePrefab()
    {
        const string path = WardrobePrefabFolder + "/CosmeticChoiceCard.prefab";
        GameObject root = Ui("CosmeticChoiceCard", null, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(160f, 175f));
        Image background = root.AddComponent<Image>(); ConfigureImage(background, ItemSprite(), Black); LayoutElement layout = root.AddComponent<LayoutElement>(); layout.preferredWidth = 160f; layout.preferredHeight = 175f;
        Button button = root.AddComponent<Button>(); button.targetGraphic = background;
        Image icon = CreateImage(root.transform, "Icon", new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -54f), new Vector2(74f, 74f), null, Color.white);
        TMP_Text title = Text(root.transform, "Name", "Item", 16, Cream, FontStyles.Bold, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 35f), new Vector2(-12f, 60f));
        TMP_Text state = Text(root.transform, "State", "Obtido", 14, Orange, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 14f), new Vector2(-12f, 38f));
        Image lockVisual = CreateImage(root.transform, "Lock", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-25f, -24f), new Vector2(26f, 26f), LoadSprite("Assets/MyProject2.0/Sprites/Icons/Icon_Lock.png"), Color.white);
        GameObject selectedRoot = Ui("Selected", root.transform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        Image selectedVisual = selectedRoot.AddComponent<Image>(); ConfigureImage(selectedVisual, TabSelectedSprite(), new Color(Orange.r, Orange.g, Orange.b, .45f)); selectedVisual.raycastTarget = false;
        CosmeticChoiceCard controller = root.AddComponent<CosmeticChoiceCard>(); Assign(controller, "nameText", title); Assign(controller, "stateText", state); Assign(controller, "icon", icon); Assign(controller, "button", button); Assign(controller, "lockVisual", lockVisual.gameObject); Assign(controller, "selectedVisual", selectedVisual.gameObject);
        CosmeticChoiceCard asset = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<CosmeticChoiceCard>(); UnityEngine.Object.DestroyImmediate(root); return asset;
    }

    private static void WireFriendsShortcut(GameObject shortcut, PanelNavigatorController navigator)
    {
        if (shortcut == null) return;
        Button button = shortcut.GetComponent<Button>() ?? shortcut.AddComponent<Button>();
        Image image = shortcut.GetComponent<Image>() ?? shortcut.AddComponent<Image>(); image.color = Color.clear; image.raycastTarget = true; button.targetGraphic = image;
        ClearPersistentListeners(button.onClick);
        LobbyNavigationController lobby = FindSceneObject<LobbyNavigationController>();
        if (lobby != null) UnityEventTools.AddStringPersistentListener(button.onClick, lobby.Open, "Friends");
    }

    private static void AddNavigatorEntry(PanelNavigatorController navigator, string id, GameObject panel)
    {
        SerializedObject serialized = new SerializedObject(navigator); SerializedProperty entries = serialized.FindProperty("panels");
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("id").stringValue != id) continue;
            entry.FindPropertyRelative("panel").objectReferenceValue = panel; serialized.ApplyModifiedPropertiesWithoutUndo(); return;
        }
        entries.InsertArrayElementAtIndex(entries.arraySize); SerializedProperty added = entries.GetArrayElementAtIndex(entries.arraySize - 1); added.FindPropertyRelative("id").stringValue = id; added.FindPropertyRelative("panel").objectReferenceValue = panel; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddBack(GameObject target, PanelNavigatorController navigator)
    {
        PanelBackButton back = target.GetComponent<PanelBackButton>() ?? target.AddComponent<PanelBackButton>(); Assign(back, "navigator", navigator);
    }

    private static RectTransform PanelBox(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = Ui(name, parent, min, max, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero).GetComponent<RectTransform>(); rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        Image image = rect.gameObject.AddComponent<Image>(); ConfigureImage(image, PanelSprite(), Panel); return rect;
    }
    private static RectTransform CardBox(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rect = Ui(name, parent, anchor, anchor, pivot, pos, size).GetComponent<RectTransform>(); Image image = rect.gameObject.AddComponent<Image>(); ConfigureImage(image, PanelSprite(), Panel); return rect;
    }
    private static RectTransform ScrollContent(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, bool grid)
    {
        GameObject root = Ui(name, parent, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); RectTransform rect = root.GetComponent<RectTransform>(); rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        Image image = root.AddComponent<Image>(); image.color = Color.clear; image.raycastTarget = true; Mask mask = root.AddComponent<Mask>(); mask.showMaskGraphic = false;
        ScrollRect scroll = root.AddComponent<ScrollRect>(); scroll.horizontal = false;
        GameObject content = Ui("Content", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), Vector2.zero, new Vector2(0f, 0f));
        if (grid) { GridLayoutGroup layout = content.AddComponent<GridLayoutGroup>(); layout.cellSize = new Vector2(160f, 175f); layout.spacing = new Vector2(12f, 12f); layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 3; }
        else { VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12f; layout.padding = new RectOffset(0, 0, 0, 12); layout.childControlWidth = true; layout.childControlHeight = false; layout.childForceExpandHeight = false; }
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.viewport = rect; scroll.content = content.GetComponent<RectTransform>();
        return content.GetComponent<RectTransform>();
    }
    private static RectTransform GridContent(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, Vector2 cell) => ScrollContent(parent, name, offsetMin, offsetMax, true);
    private static Slider Slider(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject root = Ui(name, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), Vector2.zero, Vector2.zero); RectTransform rect = root.GetComponent<RectTransform>(); rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        Slider slider = root.AddComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f; slider.value = .5f;
        Image background = CreateImage(root.transform, "Background", new Vector2(0f, .5f), new Vector2(1f, .5f), Vector2.zero, new Vector2(0f, 8f), null, Border);
        GameObject fillArea = Ui("Fill Area", root.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); RectTransform fa = fillArea.GetComponent<RectTransform>(); fa.offsetMin = new Vector2(0f, 0f); fa.offsetMax = new Vector2(-14f, 0f);
        GameObject fillRoot = Ui("Fill", fillArea.transform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        Image fill = fillRoot.AddComponent<Image>(); ConfigureImage(fill, null, Orange); slider.fillRect = fill.rectTransform;
        Image handle = CreateImage(root.transform, "Handle", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(18f, 18f), null, Cream); slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        return slider;
    }
    private static TMP_InputField Input(Transform parent, string name, string placeholder, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject root = Ui(name, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), Vector2.zero, Vector2.zero); RectTransform rect = root.GetComponent<RectTransform>(); rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        Image background = root.AddComponent<Image>(); ConfigureImage(background, LoadSprite("Assets/MyProject2.0/Sprites/Inputs/Input_Unselected.png"), Black); background.raycastTarget = true;
        TMP_InputField input = root.AddComponent<TMP_InputField>(); input.targetGraphic = background;
        TextMeshProUGUI value = Text(root.transform, "Text", string.Empty, 19, Cream, FontStyles.Normal, Stretch(), new Vector2(16f, 2f), new Vector2(-16f, -2f), Vector2.zero).GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI hint = Text(root.transform, "Placeholder", placeholder, 19, Muted, FontStyles.Normal, Stretch(), new Vector2(16f, 2f), new Vector2(-16f, -2f), Vector2.zero).GetComponent<TextMeshProUGUI>();
        input.textComponent = value; input.placeholder = hint; return input;
    }
    private static Button Button(Transform parent, string name, string label, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject root = Ui(name, parent, anchor, anchor, pivot, pos, size); Image image = root.AddComponent<Image>(); ConfigureImage(image, sprite, Color.white); image.raycastTarget = true; Button button = root.AddComponent<Button>(); button.targetGraphic = image;
        Color labelColor = sprite == PrimarySprite() ? Black : Cream;
        TMP_Text text = Text(root.transform, "Text", label, fontSize, labelColor, FontStyles.Bold, Stretch(), Vector2.zero, Vector2.zero, Vector2.zero); text.alignment = TextAlignmentOptions.Center; return button;
    }
    private static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        GameObject root = Ui(name, parent, anchor, anchor, pivot, pos, size); Image image = root.AddComponent<Image>(); ConfigureImage(image, sprite, color); return image;
    }
    private static TMP_Text Text(Transform parent, string name, string value, int size, Color color, FontStyles style, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 dimensions)
    {
        bool stretch = anchor == Stretch();
        GameObject root = stretch
            ? Ui(name, parent, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero)
            : Ui(name, parent, anchor, anchor, pivot, pos, dimensions);
        if (stretch)
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.offsetMin = pivot;
            rect.offsetMax = pos;
        }
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size; text.color = color; text.fontStyle = style; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false; text.alignment = TextAlignmentOptions.MidlineLeft; return text;
    }
    private static void Badge(Transform parent, string name, string value, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject root = Ui(name, parent, anchor, anchor, pivot, pos, size); Image image = root.AddComponent<Image>(); ConfigureImage(image, LoadSprite("Assets/MyProject2.0/Sprites/Badges/Badge_Default.png"), new Color(Orange.r, Orange.g, Orange.b, .28f)); TMP_Text text = Text(root.transform, "Text", value, 16, Orange, FontStyles.Bold, Stretch(), Vector2.zero, Vector2.zero, Vector2.zero); text.alignment = TextAlignmentOptions.Center;
    }
    private static GameObject Ui(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform)); if (parent != null) root.transform.SetParent(parent, false); RectTransform rect = root.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size; return root;
    }
    private static Vector2 Stretch() => new Vector2(-1f, -1f);
    private static void ConfigureImage(Image image, Sprite sprite, Color color) { image.sprite = sprite; image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple; image.color = color; image.raycastTarget = false; }
    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    private static Sprite PanelSprite() => LoadSprite("Assets/MyProject2.0/Sprites/Panels/Panel_Default.png");
    private static Sprite ItemSprite() => LoadSprite("Assets/MyProject2.0/Sprites/Panels/Panel_Item.png");
    private static Sprite PrimarySprite() => LoadSprite("Assets/MyProject2.0/Sprites/Buttons/Button_Primary_Default.png");
    private static Sprite SecondarySprite() => LoadSprite("Assets/MyProject2.0/Sprites/Buttons/Button_Secondary_Default.png");
    private static Sprite TabSprite() => LoadSprite("Assets/MyProject2.0/Sprites/Tabs/Tab_Unselected.png");
    private static Sprite TabSelectedSprite() => LoadSprite("Assets/MyProject2.0/Sprites/Tabs/Tab_Selected.png");
    private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color result); return result; }
    private static void EnsureFolder(string path) { if (AssetDatabase.IsValidFolder(path)) return; string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'); string name = System.IO.Path.GetFileName(path); if (!string.IsNullOrEmpty(parent)) { EnsureFolder(parent); AssetDatabase.CreateFolder(parent, name); } }
    private static void EnsureEventSystem() { if (FindSceneObject<EventSystem>() != null) return; GameObject root = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene()); }
    private static T FindSceneObject<T>() where T : Component { foreach (T item in Resources.FindObjectsOfTypeAll<T>()) if (item.gameObject.scene == SceneManager.GetActiveScene()) return item; return null; }
    private static GameObject FindSceneObject(string name) { foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) { GameObject found = FindChild(root.transform, name); if (found != null) return found; } return null; }
    private static GameObject FindChild(Transform parent, string name) { if (parent == null) return null; if (parent.name == name) return parent.gameObject; for (int i = 0; i < parent.childCount; i++) { GameObject found = FindChild(parent.GetChild(i), name); if (found != null) return found; } return null; }
    private static void ClearPersistentListeners(Button.ButtonClickedEvent events) { while (events.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(events, events.GetPersistentEventCount() - 1); }
    private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value) { SerializedObject serialized = new SerializedObject(target); SerializedProperty field = serialized.FindProperty(property); if (field == null) { Debug.LogError($"Campo serializado não encontrado: {target.GetType().Name}.{property}"); return; } field.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
}
#endif
