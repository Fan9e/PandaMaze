using NUnit.Framework.Internal;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerWeapon))]
public class Player : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private int maximumHealth = 100;

    [SerializeField]
    private int currentHealth;

    public int MaximumHealth
    {
        get => maximumHealth;
        set => maximumHealth = Mathf.Max(0, value);
    }

    public int CurrentHealth
    {
        get => currentHealth;
        set => currentHealth = Mathf.Clamp(value, 0, MaximumHealth);
    }

    private void Awake()
    {
        CurrentHealth = MaximumHealth;
    }



}
