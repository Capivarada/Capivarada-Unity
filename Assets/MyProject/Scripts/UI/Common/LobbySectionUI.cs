using UnityEngine;

/// <summary>
/// Component placed on a SectionUI root. Its child Panel is the only object
/// managed by PanelManager, keeping scripts separate from the visual content.
/// </summary>
public sealed class LobbySectionUI : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;
    [SerializeField] private GameObject panel;

    public GameObject Panel => panel;

    public void Configure(PanelManager manager, GameObject sectionPanel)
    {
        panelManager = manager;
        panel = sectionPanel;
    }

    public void Open()
    {
        if (panelManager == null || panel == null)
        {
            Debug.LogWarning("[LobbySectionUI] Painel da seção não configurado.", this);
            return;
        }
        panelManager.OpenPanel(panel);
    }
}
