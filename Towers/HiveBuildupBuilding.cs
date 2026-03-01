using UnityEngine;
using System.Collections.Generic;
public class HiveBuildupBuilding : HiveBuilding
{
    [field: SerializeField] public float patrolRadius { get; private set; } = 40;

    //private int zombieCount = 0;
    public List<ZombieUnit> defenders { get; private set; } = new();
    [SerializeField] private int maxDefenderCount = 12;
    [SerializeField] private int maxDefenderCountDistortion = 2;
    public List<Vector2Int> patrolCells { get; private set; } = new List<Vector2Int>();
    protected override void Start()
    {
        base.Start();
        zombieType = ZombieUnit.state.defend;
        maxDefenderCount += UnityEngine.Random.Range(-maxDefenderCountDistortion, maxDefenderCountDistortion + 1);
    }
    protected override void UpdateHiveCells()
    {
        base.UpdateHiveCells();
        UpdateCells(patrolCells, patrolRadius);

    }


    protected override ZombieUnit SpawnZombie(ZombieUnit.state zombieType)
    {
        if (defenders.Count >= maxDefenderCount)
        {
            if (GameHandler.instance.currentWandererCount < GameHandler.instance.maxWandererCount)
                base.SpawnZombie(ZombieUnit.state.wander);
            return null;
        }
        ZombieUnit unit = base.SpawnZombie(zombieType);
        if (unit == null) return null;
        defenders.Add(unit);
        unit.onDie += (int x) => defenders.Remove(unit);

        return unit;
    }

    public Vector2[,] wanderMap { get; private set; }
    //public Vector2[,] defendMap { get; private set; }
    protected override void GenerateBackToHiveMap()
    {
        Graph worldGraph = MapGenerator.instance.worldGraph;

        Dictionary<Vector2Int, float> sources = new();
        foreach (Vector2Int coords in patrolCells)
            sources[coords] = 0;


        float[,] costMap = worldGraph.DijkstraMap(sources, (int x, int y) => false);
        backToHiveMap = worldGraph.BuildDirectionMap(costMap, (int x, int y) => false);


        Vector2[,] map1 = new Vector2[MapGenerator.instance.playableAreaWidth, MapGenerator.instance.playableAreaHeight];
        Vector2[,] map2 = Graph.Invert(backToHiveMap);
        foreach (Vector2Int coords in patrolCells)
        {
            map1[coords.x, coords.y] = backToHiveMap[coords.x, coords.y];
            map2[coords.x, coords.y] = Vector2.zero;
        }


        wanderMap = Graph.Add(MapGenerator.instance.backToMapFlowField, Graph.Invert(map1)); //build a map such that goes out of hive on nearby cells, zero otherwise 
        //defendMap = Graph.Add(MapGenerator.instance.backToMapFlowField, map2); //double force outside of map
    }

    protected override void Die()
    {
        base.Die();

        Horde horde = new Horde();

        foreach (var z in defenders)
        {
            z.EnterHordeState(horde);
        }

    }

    public void DissolveDefenders()
    {
        Horde horde = new Horde();

        foreach (var z in defenders)
        {
            z.EnterHordeState(horde);
        }
        defenders.Clear();
    }

}
