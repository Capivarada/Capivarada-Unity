using System;
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

    public void Bind(ChatService.Message message, Action<string> openProfile)
    {
        if (message == null) return;
        if (nicknameText != null) nicknameText.text = message.senderName;
        if (messageText != null) messageText.text = message.text;
        if (timeText != null) timeText.text = message.receivedAtUtc.ToLocalTime().ToString("HH:mm");
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(message.avatarId) ?? avatarCatalog.Default : null;
        if (avatarImage != null) avatarImage.sprite = avatar != null ? avatar.Sprite : null;
        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            profileButton.interactable = !string.IsNullOrWhiteSpace(message.senderId);
            profileButton.onClick.AddListener(() => openProfile?.Invoke(message.senderId));
        }
    }
}
