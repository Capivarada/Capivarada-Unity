using TMPro;
using UnityEngine;

/// <summary>One Top 8 row. Assign the visual references on the row prefab.</summary>
public sealed class MatchResultsRowController : MonoBehaviour
{
    [SerializeField] private TMP_Text placementText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text eliminationsText;
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private Sprite standardBackground;
    [SerializeField] private Sprite localPlayerBackground;

    public void Bind(MatchResultsContext.Entry entry)
    {
        if (entry == null) return;
        if (placementText != null) placementText.text = $"#{entry.Placement}";
        if (nicknameText != null) nicknameText.text = entry.Nickname;
        if (eliminationsText != null) eliminationsText.text = $"{entry.Eliminations} eliminações";
        if (background != null)
            background.sprite = entry.IsLocal && localPlayerBackground != null
                ? localPlayerBackground
                : standardBackground;
    }
}
