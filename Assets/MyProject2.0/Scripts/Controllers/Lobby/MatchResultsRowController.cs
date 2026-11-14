using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One Top 8 row. Assign the visual references on the row prefab.</summary>
public sealed class MatchResultsRowController : MonoBehaviour
{
    [SerializeField] private TMP_Text placementText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text eliminationsText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private Button profileButton;

    public void Bind(MatchResultsContext.Entry entry, System.Action<string> openProfile)
    {
        if (entry == null) return;
        if (placementText != null) placementText.text = $"#{entry.Placement}";
        if (nicknameText != null) nicknameText.text = entry.Nickname;
        if (eliminationsText != null) eliminationsText.text = $"{entry.Eliminations} eliminações";
        AvatarDefinition avatar = avatarCatalog != null
            ? avatarCatalog.Find(entry.AvatarId) ?? avatarCatalog.Default
            : null;
        if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite;
        if (profileButton == null) return;
        profileButton.gameObject.SetActive(!entry.IsBot && !entry.IsLocal &&
            !string.IsNullOrWhiteSpace(entry.PlayerId));
        profileButton.onClick.RemoveAllListeners();
        if (profileButton.gameObject.activeSelf)
            profileButton.onClick.AddListener(() => openProfile?.Invoke(entry.PlayerId));
    }
}
