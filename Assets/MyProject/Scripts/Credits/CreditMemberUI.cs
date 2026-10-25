using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI binding for one member card authored as a prefab.</summary>
public sealed class CreditMemberUI : MonoBehaviour
{
    [Header("Card fields")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private Image portraitImage;

    public void Bind(CreditMember member)
    {
        if (member == null) return;

        if (nameText != null) nameText.text = member.name ?? string.Empty;
        if (roleText != null) roleText.text = member.role ?? string.Empty;

        if (portraitImage != null)
        {
            portraitImage.sprite = member.image;
            portraitImage.enabled = member.image != null;
        }
    }
}
