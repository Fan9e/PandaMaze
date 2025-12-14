using UnityEditor;
using UnityEngine;

/// <summary>
/// tjerde kiste i spillet.
/// Kan først åbnes når dragen er besejret (destroyed, deaktiveret, eller HP <= 0).
/// </summary>
public class ThirdChest : Chest
{
    [Header("Third Chest Loot")]
    /// <summary>
    /// ID på den nøgle, som kisten giver til spilleren.
    /// </summary>
    [SerializeField] private int keyId = 3;

    /// <summary>
    /// Antal potions, som spilleren modtager, når kisten åbnes.
    /// </summary>
    [SerializeField, Min(0)] private int potionAmount = 1;

    /// <summary>
    /// Bestemmer om kisten må åbnes.
    /// Kisten kan kun åbnes, når monsteret er død (eller ikke er sat).
    /// Viser en UI-besked, hvis monsteret stadig lever.
    /// </summary>
    /// <returns>
    /// True, hvis kisten må åbnes; ellers false.
    /// </returns>
    protected override bool CanOpen()
    {
        if (IsRequiredMonsterDefeated())
            return true;

        ShowMessageMonsterNotDefeated();
        return false;
    }

    /// <summary>
    /// Opretter det loot-objekt, der skal gives til spilleren,
    /// når kisten åbnes.
    /// </summary>
    /// <returns>
    /// Et <see cref="IChestLoot"/>-objekt med two-hand sword, nøgle og potions,
    /// eller null hvis våben-prefab mangler.
    /// </returns>
    protected override IChestLoot CreateLoot()
    {
        return new ChestLoot(keyId, potionAmount);
    }

}