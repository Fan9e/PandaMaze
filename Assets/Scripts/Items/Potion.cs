using UnityEngine;

public class Potion : Item
{
    [SerializeField] private int healAmount = 20;

    public int HealAmount => healAmount;


}
