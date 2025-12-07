using UnityEditor;
using UnityEngine;

public class SecondChest : Chest
{

    [Header("Second Chest Loot")]
    [SerializeField] private Weapon twoHandSwordWeaponPrefab;
    [SerializeField] private int keyId = 2;

    protected override IChestLoot CreateLoot()
    {
        if (twoHandSwordWeaponPrefab == null)
        {
            Debug.LogError("SecondChest: twoHandSwordWeaponPrefab er ikke sat i Inspector.", this);
            return null;
        }

        return new ChestLoot(twoHandSwordWeaponPrefab, keyId);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Hvis den ikke er sat endnu, så prøv at hente den fra en kendt sti
        if (twoHandSwordWeaponPrefab == null)
        {
            // tilpas stien hvis din mappe-struktur er anderledes
            const string path = "Assets/Prefabs/TwoHandSword.prefab";
            twoHandSwordWeaponPrefab = AssetDatabase.LoadAssetAtPath<Weapon>(path);

            if (twoHandSwordWeaponPrefab != null)
            {
                Debug.Log("SecondChest: satte twoHandSwordWeaponPrefab automatisk fra " + path, this);
            }
        }
    }
#endif
}
