using UnityEngine;
using System;
using System.Collections.Generic;

public class PathGrid : MonoBehaviour
{
    public static PathGrid instance;
    private void Awake()
    {
        instance = this;
        worldGraph = MapGenerator.instance.worldGraph;

        int width = BuildGrid.instance.width;
        int height = BuildGrid.instance.height;
        int cellSize = BuildGrid.instance.cellSize;
        Vector2 origin = BuildGrid.instance.origin;
        grid = new Grid<PathNode>(width, height, cellSize, origin);


        worldGraph.ForEachPlayableCell((int i, int j) =>
        {
            grid.SetValue(i, j, new PathNode(i, j, grid.GetCellCenter(i, j), false, true));
        });

        SetNeighbors();

        //InitProximityMap();
        zombiePreferenceMap = new float[instance.grid.width, instance.grid.height];

        BuildGrid.instance.onBuild += (BuildGrid.type type, int x, int y) => { if (type != BuildGrid.type.barricade) UpdateGrid(); };
        Entity.onZombieAttackersChanged += UpdateGrid;
        ;
        //GameHandler.instance.onGameStart += UpdateProximityMap;
    }
    public Grid<PathNode> grid;
    //public GridGraph<PathNode> gridGraph;

    private Graph worldGraph;// = MapGenerator.instance.worldGraph;
    private void SetNeighbors()
    {
        worldGraph.ForEachPlayableCell((int i, int j) =>
        {
            PathNode node = grid.GetValue(i, j);
            if (node == null)
                return;

            GraphCell worldCell = worldGraph.graph[i, j];
            foreach (var neighborCell in worldCell.neighbors)
            {
                PathNode neighbor = grid.GetValue(neighborCell.x, neighborCell.y);
                if (neighbor != null)
                    node.neighbors.Add(neighbor);
            }
        });
    }

    public Action onGridUpdate;
    private void UpdateGrid() //BuildGrid.type buildType, int x, int y
    {
        //SetBusy(buildType, x, y);
        UpdateProximityMap();


        onGridUpdate?.Invoke();
    }
    // private void SetBusy(BuildGrid.type buildType, int x, int y)
    // {
    //     PathNode node = grid.GetValue(x, y);
    //     if (node == null)
    //     {
    //         return;
    //     }
    //     if (buildType != BuildGrid.type.empty)
    //     {

    //         node.isBusy = true;
    //         node.isWalkable = false;
    //     }
    //     else
    //     {

    //         node.isBusy = false;
    //         node.isWalkable = true;
    //     }
    // }


    public Vector2Int[,] nextMap;
    public Vector2[,] directionMap;
    public float[,] zombiePreferenceMap; //zombies may prefer to attack "barracks" or something, but the priorities for winning are different 
    //public int[,] pathSafafetyMap;
    public Vector2Int[,] targetMap;



    public void UpdateProximityMap()
    {
        float GetCost(Building building) //list must match exactly the building.type enum 
        {
            if (building == null) return 0;
            BuildGrid.type type = building.type;
            switch (type)
            {
                default: return 0;
                case BuildGrid.type.decoy:
                    return 100;
                case BuildGrid.type.citadel:
                    return 100;
                case BuildGrid.type.barracks:
                case BuildGrid.type.powerplant:
                case BuildGrid.type.terraformer:
                    return 110;
                case BuildGrid.type.turret:
                case BuildGrid.type.artillery:
                case BuildGrid.type.flamethrower:
                    return 130;
                    // case BuildGrid.type.barricade:
                    //     return 150;
            }
        }
        //zombies attack everything, every building must be a target, specify lower priorities if you want them to skip
        //if adding mountains/barriers/undestructubles, have to change the whole logic. 
        //HashSet<Vector2Int> sources = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, float> sources = new Dictionary<Vector2Int, float>();
        for (int i = 0; i < grid.width; ++i)

            for (int j = 0; j < grid.height; ++j)
            {
                Building b = BuildGrid.instance.grid.GetValue(i, j);
                if (b != null
                && b.type != BuildGrid.type.barricade //make 2 maps, 1 with barricades included, make some zombies avoid them.
                && (!b.HasEnoughAttackers()) // || b.type == BuildGrid.type.citadel
                && MaskProcessing.instance.LayerInMask(b.transform.gameObject.layer, MaskProcessing.instance.ally))
                    sources[new Vector2Int(i, j)] = GetCost(b);
            }
        if (sources.Count <= 0)
        {
            Debug.Log("no sources for zombie proximity map");
            //return;
        }
        zombiePreferenceMap = worldGraph.DijkstraMap(sources,
        (int i, int j) =>
        {
            var cell = BuildGrid.instance.grid.GetValue(i, j);
            return cell == null
                || cell.type == BuildGrid.type.highground
                || MaskProcessing.instance.LayerInMask(
                    cell.transform.gameObject.layer,
                    MaskProcessing.instance.enemy
                );
        }
        );



        directionMap = worldGraph.BuildDirectionMap(zombiePreferenceMap, (x, y) => !BuildGrid.instance.GetIfWalkable(x, y));
        nextMap = worldGraph.BuildNextMap(zombiePreferenceMap, (x, y) => !BuildGrid.instance.GetIfWalkable(x, y));
        //Graph.DrawProximityMap(zombiePreferenceMap, true);
    }
}
