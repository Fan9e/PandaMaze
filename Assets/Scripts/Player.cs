using NUnit.Framework.Internal;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth;

    [Header("Weapon")]
    [SerializeField] private Weapon _weapon;           
    [SerializeField] private Animator _weaponAnimator; 

    private bool _isAttacking = false;

    public IWeapon EquippedWeapon { get; private set; }

    [Header("Attack settings")]
    public float attackRadius = 2f;
    public float attackDistance = 1f;
    public LayerMask monsterLayer;
    [SerializeField] private string _weaponAnimatorChildName = "SwordPivot";


    public int MaxHealth
    {
        get => _maxHealth;
        set => _maxHealth = Mathf.Max(0, value);
    }

    public int CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
    }

    private void Awake()
    {
        EnsureMonsterLayerIsSet();
        EnsureWeaponIsAssigned();
        EquipWeapon();
        AutoAssignWeaponAnimator();
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;
    }

    private void Update()
    {
        HandleAttackInput();
    }

    private void EnsureMonsterLayerIsSet()
    {
        if (monsterLayer.value == 0)
            monsterLayer = LayerMask.GetMask("Monster");
    }

    /// <summary>
    /// Sørger for at _weapon ikke er null (prøver at finde ét i children).
    /// </summary>
    private void EnsureWeaponIsAssigned()
    {
        if (_weapon != null)
            return;

        _weapon = GetComponentInChildren<Weapon>();
        if (_weapon == null)
        {
            Debug.LogError("Player har ikke noget Weapon sat eller fundet i children!", this);
        }
    }

    private void EquipWeapon()
    {
        if (_weapon == null)
        {
            EquippedWeapon = null;
            return;
        }

        EquippedWeapon = _weapon as IWeapon;
        if (EquippedWeapon == null)
        {
            Debug.LogError("Weapon på Player implementerer ikke IWeapon!", _weapon);
        }
    }

    /// <summary>
    /// Finder automatisk animator til våbnet (fx på SwordPivot), hvis ikke sat i Inspector.
    /// </summary>
    private void AutoAssignWeaponAnimator()
{
        if (_weaponAnimator != null)
            return;

        if (!string.IsNullOrEmpty(_weaponAnimatorChildName))
        {
            Transform child = transform.Find(_weaponAnimatorChildName);
            if (child != null)
            {
                _weaponAnimator = child.GetComponent<Animator>();
            }
        }

        if (_weaponAnimator == null && _weapon != null)
        {
            _weaponAnimator = _weapon.GetComponentInParent<Animator>();
        }


        if (_weaponAnimator == null)
        {
            _weaponAnimator = GetComponentInChildren<Animator>();
        }

        if (_weaponAnimator == null)
        {
            Debug.LogWarning("Player kunne ikke finde nogen weapon Animator.", this);
        }
    }


    public void EquipNewWeapon(IWeapon newWeapon)
    {
        if (newWeapon == null)
        {
            Debug.LogWarning("EquipNewWeapon blev kaldt med null.", this);
            return;
        }

        var newWeaponMb = newWeapon as MonoBehaviour;
        if (newWeaponMb == null)
        {
            Debug.LogError("IWeapon-instansen er ikke et MonoBehaviour. Kan ikke sættes på spilleren.", this);
            return;
        }


        if (_weapon != null)
        {
            Destroy(_weapon.gameObject);
            _weapon = null;
        }


        Transform socket = transform.Find(_weaponAnimatorChildName);
        if (socket != null)
        {
            newWeaponMb.transform.SetParent(socket, worldPositionStays: false);
            newWeaponMb.transform.localPosition = Vector3.zero;
            newWeaponMb.transform.localRotation = Quaternion.identity;
        }

        _weapon = newWeaponMb.GetComponent<Weapon>(); 
        EquippedWeapon = newWeapon;                  

   
        AutoAssignWeaponAnimator();
    }

    private void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (_isAttacking) return;
        if (EquippedWeapon == null) return;

        Monster target = GetClosestMonsterInRange();
        if (target == null) return;

        StartCoroutine(AttackRoutine(target));
    }

    private IEnumerator AttackRoutine(Monster target)
    {
        _isAttacking = true;

        if (_weaponAnimator != null)
        {
            _weaponAnimator.Play("SwordAttack", 0, 0f);

            yield return null;

            AnimatorStateInfo info = _weaponAnimator.GetCurrentAnimatorStateInfo(0);
            float animLength = info.length;

            if (animLength <= 0.05f)
                animLength = 0.3f;

            yield return new WaitForSeconds(animLength);
        }

        if (target != null && EquippedWeapon != null)
        {
            EquippedWeapon.Attack(target);
        }

        _isAttacking = false;
    }

    private Monster GetClosestMonsterInRange()
    {
        Vector3 center = transform.position + transform.forward * attackDistance;

        Collider[] hits = Physics.OverlapSphere(center, attackRadius, monsterLayer);
        Debug.Log($"OverlapSphere fandt {hits.Length} colliders");

        Monster closest = null;
        float closestDistSq = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Monster m = hit.GetComponentInParent<Monster>();
            if (m == null) continue;

            float distSq = (m.transform.position - center).sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closest = m;
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + transform.forward * attackDistance;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRadius);
    }



}
