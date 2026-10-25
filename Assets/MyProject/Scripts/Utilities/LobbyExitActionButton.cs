using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class LobbyExitActionButton : MonoBehaviour
{
    [SerializeField] private LobbyExitPanelUI exitPanel;
    [SerializeField] private bool confirmsExit;
    private Button button;

    public void Configure(LobbyExitPanelUI panel, bool confirm)
    {
        exitPanel = panel;
        confirmsExit = confirm;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Execute);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Execute);
    }

    private void Execute()
    {
        if (exitPanel == null) return;
        if (confirmsExit) exitPanel.ConfirmExit();
        else exitPanel.Cancel();
    }
}
