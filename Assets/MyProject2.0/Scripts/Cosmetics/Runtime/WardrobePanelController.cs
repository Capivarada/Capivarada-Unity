using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns only the visual navigation of WardrobeUI. Cosmetic data and persistence remain in WardrobeCosmeticsController.
/// </summary>
public sealed class WardrobePanelController : MonoBehaviour
{
    [SerializeField] private GameObject colorsView;
    [SerializeField] private GameObject expressionsView;
    [SerializeField] private GameObject accessoriesView;
    [SerializeField] private Button colorsButton;
    [SerializeField] private Button expressionsButton;
    [SerializeField] private Button accessoriesButton;

    private void OnEnable() => ShowColors();

    public void ShowColors() => Select(colorsView, colorsButton);
    public void ShowExpressions() => Select(expressionsView, expressionsButton);
    public void ShowAccessories() => Select(accessoriesView, accessoriesButton);

    private void Select(GameObject selectedView, Button selectedButton)
    {
        if (colorsView != null) colorsView.SetActive(colorsView == selectedView);
        if (expressionsView != null) expressionsView.SetActive(expressionsView == selectedView);
        if (accessoriesView != null) accessoriesView.SetActive(accessoriesView == selectedView);
        SetSelected(colorsButton, colorsButton == selectedButton);
        SetSelected(expressionsButton, expressionsButton == selectedButton);
        SetSelected(accessoriesButton, accessoriesButton == selectedButton);
    }

    private static void SetSelected(Button button, bool selected)
    {
        if (button == null) return;
        ColorBlock colors = button.colors;
        colors.normalColor = selected ? new Color(0.95f, 0.384f, 0.17f, 1f) : Color.white;
        button.colors = colors;
    }
}
