using UnityEngine;

[CreateAssetMenu(fileName = "PointPackage", menuName = "Capivarada/Shop/Point Package")]
public sealed class PointPackage : ScriptableObject
{
    [SerializeField] private string packageId;
    [SerializeField] private string displayName;
    [SerializeField] private int points;
    [SerializeField] private string displayPrice;
    [SerializeField] private Sprite sprite;
    [SerializeField] private string futurePlayFabProductId;
    public string PackageId => packageId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int Points => points;
    public string DisplayPrice => displayPrice;
    public Sprite Sprite => sprite;
    public string FuturePlayFabProductId => futurePlayFabProductId;
}
