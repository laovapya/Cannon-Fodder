using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class HiveBuilding : Building
{
    protected override void Start()
    {
        base.Start();
        StartCoroutine(SpawnZombies());


        StartCoroutine(HeatCooloff());




        heat = UnityEngine.Random.Range(0, maxHealth + 1);

        int x, y;
        BuildGrid.instance.grid.GetClampedXY(transform.position, out x, out y);
        cellIndex = new Vector2Int(x, y);

        UpdateHiveCells();
        PathGrid.instance.onGridUpdate += UpdateHiveCells;

        GenerateBackToHiveMap();

        if (type == BuildGrid.type.harasserHive)
        {
            GameHandler.instance.onWaveEnd += Die;
            onDie += (int x) => { GameHandler.instance.onWaveEnd -= Die; };
        }

    }

    protected virtual void UpdateHiveCells()
    {
        UpdateCells(spawnCells, spawnRadius);
    }
    private Vector2Int cellIndex;

    private List<Vector2Int> spawnCells = new List<Vector2Int>();

    protected void UpdateCells(List<Vector2Int> cellList, float radius)
    {
        MapGenerator map = MapGenerator.instance;
        cellList.Clear();
        map.worldGraph.CircularBFS(cellIndex, Mathf.CeilToInt(radius / MapGenerator.instance.cellSize), (int x, int y) =>
        {
            if (map.IsAreaPlayable(x, y) && !BuildGrid.instance.GetIfBusy(x, y))
                cellList.Add(new Vector2Int(x, y));
        });
    }
    protected override void Update()
    {
        base.Update();
    }

    [SerializeField] private int minZombies = 3;
    [SerializeField] private int maxZombies = 3;


    [SerializeField] private float spawnRadius = 30;

    [SerializeField] private int maxHeat = 18;

    private int heat = 0;
    [SerializeField] private float overheatTime = 3;
    [SerializeField] private float heatCooloffRate = 1;

    [SerializeField] protected ZombieUnit.state zombieType;
    private IEnumerator SpawnZombies()
    {
        while (true)
        {
            if (heat >= maxHeat) // (GameHandler.instance.waveCount * 0.5f)
            {
                yield return new WaitForSeconds(overheatTime);
                heat = 0;

            }
            else
                yield return new WaitForSeconds(cooldown);

            if (GameHandler.instance.currentPhase != GameHandler.phase.fight && type == BuildGrid.type.harasserHive)
                continue;


            int zombieAmount = UnityEngine.Random.Range(minZombies, maxZombies);
            for (int i = 0; i < zombieAmount; ++i)
            {
                SpawnZombie(zombieType);
                //unit.onDie += (int x) => { zombieCount--; };
            }


        }

    }
    protected virtual ZombieUnit SpawnZombie(ZombieUnit.state zombieType)
    {
        if (spawnCells.Count <= 0) return null;
        heat++;

        return ZombieUnit.Spawn(zombieType, GetSpawnPosition(), this);


        Vector2 GetSpawnPosition()
        {
            Vector2Int index = UtilContainers.GetRandomItem(spawnCells);
            Vector2 tileCenter = BuildGrid.instance.grid.GetCellCenter(index.x, index.y);
            return UtilMath.GetRandomTilePosition(tileCenter, MapGenerator.instance.cellSize);
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






    public Vector2[,] backToHiveMap { get; protected set; } //should be in buildup hive
    protected virtual void GenerateBackToHiveMap()
    {
        Graph worldGraph = MapGenerator.instance.worldGraph;

        Dictionary<Vector2Int, float> sources = new();
        foreach (Vector2Int coords in spawnCells)
            sources[coords] = 0;


        float[,] costMap = worldGraph.DijkstraMap(sources, (int x, int y) => false);
        backToHiveMap = worldGraph.BuildDirectionMap(costMap, (int x, int y) => false);





    }
    protected override void Die(Entity dealer)
    {

        GameHandler.instance.AddPlayerBudget(Buildable.GetReward(type));
        //Debug.Log("reward! " + Buildable.GetReward(type));
        base.Die(dealer);
    }
}
