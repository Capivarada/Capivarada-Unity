using UnityEngine;

[CreateAssetMenu(
    fileName = "Wardrobe Preview Settings",
    menuName = "Capivarada/Wardrobe/Preview Settings"
)]
public class WardrobePreviewSettings : ScriptableObject
{
    [SerializeField] private GameObject capybaraPreviewPrefab;

    public GameObject CapybaraPreviewPrefab => capybaraPreviewPrefab;
}
