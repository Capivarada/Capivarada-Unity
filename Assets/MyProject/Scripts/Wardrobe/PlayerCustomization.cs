using System;

[Serializable]
public class PlayerCustomization
{
    public WardrobeSkin Skin;
    public WardrobeHead Head;
    public WardrobeAccessory Accessory;
    public WardrobeCloth Clothing;

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
