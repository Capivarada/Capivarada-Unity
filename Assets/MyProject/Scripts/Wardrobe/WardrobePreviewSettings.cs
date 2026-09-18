using UnityEngine;

[CreateAssetMenu(
    fileName = "Wardrobe Preview Settings",
    menuName = "Capivarada/Wardrobe/Preview Settings"
)]
public class WardrobePreviewSettings : ScriptableObject
{
    [SerializeField] private GameObject capybaraPreviewPrefab;
    [SerializeField] private bool transparentBackground = true;
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.08f, 0.20f, 1f);

    public GameObject CapybaraPreviewPrefab => capybaraPreviewPrefab;
    public bool TransparentBackground => transparentBackground;
    public Color BackgroundColor => backgroundColor;
}
