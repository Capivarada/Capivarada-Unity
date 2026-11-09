using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Binds an incoming party invite. Its owner controls modal navigation/visibility.</summary>
public sealed class GroupInviteModalController : MonoBehaviour
{
    [SerializeField] private PartyService partyService;
    [SerializeField] private AvatarCatalog avatarCatalog;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private TMP_Text feedbackText;

    private PartyService.Invitation currentInvite;

    private void Awake()
    {
        if (partyService == null) partyService = ServiceHub.Instance != null ? ServiceHub.Instance.Party : PartyService.Instance;
    }

    public void BindLatest()
    {
        if (partyService == null || partyService.Incoming.Count == 0) { Bind(null); return; }
        Bind(partyService.Incoming[0]);
    }

    public void Bind(PartyService.Invitation invitation)
    {
        currentInvite = invitation;
        bool valid = invitation != null && !string.IsNullOrWhiteSpace(invitation.inviteId);
        if (nicknameText != null) nicknameText.text = valid ? invitation.nickname : string.Empty;
        if (messageText != null) messageText.text = valid ? $"{invitation.nickname} convidou você para um grupo." : "Nenhum convite pendente.";
        AvatarDefinition avatar = valid && avatarCatalog != null ? avatarCatalog.Find(invitation.avatarId) ?? avatarCatalog.Default : null;
        if (avatarImage != null) avatarImage.sprite = avatar != null ? avatar.Sprite : null;
        if (acceptButton != null) acceptButton.interactable = valid;
        if (rejectButton != null) rejectButton.interactable = valid;
    }

    public void Accept()
    {
        if (currentInvite == null || partyService == null) return;
        SetButtons(false);
        partyService.Accept(currentInvite.inviteId, () => { if (feedbackText != null) feedbackText.text = "Você entrou no grupo."; });
    }

    public void Reject()
    {
        if (currentInvite == null || partyService == null) return;
        SetButtons(false);
        partyService.Reject(currentInvite.inviteId, () => { if (feedbackText != null) feedbackText.text = "Convite recusado."; });
    }

    private void SetButtons(bool value)
    {
        if (acceptButton != null) acceptButton.interactable = value;
        if (rejectButton != null) rejectButton.interactable = value;
    }
}
