using UnityEngine;

/// <summary>Applies profile cosmetics to lobby, practice and network capybaras.</summary>
public static class CapybaraGameplayCosmetics
{
    public static void Apply(GameObject capybara, PlayerCosmeticLoadout loadout, CosmeticCatalog catalog = null)
    {
        if (capybara == null)
            return;

        CapybaraAppearanceApplier appearance = capybara.GetComponentInChildren<CapybaraAppearanceApplier>(true);
        if (appearance == null)
            appearance = capybara.AddComponent<CapybaraAppearanceApplier>();

        appearance.Apply(ResolveLoadout(loadout), catalog);
    }

    public static void ApplySavedPlayer(GameObject capybara, CosmeticCatalog catalog = null)
    {
        PlayerProfileService profile = PlayerProfileService.Instance;
        PlayerCosmeticLoadout loadout = profile != null && profile.Current != null ? profile.Current.cosmetics : null;
        Apply(capybara, loadout, catalog);
    }

    /// <summary>
    /// Existing profiles can contain the obsolete hard-coded orange defaults. They are not a real player choice,
    /// so use the active model materials until the player saves a colour selection in the wardrobe.
    /// </summary>
    public static PlayerCosmeticLoadout ResolveLoadout(PlayerCosmeticLoadout loadout)
    {
        return IsObsoleteDefault(loadout) ? null : loadout;
    }

    /// <summary>Same seed, same bot appearance on every peer.</summary>
    public static PlayerCosmeticLoadout CreateBotLoadout(int seed)
    {
        System.Random random = new System.Random(seed == 0 ? 1 : seed);
        return new PlayerCosmeticLoadout
        {
            body = CreateColor(random, .42f, .78f),
            snout = CreateColor(random, .20f, .48f),
            ears = CreateColor(random, .32f, .72f),
            hooves = CreateColor(random, .20f, .52f),
            expressionId = "expression.default",
            headAccessoryId = string.Empty
        };
    }

    private static bool IsObsoleteDefault(PlayerCosmeticLoadout loadout)
    {
        return loadout == null ||
               (Matches(loadout.body, .075f, .55f) &&
                Matches(loadout.snout, .06f, .25f) &&
                Matches(loadout.ears, .075f, .45f) &&
                Matches(loadout.hooves, .05f, .45f) &&
                (string.IsNullOrEmpty(loadout.expressionId) || loadout.expressionId == "expression.default") &&
                string.IsNullOrEmpty(loadout.headAccessoryId));
    }

    private static bool Matches(CapybaraColorSelection color, float hue, float saturation)
    {
        return Mathf.Abs(color.Hue - hue) < .0001f && Mathf.Abs(color.Saturation - saturation) < .0001f;
    }

    private static CapybaraColorSelection CreateColor(System.Random random, float minSaturation, float maxSaturation)
    {
        return new CapybaraColorSelection(
            (float)random.NextDouble(),
            Mathf.Lerp(minSaturation, maxSaturation, (float)random.NextDouble()),
            1f);
    }
}
