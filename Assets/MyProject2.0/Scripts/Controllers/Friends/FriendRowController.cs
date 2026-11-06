using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns all serialized references for an instantiated friend-list row.</summary>
public sealed class FriendRowController : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private TMP_Text nicknameText, statusText, actionText;
    [SerializeField] private Button profileButton, inviteButton;

    public void Bind(FriendsService.FriendSummary data, Action openProfile)
    {
        if (nicknameText != null) nicknameText.text = data.nickname;
        if (statusText != null) statusText.text = data.presence;
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(data.avatarId) ?? avatarCatalog.Default : null;
        if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite;
        if (actionText != null) actionText.text = data.canInvite ? "Convidar" : "Ver perfil";
        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(() => openProfile?.Invoke());
        }
        if (inviteButton != null) inviteButton.interactable = data.canInvite;
    }
}
