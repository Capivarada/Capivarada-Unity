using UnityEngine;
using UnityEngine.UI;

/// <summary>Attach to every UI Back button; it always returns through the LobbyCanvas navigator history.</summary>
[RequireComponent(typeof(Button))]
public sealed class PanelBackButton : MonoBehaviour
{
    [SerializeField] private PanelNavigatorController navigator;
    private Button button;
    private void Awake()
    {
        button = GetComponent<Button>();
        if (navigator != null) button.onClick.AddListener(navigator.Back);
    }
    private void OnDestroy()
    {
        if (button != null && navigator != null) button.onClick.RemoveListener(navigator.Back);
    }
}
