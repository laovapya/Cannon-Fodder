using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShooterBuilding : Building
{
    //private float distanceToShotStart;
    private SpriteRenderer sr;
    [SerializeField] private List<Sprite> rotationSprites = new();
    [SerializeField] private List<Sprite> rotationAttackSprites = new();
    protected override void Start()
    {
        base.Start();
        //type = BuildGrid.type.turret; //or artillery! -> in inspector
        //gun.rotation = Util.LookAt(Vector2.right);


        StartCoroutine(HeatCooloff());
        sr = gunRotating.GetComponent<SpriteRenderer>();
        //distanceToShotStart = Vector2.Distance(transform.position, itemHolder.position);
        colliderRadius = GetComponent<CircleCollider2D>().radius * transform.localScale.x;
    }
    //    private float colliderRadius;
    private float colliderRadius;
    [SerializeField] private Transform gunRotating;
    //[SerializeField] private Transform itemHolderPivot;
    protected override void Update()
    {
        base.Update();
        Shoot();
        Rotate();
        SwitchTarget();
        ChangeSprite();
    }
    [field: SerializeField] public float maxRange { get; protected set; }
    [field: SerializeField] public float minRange { get; protected set; } = 0;
    [SerializeField] protected Transform projectile;
    private float nextShotTime = 0;


    //[SerializeField] private Transform gun;
    [SerializeField] protected LayerMask targetMask;
    [SerializeField] protected LayerMask blockMask;
    [SerializeField] protected SoundManager.Sound shootSound;


    [SerializeField] private int maxHeat = 18; private int heat = 0;
    [SerializeField] private float overheatTime = 3;
    private int effectsPerOverheat = 10;
    [SerializeField] private float heatCooloffRate = 1;

    // private Entity target;
    private float nextTargetTime;
    private float targetingInterval = 0.5f;
    private void SwitchTarget()
    {
        if (Time.time > nextTargetTime)
        {
            nextTargetTime = Time.time + targetingInterval;
            target = PrioritizeTarget();
            if (target != null)
                StartRotation((target.transform.position - transform.position).normalized);
        }
    }
    protected void Shoot()
    {
        //Entity target;


        if (Time.time >= nextShotTime + Random.Range(-0.1f, 0.1f) && target != null)
        {
            Vector2 delta = target.transform.position - transform.position;
            Vector2 dir = delta.normalized;
            float distance = delta.magnitude;


            RaycastHit2D hit = Physics2D.Raycast(transform.position, (target.transform.position - transform.position).normalized, distance, blockMask);

            // bool IsHitThroughBlock = false;
            // if (target.TryGetComponent<ZombieUnit>(out var zombie))
            //     IsHitThroughBlock = (zombie.currentState == ZombieUnit.state.climbing);


            //bool isHitThroughBlock = true;//target is ZombieUnit zombie;

            if (!UtilMath.IsInViewAngle(transform.position, new Vector2(Mathf.Cos(currentRotation * Mathf.Deg2Rad), Mathf.Sin(currentRotation * Mathf.Deg2Rad)), target.transform.position, shootViewAnge)
            || (hit.collider != null && target.target == hit.transform.GetComponent<Entity>()) //is visible climbing zombie
            || distance > maxRange || distance < minRange)
            {
                target = null;
                return;
            }
            //target.AddSoldierMark();
            nextShotTime = Time.time + cooldown;
            if (heat++ >= maxHeat)
            {
                nextShotTime = Time.time + overheatTime;
                heat = 0;
                StartCoroutine(PlaySteamEffect(effectsPerOverheat));
                SoundManager.PlaySound(SoundManager.Sound.overheat, itemHolder.position);
            }



            //float targetDistance = delta.magnitude;

            Vector2 shootPos = itemHolder.position;
            //shootPos += dir * (width + Projectile.colliderRadius + 0.1f); //dont start inside collider. bullet has some size too.



            //Util.LookAt(gun, dir);
            //currentRotation = UtilMath.GetAngleFromDirection(dir);
            //SetRotationFrame();
            //if (targetDistance - distanceToShotStart < 0) Debug.LogError("negative projectile range");
            if (type == BuildGrid.type.artillery)
            { //custom range and velocity adjustment
                Vector2 targetPos = target.transform.position;

                if (target is ZombieUnit zombie)
                {
                    float predictionFactor = 0.5f;
                    float projectileSpeed = projectile.GetComponent<Projectile>().initSpeed;
                    if (zombie.currentState == ZombieUnit.state.horde)
                    {
                        targetPos = zombie.currentHorde.GetLocalCentroid(targetPos);
                        predictionFactor = 0.3f;
                    }

                    float timeToTarget = Vector2.Distance(itemHolder.position, targetPos) / projectileSpeed;
                    targetPos = (Vector3)targetPos + zombie.mf.velocity * timeToTarget * predictionFactor;
                }

                Projectile.Create(projectile, this, shootPos, dir, Vector2.Distance(itemHolder.position, targetPos));
            }

            else
                Projectile.Create(projectile, this, shootPos, dir, maxRange);
            //EffectManager.PlayEffect(EffectManager.Effect.muzzleFlash, itemHolder.position, Quaternion.AngleAxis(currentRotation, Vector3.forward)).localScale *= 3.9f; //make its own effect;
            SoundManager.PlaySound(shootSound, transform.position, Random.Range(0.95f, 1.05f));

        }
    }
    private IEnumerator PlaySteamEffect(int times)
    {
        while (times > 0)
        {
            EffectManager.PlayEffect(EffectManager.Effect.steam, itemHolder.position, Quaternion.identity);
            times--;
            yield return new WaitForSeconds(overheatTime / effectsPerOverheat);
        }
    }
    private IEnumerator HeatCooloff()
    {
        while (true)
        {
            heat--;
            if (heat < 0) heat = 0;
            yield return new WaitForSeconds(heatCooloffRate);
        }
    }

    private float desiredRotation = 180;
    [SerializeField] private float rotationTime360 = 5;
    [SerializeField] private float shootViewAnge = 10;

    private float rotationTime;
    private float rotationTimer;
    private bool isRotating;
    private float startRotation;
    private float currentRotation = 0; //because gun sprite faces up ?
    private void StartRotation(Vector2 dir)
    {
        startRotation = currentRotation;//gun.rotation.eulerAngles.z;

        desiredRotation = Mathf.Repeat(UtilMath.GetAngleFromDirection(dir), 360f);

        rotationTime = rotationTime360 * Mathf.Abs(Mathf.DeltaAngle(startRotation, desiredRotation)) / 360f;

        rotationTimer = 0;
        isRotating = true;
    }
    //[SerializeField] private float rotationAngleOffset = 180;
    private void Rotate()
    {
        if (!isRotating) return;
        rotationTimer += Time.deltaTime;

        float t = Mathf.Clamp01(rotationTimer / rotationTime);
        currentRotation = Mathf.LerpAngle(startRotation, desiredRotation, t);
        currentRotation = Mathf.Repeat(currentRotation, 360f);

        //itemHolderPivot.rotation = Quaternion.Euler(0, 0, currentRotation);

        if (t >= 1f)
            isRotating = false;
    }
    private int currentRotationIndex = 0;
    private void ChangeSprite()
    {
        if (type == BuildGrid.type.turret)
        {
            int frameAmount = rotationSprites.Count;
            int index = Mathf.RoundToInt(currentRotation / (360f / frameAmount)) % frameAmount;
            if (index != currentRotationIndex)
            {
                Debug.Log("rotation index " + index);
                sr.sprite = rotationSprites[index];
                currentRotationIndex = index;
            }

        }
        else
        {
            gunRotating.rotation = Quaternion.Euler(0, 0, currentRotation);
        }
    }
    //private float currentRotation = 0;
    //private SpriteRenderer sr;
    //private Sprite[] rotationFrames;
    //private void SetRotationFrame()
    //{
    //    int frameAmount = rotationFrames.Length;
    //    int index = Mathf.RoundToInt(currentRotation / (360 / frameAmount)) % frameAmount;
    //    sr.sprite = rotationFrames[index];
    //}
    private int targetBin;
    protected Entity PrioritizeTarget()
    {
        Vector2 forward = new Vector2(Mathf.Cos(currentRotation * Mathf.Deg2Rad), Mathf.Sin(currentRotation * Mathf.Deg2Rad));
        List<Collider2D> list = FindTargets(transform.position, colliderRadius, minRange, maxRange, targetMask, blockMask, 360, forward);//, transfo);
        //list = UtilContainers.FilteredList(list, c => Vector2.Distance(position, c.transform.position) > minRadius);

        if (list == null || list.Count == 0)
            return null;

        // convert target positions to angles and assign bins (0-7)

        Collider2D best = null;
        int bestBin = -1;
        int bestSoldierMark = -1;

        foreach (var c in list)
        {
            Vector2 dir = (c.transform.position - transform.position).normalized;
            int bin = GetBin(dir);
            if (best == null || bin < bestBin || (bin == bestBin && c.GetComponent<Entity>().soldierMark > bestSoldierMark))
            {
                best = c;
                bestBin = bin;
                bestSoldierMark = c.GetComponent<Entity>().soldierMark;
            }
        }

        if (best != null && best.TryGetComponent<Entity>(out Entity target))
        {
            targetBin = bestBin;
            return target;
        }


        return null;


        int GetBin(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            return Mathf.FloorToInt(angle / 45f); // 360/8 = 45
        }
    }


}
