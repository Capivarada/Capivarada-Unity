using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns all serialized references for an incoming or outgoing friend-request row.</summary>
public sealed class FriendRequestRowController : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private TMP_Text nicknameText, metadataText;
    [SerializeField] private Button acceptButton, rejectButton, cancelButton;

    public void Bind(FriendsService.FriendRequestSummary data, Action accept, Action reject, Action cancel)
    {
        if (nicknameText != null) nicknameText.text = data.nickname;
        if (metadataText != null) metadataText.text = data.level > 0 ? "Nível " + data.level.ToString("00") : data.createdAtUtc;
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(data.avatarId) ?? avatarCatalog.Default : null;
        if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite;
        BindButton(acceptButton, accept);
        BindButton(rejectButton, reject);
        BindButton(cancelButton, cancel);
    }

    private static void BindButton(Button button, Action action)
    {
        if (button == null) return;
        button.gameObject.SetActive(action != null);
        button.onClick.RemoveAllListeners();
        if (action != null) button.onClick.AddListener(() => action());
    }
}
