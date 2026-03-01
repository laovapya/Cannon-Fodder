using UnityEngine;
using System.Collections;

public class DecoyBuilding : Building
{
    [SerializeField] private float aggroRadius = 40;

    private LineRenderer lr;
    private int lrPosCount = 32;
    protected override void Start()
    {
        base.Start();

        type = BuildGrid.type.decoy;


        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.positionCount = lrPosCount;
        lr.startWidth = lr.endWidth = waveThickness * 0.1f;




        StartCoroutine(AggroPulse());

    }

    private IEnumerator AggroPulse()
    {
        while (true)
        {
            //AggroNearbyZombies();
            lr.positionCount = lrPosCount;
            StartCoroutine(StartWave());
            yield return new WaitForSeconds(cooldown);
        }
    }

    private void AggroNearbyZombies()
    {
        // Collider2D[] hits = Physics2D.OverlapCircleAll(
        //     transform.position,
        //     aggroRadius,
        //     MaskProcessing.instance.enemy
        // );

        // foreach (var hit in hits)
        // {
        //     if (hit.TryGetComponent<ZombieUnit>(out var zombie))
        //     {
        //         zombie.Aggro(this);
        //     }
        // }
    }
    [SerializeField] private float waveSpeed;
    private float waveStepDistance = 0.5f;
    private float waveThickness = 2;
    private IEnumerator StartWave()
    {
        float radius = 0;

        while (radius < aggroRadius)
        {

            Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            radius,
            MaskProcessing.instance.unit
            );

            foreach (var hit in hits)
            {
                Vector2 delta = hit.transform.position - transform.position;
                float distance = delta.magnitude;
                if (distance > radius - waveThickness && distance < radius && !Physics2D.Raycast(transform.position, delta, radius, MaskProcessing.instance.ridge) && hit.TryGetComponent<ZombieUnit>(out var zombie))
                {
                    zombie.Aggro(this);
                }
            }


            radius += waveStepDistance;
            DrawRing(radius);

            yield return new WaitForSeconds(waveStepDistance / waveSpeed);
        }


    }


    private void DrawRing(float radius)
    {
        if (radius >= aggroRadius)
        {
            lr.positionCount = 0;
            return;
        }

        int count = lr.positionCount;
        float step = 2f * Mathf.PI / count;

        for (int i = 0; i < count; i++)
        {
            float a = step * i;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
        }
    }
}
