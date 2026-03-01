using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class Movement : MonoBehaviour
{
    protected Unit unit;
    //public Rigidbody2D rb { get; private set; }
    [field: SerializeField] public Vector3 velocity { get; protected set; }
    protected virtual void Start()
    {
        //rb = GetComponent<Rigidbody2D>();

        unit = GetComponent<Unit>();

        //velocity saturation equation: speed = acceleration / drag - acceleration * fixedDeltaTime 
        //inputDrag = inputAcceleration / (inputMaxSpeed + inputAcceleration * Time.fixedDeltaTime);

        SetInputParameters(groundMaxSpeed, groundAcceleration);
    }

    protected virtual void FixedUpdate()
    {

        SetUnitMovementDirection();


        ApplyFriction();

        ApplyChangingVelocity();

        StopSlowDown();
        ApplyVelocity();

        UpdateSlows();
    }

    protected virtual void ApplyVelocity()
    {
        if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) ||
        float.IsInfinity(velocity.x) || float.IsInfinity(velocity.y))
            velocity = Vector3.zero;

        transform.position += velocity * Time.fixedDeltaTime;
    }
    [field: SerializeField] public Vector2 unitMovementDirection { get; protected set; }
    protected virtual void SetUnitMovementDirection()
    {
        unitMovementDirection = unit.desiredUnitDirection.normalized;

    }


    [Header("Friction")]

    [SerializeField] public float airFriction = 0.86f;
    //public float surfaceMultiplier { get; protected set; } = 1;
    [SerializeField] private float linearFriction = 60; //to stop in place when not moving.
    protected void ApplyFriction()
    {
        // if (hasFullVelocityControl && unitMovementDirection != Vector2.zero)
        //     return;
        ApplyDrag(airFriction);
        if (unitMovementDirection != Vector2.zero)
            return;
        if (velocity.magnitude > linearFriction * Time.fixedDeltaTime)
            velocity -= velocity.normalized * linearFriction * Time.fixedDeltaTime;
        else
            velocity = Vector2.zero;


    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------------

    [field: SerializeField] public float groundAcceleration { get; protected set; } = 150;
    [field: SerializeField] public float groundMaxSpeed { get; protected set; } = 10;
    [field: SerializeField] public float inputAcceleration { get; protected set; } = 150;
    [field: SerializeField] public float inputMaxSpeed { get; protected set; } = 10;
    private float inputDrag;


    protected void SetInputParameters(float speed, float acceleration)
    {
        inputMaxSpeed = speed;
        inputAcceleration = acceleration;

        //velocity saturation equation: speed = acceleration / drag - acceleration * fixedDeltaTime 
        inputDrag = inputAcceleration / (inputMaxSpeed + inputAcceleration * Time.fixedDeltaTime);
    }
    private bool isSlowedDown;
    private float slowDownTimer;
    //protected float forceMultiplier = 1;
    public void SlowDown(float speedMultiplier, float accelerationMultiplier, float time)
    {
        slowDownTimer = time;
        if (groundMaxSpeed * speedMultiplier < inputMaxSpeed) //choose max slow 
        {
            //forceMultiplier = accelerationMultiplier;
            SetInputParameters(groundMaxSpeed * speedMultiplier, groundAcceleration * accelerationMultiplier);
            isSlowedDown = true;
        }
    }
    private void StopSlowDown()
    {
        if (!isSlowedDown) return;
        slowDownTimer -= Time.fixedDeltaTime;
        if (slowDownTimer <= 0)
        {
            //forceMultiplier = 1;
            SetInputParameters(groundMaxSpeed, groundAcceleration);
            isSlowedDown = false;
        }

    }

    protected float additiveMultiplier = 1f;
    //============================================================================================================================================================
    struct Slow
    {
        public float amount;
        public float time;
    }

    List<Slow> slows = new List<Slow>();

    public void SlowDown(float amount, float time)
    {
        slows.Add(new Slow { amount = amount, time = time });
        Recalculate();
    }

    private void UpdateSlows()
    {
        bool dirty = false;

        for (int i = slows.Count - 1; i >= 0; i--)
        {
            var s = slows[i];
            s.time -= Time.fixedDeltaTime;

            if (s.time <= 0)
            {
                slows.RemoveAt(i);
                dirty = true;
            }
            else
            {
                slows[i] = s;
            }
        }

        if (dirty)
            Recalculate();
    }

    void Recalculate()
    {
        additiveMultiplier = 1f;

        for (int i = 0; i < slows.Count; i++)
            additiveMultiplier -= slows[i].amount;

        additiveMultiplier = Mathf.Clamp01(additiveMultiplier);
    }

    public bool hasFullVelocityControl { get; protected set; }





    protected virtual void ApplyChangingVelocity()
    {
        hasFullVelocityControl = velocity.magnitude <= inputMaxSpeed;

        if (hasFullVelocityControl)
        {
            //justGotImpulse = false;
            //Vector2 velocity = rb.linearVelocity;
            velocity += (Vector3)unitMovementDirection * inputAcceleration * Time.fixedDeltaTime;
            velocity *= 1 - inputDrag * Time.fixedDeltaTime;

            // if (!float.IsNaN(velocity.x) && !float.IsNaN(velocity.y))
            //     rb.linearVelocity = velocity;

        }
        else
            RedirectAndDecreaseVelocity();

    }

    protected float redirectionMultiplier = 2;
    protected virtual void RedirectAndDecreaseVelocity()
    {
        Vector3 inputForce = unitMovementDirection * inputMaxSpeed * redirectionMultiplier * Time.fixedDeltaTime;
        Vector3 wouldBeVelocity = velocity + inputForce;

        if (wouldBeVelocity.magnitude < velocity.magnitude)
            velocity += inputForce;
        else
            velocity = wouldBeVelocity.normalized * velocity.magnitude;
    }


    //----------------------------------------------------------
    public Action onAddForce;
    public Action OnAddImpulse;
    private float knockbackRecieved = 1;
    public void DisableKnockback()
    {
        knockbackRecieved = 0;
    }
    public void EnableKnockback()
    {
        knockbackRecieved = 1;
    }
    public virtual void AddForce(Vector3 force)//over time
    {
        if (onAddForce != null)
            onAddForce();
        velocity += force * Time.fixedDeltaTime;
    }
    //protected bool justGotImpulse;
    public virtual void AddImpulse(Vector3 impulse)// once
    {
        if (OnAddImpulse != null)
            OnAddImpulse();
        velocity += impulse * knockbackRecieved;
        //justGotImpulse = true;
    }


    public void ApplyDrag(float drag)
    {
        velocity *= (1 - drag * Time.fixedDeltaTime);
    }


    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}

