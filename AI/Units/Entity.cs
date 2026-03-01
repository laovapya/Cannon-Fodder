using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public abstract class Entity : MonoBehaviour
{
    protected virtual void Awake()
    {
        DefineCollider();
    }
    protected virtual void Start()
    {
        health = 0;
        AddHealth(maxHealth);

        healthRegenInterval = 1 / healthRegenRate;

    }


    protected virtual void Update()
    {
        Regen();
        ResetSoldierMark();

    }
    protected float width;
    protected abstract void DefineCollider();

    [field: SerializeField] public Transform itemHolder { protected set; get; }

    public virtual Vector2 GetBodyCenter()
    {
        return transform.position;
    }

    public int health { protected set; get; } = 0;
    [field: SerializeField] public int maxHealth { get; protected set; } = 40;
    [field: SerializeField] public float healthRegenRate { get; protected set; } = 10;
    private float nextHealthTime = 0;
    protected float healthRegenInterval;



    [field: SerializeField] public float cooldown { protected set; get; } = 0;



    public event Action<int, int> onAddHealth;
    public event Action<int> onDamage;
    public event Action<int, Entity> onDamageFrom;
    public void AddHealth(int amount)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        if (health <= 0)
            Die();

        onAddHealth?.Invoke(health, maxHealth);
    }
    public void AddHealth(int amount, Entity dealer)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        if (health <= 0)
            Die(dealer);

        onAddHealth?.Invoke(health, maxHealth);
        if (amount < 0)
        {
            onDamage?.Invoke(amount);
            onDamageFrom?.Invoke(amount, dealer);
        }
    }
    private bool isDead; //Destroyed only at the end of frame
    private void Regen()
    {
        if (!isDead && Time.time > nextHealthTime)
        {
            nextHealthTime = Time.time + healthRegenInterval;
            AddHealth(1);
        }
    }
    public Action<int> onDie;
    protected virtual void Die()
    {
        StopAllCoroutines();
        isDead = true;
        Destroy(gameObject);
        onDie?.Invoke(0);
    }
    protected virtual void Die(Entity dealer)
    {
        StopAllCoroutines();
        isDead = true;
        Destroy(gameObject);
        onDie?.Invoke(0);
    }

    public void Kill()
    {
        Die();
    }

    public Entity target;
    // protected virtual Entity PrioritizeTarget(Vector2 position, float radius, LayerMask targetMask, LayerMask blockMask)//, float selfColliderRadius)
    // {
    //     List<Collider2D> list = FindTargets(position, radius, targetMask, blockMask);//, selfColliderRadius);
    //     if (list != null && list.Count > 0 &&
    //         UtilContainers.FirstOf<Collider2D>(list, (Collider2D c) => { return Vector2.SqrMagnitude((Vector2)c.transform.position - position); }
    //     ).TryGetComponent<Entity>(out Entity target))
    //         return target;
    //     return null;
    // }


    // public static List<Collider2D> FindTargets(Vector2 position, float radius, LayerMask targetMask, LayerMask blockMask)//, float selfColliderRadius)
    // {
    //     Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius, targetMask);
    //     //if (colliders.Length <= 0) return colliders.ToList();

    //     List<Collider2D> list = new List<Collider2D>();
    //     return UtilContainers.FilteredList(colliders, (Collider2D c) =>
    //     {
    //         Vector2 dir = ((Vector2)c.transform.position - position).normalized;
    //         Vector2 start = position;
    //         start += dir;//* (selfColliderRadius + 0.1f); //dont start inside collider 
    //         RaycastHit2D hit = Physics2D.Raycast(start, dir, radius, targetMask | blockMask);

    //         if (!hit.transform.TryGetComponent<Entity>(out Entity entity)) return false;
    //         return hit.collider == null || entity.IsHitThroughBlock() || !MaskProcessing.instance.LayerInMask(hit.transform.gameObject.layer, blockMask);
    //     });
    // }
    // protected virtual Entity PrioritizeTarget(Vector2 position, float minRadius, float maxRadius, LayerMask targetMask, LayerMask blockMask)//, float selfColliderRadius)
    // {
    //     List<Collider2D> list = FindTargets(position, maxRadius, targetMask, blockMask);//, selfColliderRadius);
    //     list = UtilContainers.FilteredList(list, (Collider2D collider) => Vector2.Distance(position, collider.transform.position) > minRadius);
    //     if (list != null && list.Count > 0 &&
    //         UtilContainers.FirstOf<Collider2D>(list, (Collider2D c) => { return Vector2.SqrMagnitude((Vector2)c.transform.position - position); }
    //     ).TryGetComponent<Entity>(out Entity target))
    //         return target;
    //     return null;
    // }
    public static List<Collider2D> FindTargets(
     Vector2 position,
     float colliderRadius, //ASSUMES CIRCULAR COLLIDER
     float minRadius,
     float maxRadius,
     LayerMask targetMask,
     LayerMask blockMask,
     float viewAngle,
     Vector2 facing)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, maxRadius, targetMask);

        return UtilContainers.FilteredList(colliders, (Collider2D c) =>
        {
            Vector2 delta = (Vector2)c.transform.position - position;
            float distance = delta.magnitude;

            if (distance < minRadius || distance > maxRadius)
                return false;

            if (!UtilMath.IsInViewAngle(position, facing, c.transform.position, viewAngle))
                return false;

            Vector2 dir = delta.normalized;


            Vector2 outsideColliderPos = position + dir * (colliderRadius + 0.1f); //dont start inside collider
            RaycastHit2D hit = Physics2D.Raycast(outsideColliderPos, dir, distance, blockMask);

            if (hit.collider == null)
                return true;

            bool isHitThroughBlock = c.GetComponent<Entity>().target == hit.transform.GetComponent<Entity>(); //for visible climbing zombies 
            return isHitThroughBlock;
        });
    }


    public static Entity FindFirstTarget(Vector2 position, float radius, LayerMask targetMask, LayerMask blockMask, float selfColliderRadius)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius, targetMask);
        Entity entity = null;
        foreach (Collider2D c in colliders)
        {
            Vector2 dir = ((Vector2)c.transform.position - position).normalized;
            Vector2 start = position + dir * (selfColliderRadius + 0.1f); //dont start inside collider;
            RaycastHit2D hit = Physics2D.Raycast(start, dir, radius, targetMask);


            if (hit.collider == null || !MaskProcessing.instance.LayerInMask(hit.transform.gameObject.layer, blockMask) && c.TryGetComponent<Entity>(out entity))
                return entity;
        }
        return null;
    }




    public Action onAttack;


    private float soldierMarkInterval = 0.2f; //soldier cooldown = 0.25
    private float nextSoldierMarkTime;
    public int soldierMark { private set; get; }
    public void AddSoldierMark()
    {
        soldierMark++;
    }
    private void ResetSoldierMark()
    {
        if (Time.time > nextSoldierMarkTime)
        {
            nextSoldierMarkTime = Time.time + soldierMarkInterval;
            soldierMark--;
        }
    }
    protected int zombieAttackersCount;
    public static Action onZombieAttackersChanged;
    private bool hasMaxedOutZombieAttackers = false;
    public void AddZombieAttacker()
    {
        zombieAttackersCount++;
        if (zombieAttackersCount >= GameHandler.instance.maxZombiesPerBuilding && !hasMaxedOutZombieAttackers)
        {
            hasMaxedOutZombieAttackers = true;
            onZombieAttackersChanged?.Invoke();
        }

    }
    public bool HasEnoughAttackers()
    {
        return hasMaxedOutZombieAttackers;//zombieAttackersCount >= GameHandler.instance.maxZombiesPerBuilding;
    }
    public void RemoveZombieAttacker()
    {
        zombieAttackersCount--;
        if (zombieAttackersCount < 0)
            zombieAttackersCount = 0;

        if (zombieAttackersCount < GameHandler.instance.maxZombiesPerBuilding && hasMaxedOutZombieAttackers)
        {
            hasMaxedOutZombieAttackers = false;
            onZombieAttackersChanged?.Invoke();
        }
    }

    public virtual bool IsHitThroughBlock()
    {
        //Debug.Log("called from entity");
        return false;
    }
}
