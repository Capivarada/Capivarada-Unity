using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns references for an instantiated friend entry in InviteView.</summary>
public sealed class FriendGroupInvitePrefabController : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Button profileButton;
    [SerializeField] private Button inviteButton;

    public void Bind(FriendsService.FriendSummary friend, bool canInvite, bool pending, Action openProfile, Action invite)
    {
        if (friend == null) return;
        if (nicknameText != null) nicknameText.text = friend.nickname;
        if (stateText != null) stateText.text = pending ? "Convite enviado" : friend.presence;
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(friend.avatarId) ?? avatarCatalog.Default : null;
        if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite;
        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(() => openProfile?.Invoke());
        }
        if (inviteButton != null)
        {
            inviteButton.onClick.RemoveAllListeners();
            inviteButton.interactable = canInvite && !pending;
            inviteButton.onClick.AddListener(() => invite?.Invoke());
        }
    }
}
