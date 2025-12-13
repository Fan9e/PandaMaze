using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Første kiste i spillet.
/// Kan først åbnes når dragen er besejret (destroyed, deaktiveret, eller HP <= 0).
/// </summary>
public class FirstChest : Chest
{
    private const float MessageDuration = 1f;
    private const string MessageMissingDragon = "Du mangler at bekæmpe dragen.";

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
    [SerializeField] private GameObject dragon;

    [SerializeField, Tooltip("Tag på dragon-objektet i scenen (skal oprettes i Tags & Layers).")]
    private string dragonTag = "Dragon";

    /// <summary>
    /// Cached reference til Monster-komponenten på dragen (bruges til HP-check).
    /// </summary>
    private Monster dragonMonster;

    /// <summary>
    /// Start er kaldt af Unity, når objektet initialiseres.
    /// </summary>
    private void Start()
    {
        ResolveDragon();
    }

    /// <summary>
    /// Finder og cacher reference til dragen samt dens <see cref="Monster"/>-komponent.
    /// </summary>
    /// <remarks>
    /// Hvis <see cref="dragon"/> ikke er sat (eller er blevet destroyed), forsøger metoden at finde
    /// et objekt i scenen med tagget <see cref="dragonTag"/>. Når <see cref="dragon"/> er fundet,
    /// caches <see cref="Monster"/>-komponenten i <see cref="dragonMonster"/> så vi kan tjekke HP.
    /// Metoden gør ingenting, hvis der hverken er sat en dragon eller der kan findes en via tag.
    /// </remarks>
    private void ResolveDragon()
    {
        if (dragon == null)
        {
            if (TryFindDragonByTag(out GameObject found))
                dragon = found;
        }
        if (dragon == null)
            return;

        if (dragon != null && dragonMonster == null)
        {
            dragonMonster = dragon.GetComponent<Monster>()
                           ?? dragon.GetComponentInParent<Monster>()
                           ?? dragon.GetComponentInChildren<Monster>();
        }
    }

    /// <summary>
    /// Forsøger at finde et GameObject i scenen med det angivne dragon-tag.
    /// </summary>
    /// <remarks>
    /// Metoden kaster ikke en exception, hvis tagget ikke findes; i stedet logges en advarsel,
    /// og metoden returnerer false. Tagget skal være sat og ikke tomt, før der søges.
    /// </remarks>
    /// <param name="found">
    /// Når metoden returnerer, indeholder den det GameObject, der blev fundet med dragon-tagget,
    /// eller null hvis der ikke findes noget match.
    /// </param>
    /// <returns>
    /// True hvis et GameObject med dragon-tagget blev fundet; ellers false.
    /// </returns>
    private bool TryFindDragonByTag(out GameObject found)
    {
        found = null;

        if (string.IsNullOrWhiteSpace(dragonTag))
            return false;

        try
        {
            found = GameObject.FindGameObjectWithTag(dragonTag);
            return found != null;
        }
        catch (UnityException)
        {
            Debug.LogWarning(
                $"{nameof(FirstChest)}: Tag '{dragonTag}' findes ikke. Opret den under Tags & Layers eller sæt dragon manuelt i Inspector.",
                this
            );
            return false;
        }
    }

    /// <summary>
    /// Afgør om dragen er besejret.
    /// True hvis dragon er destroyed, deaktiveret, eller hvis Monster.CurrentHealth <= 0.
    /// </summary>
    private bool IsDragonDefeated()
    {
        if (dragon == null) return true;    
        if (!dragon.activeInHierarchy) return true;

        if (dragonMonster == null) return false;
        return dragonMonster.CurrentHealth <= 0;
    }

    /// <summary>
    /// Bestemmer om kisten må åbnes.
    /// Kisten kan kun åbnes, når dragen er besejret.
    /// </summary>
    /// <returns>
    /// True, hvis kisten må åbnes; ellers false.
    /// </returns>
    protected override bool CanOpen()
    {
        ResolveDragon();

        if (IsDragonDefeated())
            return true;

        ShowMessageDragonNotDefeated();
        return false;
    }

    /// <summary>
    /// Viser besked til spilleren om at dragen stadig skal besejres.
    /// </summary>
    private void ShowMessageDragonNotDefeated()
    {
        if (uiMessageManager != null)
        {
            uiMessageManager.ShowMessage(
                MessageMissingDragon,
                MessageDuration
            );
        }
        else
        {
            Debug.LogWarning("FirstChest: uiMessageManager er NULL, kan ikke vise besked.", this);
        }

    }
    /// <summary>
    /// Opretter det loot-objekt, der skal gives til spilleren,
    /// når kisten åbnes.
    /// </summary>
    /// <returns>
    /// Et <see cref="IChestLoot"/>-objekt med økse, nøgle og potions.
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
