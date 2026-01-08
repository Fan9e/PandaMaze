using UnityEngine;

public class Key : Item
{
    [SerializeField]
    [Tooltip("ID på denne nøgle. Skal matche den dør, den kan åbne.")]
    private int keyId = 1;

}