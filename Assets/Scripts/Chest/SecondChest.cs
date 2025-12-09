using UnityEditor;
using UnityEngine;

public class SecondChest : Chest
{

    [Header("Second Chest Loot")]

    /// <summary>
    /// Våbenet, som spilleren modtager, når denne kiste åbnes.
    /// Skal være et two-hand sword weapon-prefab.
    /// </summary>
    [SerializeField] private Weapon twoHandSwordWeaponPrefab;

    /// <summary>
    /// ID på den nøgle, som kisten giver til spilleren.
    /// Bruges fx til at låse døre eller andre kister.
    /// </summary>
    [SerializeField] private int keyId = 2;

    /// <summary>
    /// Antal potions, som spilleren modtager, når kisten åbnes.
    /// Værdien kan ikke være negativ.
    /// </summary>
    [SerializeField, Min(0)] private int potionAmount = 1;

    /// <summary>
    /// Katte-monsteret, som skal være besejret (destroyed),
    /// før kisten kan åbnes. Hvis feltet er tomt (null),
    /// kan kisten åbnes uden dette krav.
    /// </summary>
    [SerializeField] private GameObject cat;

    /// <summary>
    /// Bestemmer, om kisten må åbnes på det aktuelle tidspunkt.
    /// Kisten kan kun åbnes, når katte-monsteret er død (eller ikke er sat).
    /// Viser en UI-besked, hvis katten stadig lever.
    /// </summary>
    /// <returns>
    /// True, hvis kisten må åbnes; ellers false.
    /// </returns>
    protected override bool CanOpen()
    {
        const float MessageDuration = 1f;

        if (cat == null)
        {
            return base.CanOpen();
        }

        if (uiMessageManager != null)
        {
            uiMessageManager.ShowMessage(
                "Du mangler at bekæmpe katten.",
                MessageDuration
            );
        }
        else
        {
            Debug.LogWarning("SecondChest: uiMessageManager er NULL, kan ikke vise besked.", this);
        }

        return false;
    }

    /// <summary>
    /// Opretter det loot-objekt, der skal gives til spilleren,
    /// når kisten åbnes.
    /// </summary>
    /// <returns>
    /// Et <see cref="IChestLoot"/>-objekt med våben, nøgle og potions,
    /// eller null hvis våben-prefab mangler.
    /// </returns>
    protected override IChestLoot CreateLoot()
    {
        if (twoHandSwordWeaponPrefab == null)
        {
            Debug.LogError("SecondChest: twoHandSwordWeaponPrefab er ikke sat i Inspector.", this);
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
                $"SecondChest: satte standard two-hand sword våben fra '{weaponPrefabPath}'. " +
                $"Våben: {twoHandSwordWeaponPrefab.name}",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                $"SecondChest: kunne ikke finde two-hand sword våben på path '{weaponPrefabPath}'.",
                this
            );
        }
    }
#endif
}