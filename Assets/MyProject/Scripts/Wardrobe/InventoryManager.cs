using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private List<WardrobeContent> ownedContent = new();

    public IReadOnlyList<WardrobeContent> OwnedContent => ownedContent;

    public IReadOnlyList<T> GetOwned<T>() where T : WardrobeContent
    {
        List<T> result = new();
        foreach (WardrobeContent content in ownedContent)
        {
            if (content is T typedContent)
                result.Add(typedContent);
        }

        return result;
    }

    public bool Contains(WardrobeContent content)
    {
        return content != null && ownedContent.Contains(content);
    }

    public bool CanGrant(WardrobeContent content)
    {
        return content != null && !Contains(content);
    }

    public bool Grant(WardrobeContent content)
    {
        if (!CanGrant(content))
            return false;

        ownedContent.Add(content);
        return true;
    }
}
