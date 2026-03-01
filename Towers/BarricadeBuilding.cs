using UnityEngine;
using System.Collections.Generic;
public class BarricadeBuilding : Building
{
    protected override void Start()
    {
        base.Start();
        type = BuildGrid.type.barricade;
        colliderPushForce = 0;
    }
    protected override void DefineCollider()
    {
        BoxCollider2D col = transform.gameObject.AddComponent<BoxCollider2D>();
        width = 64 / 6.4f;
        col.size = new Vector2(width, width); //64 pixels for 6.4f PPU 
        col.isTrigger = true;
    }
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private int damage = 10;
    private Dictionary<Unit, float> lastHitTime = new Dictionary<Unit, float>();
    [SerializeField] private float timeDistortionPercent = 0.2f;
    protected override void OnTriggerStay2D(Collider2D collision)
    {
        //!MaskProcessing.instance.LayerInMask(collision.gameObject.layer, MaskProcessing.instance.enemy) ||
        if (!collision.transform.TryGetComponent<ZombieUnit>(out ZombieUnit unit)) return;
        float now = Time.time;
        if (!lastHitTime.TryGetValue(unit, out float lastTime) || now - lastTime >= damageInterval)
        {
            unit.AddHealth(-damage);
            AddHealth(-1);
            unit.mf.ResetVelocity();
            unit.mf.SlowDown(0.9f, damageInterval / 2);
            lastHitTime[unit] = now + Random.Range(-timeDistortionPercent * damageInterval, timeDistortionPercent * damageInterval);
        }
    }
    void OnTriggerExit(Collider collision)
    {
        if (!collision.transform.TryGetComponent<Unit>(out Unit unit)) return;
        if (unit != null)
            lastHitTime.Remove(unit);
    }
}
