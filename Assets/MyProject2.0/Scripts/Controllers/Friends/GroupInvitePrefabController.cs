using UnityEngine;
using UnityEngine.UI;

/// <summary>Reference holder for an empty party slot. Navigation is wired in the Inspector.</summary>
public sealed class GroupInvitePrefabController : MonoBehaviour
{
    [SerializeField] private Button inviteButton;
    public Button InviteButton => inviteButton;
}
