using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns all serialized references for one instantiated chat message row.</summary>
public sealed class ChatMessagePrefabController : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Button profileButton;

    public void Bind(ChatService.Message message)
    {
        if (message == null) return;
        if (nicknameText != null) nicknameText.text = message.senderName;
        if (messageText != null) messageText.text = message.text;
        if (timeText != null) timeText.text = message.receivedAtUtc.ToLocalTime().ToString("HH:mm");
        if (avatarImage != null && avatarCatalog != null) avatarImage.sprite = avatarCatalog.Default != null ? avatarCatalog.Default.Sprite : null;
        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            // Vivox exposes the Unity Authentication account ID. It is not a PlayFab ID,
            // so opening OtherProfile with it would be incorrect until the title enables
            // a verified account mapping in the chat metadata/backend.
            profileButton.interactable = false;
        }
    }
}
