using System;
using UnityEngine;

/// <summary>
/// Applies cosmetic loadouts to capybaras created by a gameplay scene.
/// Gameplay consumes profile data but never owns its persistence.
/// </summary>
public static class CapybaraGameplayCosmetics
{
    public static void Apply(GameObject capybara, PlayerCosmeticLoadout loadout, CosmeticCatalog catalog = null)
    {
        if (capybara == null)
            return;

        CapybaraAppearanceApplier appearance = capybara.GetComponentInChildren<CapybaraAppearanceApplier>(true);
        if (appearance != null)
            appearance.Apply(loadout ?? new PlayerCosmeticLoadout(), catalog);
    }

    public static void ApplySavedPlayer(GameObject capybara, CosmeticCatalog catalog = null)
    {
        PlayerProfileService profile = PlayerProfileService.Instance;
        PlayerCosmeticLoadout loadout = profile != null && profile.Current != null
            ? profile.Current.cosmetics
            : null;
        Apply(capybara, loadout, catalog);
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

    private static CapybaraColorSelection CreateColor(System.Random random, float minSaturation, float maxSaturation)
    {
        return new CapybaraColorSelection(
            (float)random.NextDouble(),
            Mathf.Lerp(minSaturation, maxSaturation, (float)random.NextDouble()),
            1f);
    }
}
