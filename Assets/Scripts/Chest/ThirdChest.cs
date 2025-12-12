using UnityEditor;
using UnityEngine;

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
    /// Boss-monsteret, som skal være besejret (destroyed),
    /// før kisten kan åbnes. Hvis feltet er tomt (null),
    /// kan kisten åbnes uden dette krav.
    /// </summary>
    [SerializeField] private GameObject monsterBoss;

    /// <summary>
    /// Bestemmer om kisten må åbnes.
    /// Kisten kan kun åbnes, når boss-monsteret er død (eller ikke er sat).
    /// Viser en UI-besked, hvis bossen stadig lever.
    /// </summary>
    /// <returns>
    /// True, hvis kisten må åbnes; ellers false.
    /// </returns>
    protected override bool CanOpen()
    {
        const float MessageDuration = 1f;

        // Hvis bossen ikke er sat, eller allerede er blevet destroyed,
        // så må kisten opføre sig normalt og bruge base-logikken.
        if (monsterBoss == null)
        {
            return base.CanOpen();
        }

        // Bossen lever stadig → vis besked og blokér åbning.
        if (uiMessageManager != null)
        {
            uiMessageManager.ShowMessage(
                "Du mangler at bekæmpe bossen.",
                MessageDuration
            );
        }
        else
        {
            Debug.LogWarning("ThirdChest: uiMessageManager er NULL, kan ikke vise besked.", this);
        }

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