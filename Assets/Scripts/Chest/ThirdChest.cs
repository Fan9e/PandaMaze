using UnityEditor;
using UnityEngine;

public class ThirdChest : Chest
{

    [Header("Third Chest Loot")]

    /// <summary>
    /// Våbenet, som spilleren modtager, når denne kiste åbnes.
    /// Skal være et two-hand sword weapon-prefab.
    /// </summary>
    [SerializeField] private Weapon twoHandSwordWeaponPrefab;

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
        if (twoHandSwordWeaponPrefab == null)
        {
            Debug.LogError("ThirdChest: twoHandSwordWeaponPrefab er ikke sat i Inspector.", this);
            return null;
        }

        return new ChestLoot(twoHandSwordWeaponPrefab, keyId, potionAmount);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Kaldt af Unity i editoren, når værdier ændres i Inspector
    /// eller scriptet recompiles. Sørger for, at standard two-hand
    /// sword-våbnet automatisk bliver sat, hvis feltet er tomt.
    /// </summary>
    private void OnValidate()
    {
        EnsureDefaultTwoHandSwordIsAssigned();
    }

    /// <summary>
    /// Sørger for, at denne kiste altid har et two-hand sword-våben sat.
    /// Hvis der ikke er sat noget i Inspector, forsøger metoden at hente
    /// et standard-prefab fra projektmappen via en fast asset-sti.
    /// Logger en advarsel, hvis prefabbet ikke kan findes.
    /// </summary>
    private void EnsureDefaultTwoHandSwordIsAssigned()
    {
        if (twoHandSwordWeaponPrefab != null)
            return;

        const string weaponPrefabPath = "Assets/Prefabs/TwoHandSword.prefab";
        twoHandSwordWeaponPrefab = AssetDatabase.LoadAssetAtPath<Weapon>(weaponPrefabPath);

        if (twoHandSwordWeaponPrefab != null)
        {
            Debug.Log(
                $"ThirdChest: satte standard two-hand sword våben fra '{weaponPrefabPath}'. " +
                $"Våben: {twoHandSwordWeaponPrefab.name}",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                $"ThirdChest: kunne ikke finde two-hand sword våben på path '{weaponPrefabPath}'.",
                this
            );
        }
    }
#endif
}