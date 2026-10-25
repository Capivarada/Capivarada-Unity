using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds the credits list from an inspector-authored member card prefab.</summary>
public sealed class CreditsPanelUI : MonoBehaviour
{
    [SerializeField] private CreditsData credits;
    [SerializeField] private Transform content;
    [SerializeField] private CreditMemberUI memberPrefab;
    private readonly List<CreditMemberUI> spawnedRows = new();

    public void Configure(CreditsData data, Transform rowsContent)
    {
        credits = data;
        content = rowsContent;
    }

    public void Configure(CreditsData data, Transform rowsContent, CreditMemberUI configuredMemberPrefab)
    {
        Configure(data, rowsContent);
        memberPrefab = configuredMemberPrefab;
    }

    private void OnEnable()
    {
        ClearRows();
        if (credits == null || content == null) return;
        if (memberPrefab == null)
        {
            Debug.LogWarning("[Credits] Defina o Member Prefab no CreditsPanelUI para exibir os créditos.");
            return;
        }

        foreach (CreditMember member in credits.Members.OrderBy(member => member.order))
        {
            if (member == null) continue;
            CreditMemberUI row = Instantiate(memberPrefab, content);
            row.name = $"Credit - {member.name}";
            row.Bind(member);
            spawnedRows.Add(row);
        }
    }

    private void OnDisable() => ClearRows();

    private void ClearRows()
    {
        foreach (CreditMemberUI row in spawnedRows)
            if (row != null) Destroy(row.gameObject);
        spawnedRows.Clear();
    }
}
