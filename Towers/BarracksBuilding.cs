using Unity.Collections;
using UnityEngine;
using System.Collections;
public class BarracksBuilding : Building
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    protected override void Start()
    {
        base.Start();

        type = BuildGrid.type.barracks;
        StartCoroutine(SpawnSoldiers());

        healthUseDelta = -Mathf.CeilToInt(spawnAmount * maxHealth / maxSoldierAmount);
    }
    //[SerializeField] private int spawnAmount = 4;
    private int healthUseDelta;
    private IEnumerator SpawnSoldiers()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldown);
            //if (GameHandler.instance.currentPhase == GameHandler.phase.rest) continue;
            foreach (GraphCell c in MapGenerator.instance.worldGraph.graph[cell.x, cell.y].neighbors)
            {
                int x = c.coords.x;
                int y = c.coords.y;
                if (!BuildGrid.instance.GetIfBusy(x, y) && !SoldierGrid.instance.GetIfMaxed(x, y, spawnAmount) && TerritoryGrid.instance.grid.GetValue(x, y) == TerritoryGrid.type.ally)
                {
                    SpawnSoldiers(x, y);
                    AddHealth(healthUseDelta);
                    if (currentSoldierCount >= maxSoldierAmount)
                        Die();
                    break;
                }
            }
        }
    }

    [SerializeField] private int maxSoldierAmount = 32;
    private int currentSoldierCount = 0;
    [field: SerializeField] public int spawnAmount { get; private set; } = 4;


    public void SpawnSoldiers(int x, int y)
    {
        Vector2 tileCenter = BuildGrid.instance.grid.GetCellCenter(x, y);

        int max = spawnAmount;
        int count = SoldierGrid.instance.soldierGrid.GetValue(x, y).Count;
        if (count > SoldierSquad.maxSoldiersPerTile - spawnAmount) max = SoldierSquad.maxSoldiersPerTile - count;
        for (int i = 0; i < max && currentSoldierCount < maxSoldierAmount; ++i)
        //if (GameHandler.instance.SpendSoldier())
        {
            currentSoldierCount++;
            SoldierUnit unit = Unit.Spawn(PrefabReference.instance.soldier, tileCenter, Quaternion.identity) as SoldierUnit;
            //StartCoroutine(JoinFormation(unit, tileCenter));
        }


    }

    // private IEnumerator JoinFormation(SoldierUnit unit, Vector2 cellCenter)
    // {
    //     yield return new WaitForEndOfFrame();
    //     Vector2[] path = { cellCenter, cellCenter };
    //     unit.Reposition(path);
    // }
}
