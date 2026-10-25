using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class LobbySectionButton : MonoBehaviour
{
    [SerializeField] private LobbySectionUI targetSection;
    private Button button;

    public void Configure(LobbySectionUI section)
    {
        targetSection = section;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OpenTarget);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OpenTarget);
    }

    private void OpenTarget()
    {
        if (targetSection != null) targetSection.Open();
    }
}
