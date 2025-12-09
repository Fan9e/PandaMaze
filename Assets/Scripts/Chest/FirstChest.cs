using UnityEditor;
using UnityEngine;

public class FirstChest : Chest
{
    [Header("First Chest Loot")]

    /// <summary>
    /// Våbenet (økse), som spilleren modtager, når denne kiste åbnes.
    /// </summary>
    [SerializeField] private Weapon axeWeaponPrefab;

    /// <summary>
    /// ID på den nøgle, som kisten giver til spilleren.
    /// Bruges fx til at låse døre eller andre kister.
    /// </summary>
    [SerializeField] private int keyId = 1;

    /// <summary>
    /// Antal potions, som spilleren modtager, når kisten åbnes.
    /// </summary>
    [SerializeField, Min(0)] private int potionAmount = 1;

    [Header("Open Condition")]

    /// <summary>
    /// Dragon-monsteret i scenen, som skal være død (destroyed),
    /// før kisten kan åbnes. Hvis feltet er tomt (null),
    /// kan kisten åbnes uden dette krav.
    /// </summary>
    [Tooltip("Dragon-monsteret i scenen, som skal være død (destroyed), før kisten kan åbnes.")]
    [SerializeField] private GameObject dragon;

    /// <summary>
    /// Bestemmer om kisten må åbnes.
    /// Kisten kan kun åbnes, når dragon-monsteret er død (eller ikke er sat).
    /// Viser en UI-besked, hvis dragen stadig lever.
    /// </summary>
    /// <returns>
    /// True, hvis kisten må åbnes; ellers false.
    /// </returns>
    protected override bool CanOpen()
    {
        const float MessageDuration = 1f;

        if (dragon == null)
        {
            return base.CanOpen();
        }

        if (uiMessageManager != null)
        {
            uiMessageManager.ShowMessage(
                "Du mangler at bekæmpe dragen.",
                MessageDuration
            );
        }
        else
        {
            Debug.LogWarning("FirstChest: uiMessageManager er NULL, kan ikke vise besked.", this);
        }

        return false;
    }

    /// <summary>
    /// Opretter det loot-objekt, der skal gives til spilleren,
    /// når kisten åbnes.
    /// </summary>
    /// <returns>
    /// Et <see cref="IChestLoot"/>-objekt med økse, nøgle og potions,
    /// eller null hvis våben-prefab mangler.
    /// </returns>
    protected override IChestLoot CreateLoot()
    {
        if (axeWeaponPrefab == null)
        {
            Debug.LogError("FirstChest: axeWeaponPrefab er ikke sat i Inspector.", this);
            return null;
        }

        // Giver våben + nøgle + potions
        return new ChestLoot(axeWeaponPrefab, keyId, potionAmount);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Kaldt af Unity i editoren, når værdier ændres i Inspector
    /// eller scriptet recompiles. Sørger for, at standard-økse-våbnet
    /// automatisk bliver sat, hvis feltet er tomt.
    /// </summary>
    private void OnValidate()
    {
        EnsureDefaultAxeIsAssigned();
    }

    /// <summary>
    /// Sørger for, at denne kiste altid har en økse sat som våben.
    /// Hvis der ikke er sat noget i Inspector, forsøger metoden at hente
    /// et standard Axe-prefab fra projektmappen via en fast asset-sti.
    /// Logger en advarsel, hvis prefabbet ikke kan findes.
    /// </summary>
    private void EnsureDefaultAxeIsAssigned()
    {
        if (axeWeaponPrefab != null)
            return;

        const string weaponPrefabPath = "Assets/Prefabs/Axe.prefab";
        axeWeaponPrefab = AssetDatabase.LoadAssetAtPath<Weapon>(weaponPrefabPath);

        if (axeWeaponPrefab != null)
        {
            Debug.Log(
                $"FirstChest: satte standard Axe-våben fra '{weaponPrefabPath}'. " +
                $"Våben: {axeWeaponPrefab.name}",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                $"FirstChest: kunne ikke finde Axe-våben på path '{weaponPrefabPath}'.",
                this
            );
        }
    }
#endif
}
