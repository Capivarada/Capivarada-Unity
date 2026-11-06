using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CreditsController : MonoBehaviour
{
    [SerializeField] private CreditsData credits; [SerializeField] private Transform content; [SerializeField] private CreditMemberController memberPrefab;
    private readonly List<CreditMemberController> members = new();
    private void OnEnable() { Rebuild(); }
    private void Rebuild() { foreach (CreditMemberController member in members) if (member != null) Destroy(member.gameObject); members.Clear(); if (credits == null || content == null || memberPrefab == null) return; foreach (CreditMember data in credits.Members) { CreditMemberController member = Instantiate(memberPrefab, content); member.Bind(data); members.Add(member); } }
}

public class CreditMemberController : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText, roleText; [SerializeField] private Image iconImage, portraitImage;
    public void Bind(CreditMember member) { if (nameText != null) nameText.text = member.name; if (roleText != null) roleText.text = member.role; if (iconImage != null) iconImage.sprite = member.icon; if (portraitImage != null) portraitImage.sprite = member.image; }
}
