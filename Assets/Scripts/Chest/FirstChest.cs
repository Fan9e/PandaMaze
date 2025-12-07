using UnityEditor;
using UnityEngine;

public class FirstChest : Chest
{
    [Header("First Chest Loot")]
    [SerializeField] private Weapon axeWeaponPrefab;   // må gerne være None i Inspector
    [SerializeField] private int keyId = 1;

    protected override IChestLoot CreateLoot()
    {
        if (axeWeaponPrefab == null)
        {
            Debug.LogError("FirstChest: axeWeaponPrefab er ikke sat i Inspector.", this);
            return null;
        }

        return new ChestLoot(axeWeaponPrefab, keyId);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Hvis den ikke er sat endnu, så prøv at hente den fra en kendt sti
        if (axeWeaponPrefab == null)
        {
            // tilpas stien hvis din mappe-struktur er anderledes
            const string path = "Assets/Prefabs/Axe.prefab";
            axeWeaponPrefab = AssetDatabase.LoadAssetAtPath<Weapon>(path);

            if (axeWeaponPrefab != null)
            {
                Debug.Log("FirstChest: satte axeWeaponPrefab automatisk fra " + path, this);
            }
        }
    }
#endif
}


