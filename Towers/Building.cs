using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Building : Entity
{
    [field: SerializeField] public BuildGrid.type type { protected set; get; }
    protected Transform healthbarCanvas;
    private Slider healthbar;


    protected Vector2Int cell;

    [SerializeField] protected int rewardPerInterval;
    [SerializeField] protected float rewardInterval;
    protected override void Start()
    {

        int x, y;
        BuildGrid.instance.grid.GetClampedXY(transform.position, out x, out y);
        cell = new Vector2Int(x, y);

        ResetCorruption();
        TerritoryGrid.instance.onTerritoryUpdate += ResetCorruption;
        onDie += (int x) => { TerritoryGrid.instance.onTerritoryUpdate -= ResetCorruption; };
        //type = BuildGrid.type.empty;

        onAddHealth += UpdateHealthbar;

        healthbarCanvas = Instantiate(PrefabReference.instance.healthbar, transform);
        healthbar = healthbarCanvas.GetComponentInChildren<Slider>(true);
        if (healthbar == null)
            Debug.LogError("no slider on healthbar prefab");

        healthbar.transform.parent.SetParent(PrefabReference.instance.folderDynamicObjects); //unparent canvas, freeze rotation. 

        sprite = GetComponent<SpriteRenderer>();
        startingColor = sprite.color;
        if (rewardPerInterval != 0)
            StartCoroutine(Reward());

        onDamage += (int x) => { healthbarVisibilityTimer = healthbarDamageVisibleTime; };


        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        SwitchHealthbarVisibility();
    }
    protected override void DefineCollider() //remove this!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    {
        if (gameObject.layer == LayerMask.NameToLayer("buildingNeutral")) return;
        CircleCollider2D col = transform.gameObject.GetComponent<CircleCollider2D>();
        width = 16 / 6.4f;
        col.radius = width; //16 pixels for 6.4f PPU 
        col.isTrigger = true;
    }
    private void UpdateHealthbar(int health, int maxHealth)
    {
        if (healthbar != null) healthbar.value = (float)health / (float)maxHealth;
    }
    private float healthbarVisibilityTimer = 0;
    private float healthbarDamageVisibleTime = 2;
    private void SwitchHealthbarVisibility()
    {
        healthbarVisibilityTimer -= Time.deltaTime;
        bool isVisible = healthbarVisibilityTimer > 0 || SelectionGrid.instance.isTileHighlighted(cell.x, cell.y);
        if (healthbarCanvas != null) healthbarCanvas.gameObject.SetActive(isVisible);
    }

    protected override void Die()
    {
        if (healthbar != null)
            Destroy(healthbar.transform.parent.gameObject);
        BuildGrid.instance.TryBuild(BuildGrid.type.empty, transform.position);
        onAddHealth -= UpdateHealthbar;
        base.Die();
    }
    protected override void Die(Entity dealer)
    {
        if (healthbar != null)
            Destroy(healthbar.transform.parent.gameObject);
        BuildGrid.instance.TryBuild(BuildGrid.type.empty, transform.position);
        onAddHealth -= UpdateHealthbar;
        base.Die(dealer);
    }
    [SerializeField] protected float colliderPushForce = 20;
    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (!MaskProcessing.instance.LayerInMask(collision.gameObject.layer, MaskProcessing.instance.unit) || !collision.transform.TryGetComponent<Unit>(out Unit unit)) return;
        if (MaskProcessing.instance.LayerInMask(collision.gameObject.layer, MaskProcessing.instance.enemy) && unit is ZombieUnit zombieUnit && zombieUnit.currentState == ZombieUnit.state.climbing) return;

        // Vector3 fromTarget = collision.transform.position - transform.position;
        // float force = colliderPushForce * Mathf.Clamp01(1 - Vector2.SqrMagnitude(fromTarget) / (width * width));
        // unit.mf.AddForce(fromTarget.normalized * force);
        Vector2 fromTarget = collision.transform.position - transform.position;

        if (fromTarget.sqrMagnitude <= 0f)
            return;

        //constant force
        unit.mf.AddForce(fromTarget.normalized * colliderPushForce);
    }
    [SerializeField] private bool shouldCorrupt = true;
    private Color startingColor;
    private Color corruptionColor = new Color(0.6f, 0f, 1f);
    private SpriteRenderer sprite;
    private Coroutine corruptionCoroutine = null;
    static private float corruptionInterval = 0.5f;
    private float corruptionTimer;
    protected float corruptionTime = 8;
    private float corruptionPercent = 0;
    private IEnumerator Corrupt(float corruptionTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(corruptionInterval);
            //if (GameHandler.instance.currentPhase != GameHandler.phase.fight) continue;
            corruptionPercent = (corruptionTimer += corruptionInterval) / corruptionTime;
            if (corruptionPercent >= 1) Die();

            sprite.color = Color.Lerp(startingColor, corruptionColor, corruptionPercent);
        }
    }

    private void ResetCorruption()
    {
        if (!shouldCorrupt) return;
        if (MaskProcessing.instance.LayerInMask(gameObject.layer, MaskProcessing.instance.enemy)) return;
        if (TerritoryGrid.instance.grid.GetValue(cell.x, cell.y) == TerritoryGrid.type.enemy && corruptionCoroutine == null) corruptionCoroutine = StartCoroutine(Corrupt(corruptionTime));
        if (TerritoryGrid.instance.grid.GetValue(cell.x, cell.y) == TerritoryGrid.type.neutral && corruptionCoroutine == null) corruptionCoroutine = StartCoroutine(Corrupt(corruptionTime * 2));
        if (TerritoryGrid.instance.grid.GetValue(cell.x, cell.y) == TerritoryGrid.type.ally && corruptionCoroutine != null)
        {
            StopCoroutine(corruptionCoroutine); corruptionCoroutine = null;
            corruptionTimer = 0;
            corruptionPercent = 0;
            sprite.color = startingColor;
        }
    }

    private IEnumerator Reward()
    {
        while (true)
        {
            yield return new WaitForSeconds(rewardInterval);
            GameHandler.instance.AddPlayerBudget(rewardPerInterval);
        }
    }
}
