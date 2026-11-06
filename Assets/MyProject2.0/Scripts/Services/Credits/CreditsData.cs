using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CreditMember
{
    public string name;
    public string role;
    public Sprite icon;
    public Sprite image;
    public int order;
}

[CreateAssetMenu(fileName = "Credits", menuName = "Capivarada/Credits Data")]
public sealed class CreditsData : ScriptableObject
{
    [SerializeField] private List<CreditMember> members = new();
    public IReadOnlyList<CreditMember> Members => members;

    public void Configure(IEnumerable<CreditMember> configuredMembers)
    {
        members = configuredMembers == null ? new List<CreditMember>() : new List<CreditMember>(configuredMembers);
    }
}
