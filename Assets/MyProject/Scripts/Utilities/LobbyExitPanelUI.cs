using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>Default exit panel actions. The confirmation stays editable in the lobby scene.</summary>
public sealed class LobbyExitPanelUI : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;
    [SerializeField] private GameObject homePanel;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void CapivaradaExitFullscreenAndReload();
#endif

    public void Configure(PanelManager manager, GameObject lobbyPanel)
    {
        panelManager = manager;
        homePanel = lobbyPanel;
    }

    public void Cancel()
    {
        if (panelManager != null && homePanel != null) panelManager.OpenMainPanel();
        else gameObject.SetActive(false);
    }

    public void ConfirmExit()
    {
#if UNITY_EDITOR
        Debug.Log("[Lobby] Saída confirmada. Application.Quit só fecha a versão compilada do jogo.");
#elif UNITY_WEBGL
        CapivaradaExitFullscreenAndReload();
#else
        Application.Quit();
#endif
    }
}
