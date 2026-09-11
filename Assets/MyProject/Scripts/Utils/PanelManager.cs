using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private List<GameObject> panels = new List<GameObject>();
    [SerializeField] private GameObject mainPanel;

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
}