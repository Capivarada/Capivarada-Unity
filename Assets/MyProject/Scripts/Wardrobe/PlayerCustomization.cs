using System;

[Serializable]
public class PlayerCustomization
{
    public WardrobeSkin Skin;
    public WardrobeItem Head;
    public WardrobeItem Accessory;
    public WardrobeItem Clothing;

    public PlayerCustomization Clone()
    {
        return new PlayerCustomization
        {
            Skin = Skin,
            Head = Head,
            Accessory = Accessory,
            Clothing = Clothing
        };
    }
}
