using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns references for a concrete party-member grid prefab.</summary>
public sealed class GroupPlayerPrefabController : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image stateBadge;
    [SerializeField] private Color readyColor = new(0.49f, 0.91f, 0.60f, 1f);
    [SerializeField] private Color notReadyColor = new(1f, 0.39f, 0.16f, 1f);
    [SerializeField] private Button profileButton;

    public void Bind(PartyService.Member member, Action openProfile)
    {
        if (member == null) return;
        if (nicknameText != null) nicknameText.text = member.nickname;
        AvatarDefinition avatar = avatarCatalog != null ? avatarCatalog.Find(member.avatarId) ?? avatarCatalog.Default : null;
        if (avatarImage != null && avatar != null) avatarImage.sprite = avatar.Sprite;

        bool ready = member.isLeader || member.ready;
        if (stateText != null) stateText.text = member.isLeader ? "Líder" : ready ? "Pronto" : "Não pronto";
        if (stateText != null) stateText.color = ready ? readyColor : notReadyColor;
        if (stateBadge != null) stateBadge.color = ready ? readyColor : notReadyColor;
        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(() => openProfile?.Invoke());
        }
    }
}
