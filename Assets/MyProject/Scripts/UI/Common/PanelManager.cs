using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Events;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    [System.Serializable]
    public sealed class PanelButtonBinding
    {
        public Button button;
        public GameObject panel;
    }

    [Header("Panels")]
    [SerializeField] private List<GameObject> panels = new List<GameObject>();
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private LobbyHeaderUI lobbyHeader;
    [SerializeField] private List<PanelButtonBinding> sectionButtons = new List<PanelButtonBinding>();
    [SerializeField] private List<Button> backToMainButtons = new List<Button>();
    private readonly List<UnityAction> sectionActions = new List<UnityAction>();

    private void Awake()
    {
        if (lobbyHeader == null) lobbyHeader = GetComponentInChildren<LobbyHeaderUI>(true);

        // O cabeçalho pode estar fora da hierarquia deste gerenciador na cena.
        if (lobbyHeader == null)
            lobbyHeader = FindFirstObjectByType<LobbyHeaderUI>(FindObjectsInactive.Include);
    }
    private void OnEnable() => BindButtons();
    private void Start() => OpenMainPanel();
    private void OnDisable() => UnbindButtons();

    /// <summary>Editor/setup entry point for the lobby's SectionUI roots.</summary>
    public void Configure(GameObject configuredMainPanel, IEnumerable<GameObject> configuredPanels)
    {
        mainPanel = configuredMainPanel;
        panels.Clear();
        if (configuredPanels == null) return;
        foreach (GameObject panel in configuredPanels)
            if (panel != null && !panels.Contains(panel)) panels.Add(panel);
    }

    public void ConfigureButtons(IEnumerable<PanelButtonBinding> configuredSectionButtons, IEnumerable<Button> configuredBackButtons)
    {
        UnbindButtons();
        sectionButtons = configuredSectionButtons == null ? new List<PanelButtonBinding>() : new List<PanelButtonBinding>(configuredSectionButtons);
        backToMainButtons = configuredBackButtons == null ? new List<Button>() : new List<Button>(configuredBackButtons);
        if (isActiveAndEnabled) BindButtons();
    }

    /// <summary>
    /// Abre o painel informado e fecha todos os outros.
    /// </summary>
    public void OpenPanel(GameObject panelToOpen)
    {
        if (panelToOpen == null)
        {
            Debug.LogWarning("[PanelManager] Panel não foi definido.");
            return;
        }

        foreach (GameObject panel in panels)
        {
            if (panel != null)
                panel.SetActive(panel == panelToOpen);
        }
        if (mainPanel != null) mainPanel.SetActive(mainPanel == panelToOpen);
        lobbyHeader?.SetSectionTitle(GetTitleForPanel(panelToOpen));
    }
    public void OpenMainPanel()
    {
        CloseAllPanels();
        if (mainPanel == null)
        {
            Debug.LogWarning("[PanelManager] Main Panel não foi definido.");
            return;
        }

        mainPanel.SetActive(true);
        lobbyHeader?.SetSectionTitle("Início");
        WardrobeManager.Instance?.DiscardSelectedSkin();

    }

    /// <summary>
    /// Fecha todos os painéis.
    /// </summary>
    public void CloseAllPanels()
    {
        foreach (GameObject panel in panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    /// <summary>
    /// Fecha um painel específico.
    /// </summary>
    public void ClosePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void BindButtons()
    {
        if (sectionActions.Count > 0) return;
        foreach (PanelButtonBinding binding in sectionButtons)
        {
            if (binding == null || binding.button == null || binding.panel == null) continue;
            GameObject targetPanel = binding.panel;
            UnityAction action = () => OpenPanel(targetPanel);
            binding.button.onClick.AddListener(action);
            sectionActions.Add(action);
        }
        foreach (Button button in backToMainButtons)
            if (button != null) button.onClick.AddListener(OpenMainPanel);
    }

    private void UnbindButtons()
    {
        int actionIndex = 0;
        foreach (PanelButtonBinding binding in sectionButtons)
        {
            if (binding == null || binding.button == null || binding.panel == null) continue;
            if (actionIndex < sectionActions.Count) binding.button.onClick.RemoveListener(sectionActions[actionIndex]);
            actionIndex++;
        }
        sectionActions.Clear();
        foreach (Button button in backToMainButtons)
            if (button != null) button.onClick.RemoveListener(OpenMainPanel);
    }

    private string GetTitleForPanel(GameObject panel)
    {
        foreach (PanelButtonBinding binding in sectionButtons)
        {
            if (binding == null || binding.panel != panel || binding.button == null) continue;
            return binding.button.name switch
            {
                "Button.Shop" => "Loja",
                "Button.Restroom" => "Vestiário",
                "Button.Leaderboards" => "Placar de Líderes",
                "Button.Credits" => "Créditos",
                "Button.Leave" => "Sair",
                _ => binding.button.name
            };
        }
        return string.Empty;
    }
}
