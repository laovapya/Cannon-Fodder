using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class Unit : Entity
{
    public Movement mf { private set; get; }

    static public Unit Spawn(Transform prefab, Vector3 position, Quaternion rotation)
    {
        if (!Instantiate(prefab, position, rotation).TryGetComponent<Unit>(out Unit unit))
        {
            Debug.LogError("prefab doesnt have UnitManager component");
            return null;
        }
        else
            return unit;
    }
    protected override void Awake()
    {
        base.Awake();
        mf = GetComponent<Movement>();
    }
    protected override void Start()
    {
        base.Start();

    }
    protected override void Update()
    {
        base.Update();
        SetUnitDesiredDirection();
        FollowDestination();
        SetFacingDirection();
    }
    protected override void DefineCollider()
    {
        CircleCollider2D col = transform.gameObject.GetComponent<CircleCollider2D>();
        width = 8 / 6.4f; //8 pixels for 6.4f PPU 
        col.radius = width;
        col.isTrigger = true;
    }
    public override Vector2 GetBodyCenter()
    {
        Vector2 pos = transform.position;
        return pos + Vector2.up * width * 0.9f;
    }


    [SerializeField] protected int reward;
    protected override void Die()
    {
        onDie?.Invoke(reward);
        base.Die();
    }
    protected override void Die(Entity dealer)
    {
        onDie?.Invoke(reward);
        base.Die();
    }
    public Vector2 facingDirection { protected set; get; } = Vector2.up;
    public virtual void SetFacingDirection()
    {
        if (desiredUnitDirection != Vector2.zero)
            facingDirection = desiredUnitDirection;
    }


    //path-----------------------------------------------------

    public Vector2 desiredUnitDirection { get; protected set; }
    protected virtual void SetUnitDesiredDirection()
    {
        desiredUnitDirection = Vector2.zero;
        if (hasDestination)
            desiredUnitDirection = (destination - transform.position).normalized;
    }

    public bool GetIfTryingToMove() //direction has to be set to zero for unit not to move
    {
        return desiredUnitDirection != Vector2.zero;
    }


    protected Vector2[] currentPath;
    protected int currentPathIndex;
    [SerializeField] protected Vector3 destination;
    protected bool hasDestination = false;
    protected virtual void SetPath(Vector2[] path)
    {
        currentPath = path;

        hasDestination = false;
        if (currentPath != null && currentPath.Length > 1)
        {
            destination = currentPath[1];
            currentPathIndex = 1;
            hasDestination = true;
        }
    }
    protected static float reachedThreshold = 0.4f;
    protected static float pathFindingDistanceThreshold = 0.1f;
    private void FollowDestination()
    {
        if (hasDestination && currentPath != null)
        {
            Vector2 toTarget = destination - transform.position;
            //Vector2 velocity = mf.rb.linearVelocity;
            float dist = toTarget.sqrMagnitude;
            bool reached = dist < reachedThreshold;
            bool angleOK = GetIfTryingToMove() && UtilMath.IsAngleMoreThan90(mf.velocity, toTarget) && dist < pathFindingDistanceThreshold;
            if (reached || angleOK)
            {
                ++currentPathIndex;
                if (currentPathIndex == currentPath.Length)
                {
                    ReachDestination();
                    hasDestination = false;
                }


                else
                    destination = currentPath[currentPathIndex];

            }
        }
    }
    protected virtual void ReachDestination()
    {

    }
}
