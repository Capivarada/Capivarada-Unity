using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardTopPlayerController : MonoBehaviour
{
    [SerializeField] private TMP_Text nicknameText, recordText, metricText, secondaryText, badgeText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private Button profileButton;
    public void Bind(LeaderboardPlayerData data, LeaderboardMetric metric, System.Action<string> open)
    {
        if (nicknameText != null) nicknameText.text = data.nickname;
        if (recordText != null) recordText.text = data.value.ToString("N0");
        string label = metric == LeaderboardMetric.Matches ? "Partidas" : metric == LeaderboardMetric.Eliminations ? "Eliminações" : metric == LeaderboardMetric.Wins ? "Vitórias" : "Nível";
        if (metricText != null) metricText.text = label;
        if (badgeText != null) badgeText.text = $"Líder - {label}";
        if (secondaryText != null) secondaryText.text = metric == LeaderboardMetric.Matches ? $"{data.wins} vitórias - {data.eliminations} eliminações" : metric == LeaderboardMetric.Eliminations ? $"{data.matches} partidas - {data.wins} vitórias" : metric == LeaderboardMetric.Wins ? $"{data.matches} partidas - {data.eliminations} eliminações" : string.Empty;
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(data.avatarId) ?? avatarCatalog.Default : null;
        if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite;
        if (profileButton != null) { profileButton.onClick.RemoveAllListeners(); profileButton.onClick.AddListener(() => open?.Invoke(data.playerId)); }
    }
}
