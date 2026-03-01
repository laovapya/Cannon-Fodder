using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class GameState
{
    private bool[,] isAIbuildable;// { get; private set; }
    public GameState()
    {
        isAIbuildable = new bool[worldGraph.width, worldGraph.height];
        pathDanger = new float[worldGraph.width, worldGraph.height];
        pathZombiePresence = new float[worldGraph.width, worldGraph.height];
        zombiePresence = new int[worldGraph.width, worldGraph.height];
        nearbyDanger = new float[worldGraph.width, worldGraph.height];
        ResourceCitadelCostMap = new float[worldGraph.width, worldGraph.height];
        pathLength = new float[worldGraph.width, worldGraph.height];
        pathDangerousPartLength = new float[worldGraph.width, worldGraph.height];
        cellTarget = new Vector2Int[worldGraph.width, worldGraph.height];
        cellPressure = new float[worldGraph.width, worldGraph.height];
        cellDanger = new int[worldGraph.width, worldGraph.height];
        frontlineDistances = new int[worldGraph.width, worldGraph.height];
        neutralFrontlineDistances = new int[worldGraph.width, worldGraph.height];
        hivePresence = new float[worldGraph.width, worldGraph.height];
        isNearReclaimer = new bool[worldGraph.width, worldGraph.height];
        isNearResource = new bool[worldGraph.width, worldGraph.height];
        enclosureMap = ComputeEnclosureMap(4);
        //Graph.DrawProximityMap(enclosureMap, false);
        citadelMainhiveDistance = Vector2.Distance(MapGenerator.instance.citadelPos, MapGenerator.instance.mainHivePos);

        debugMap = new int[worldGraph.width, worldGraph.height];
        for (int x = 0; x < worldGraph.width; x++)
            for (int y = 0; y < worldGraph.height; y++)
            {
                pathDanger[x, y] = -1;
                cellTarget[x, y] = new Vector2Int(-1, -1);
            }

        for (int x = 0; x < worldGraph.width; x++)
            for (int y = 0; y < worldGraph.height; y++)
                ResourceCitadelCostMap[x, y] = int.MaxValue;
        List<Vector2Int> sources = new();
        for (int x = 0; x < worldGraph.width; x++)
            for (int y = 0; y < worldGraph.height; y++)
                if (MapGenerator.instance.isResourceCell[x, y])
                    sources.Add(new Vector2Int(x, y));
        int xcitadel, ycitadel;
        BuildGrid.instance.grid.GetClampedXY(MapGenerator.instance.citadelPos, out xcitadel, out ycitadel);
        sources.Add(new Vector2Int(xcitadel, ycitadel));
        MapGenerator.instance.worldGraph.BFSdistance(sources, (int x, int y, int distance) => { ResourceCitadelCostMap[x, y] = distance; });


    }
    //game state from the enemy ai perspective 


    //danger means how many player things can shoot this cell



    //economy, isolated buildings, base defense/health,

    //economy
    public float playerBudget { get; private set; }
    public float enemyBudget { get; private set; }


    //territory
    public float powercellsCaptured { get; private set; } // number of captured power cells (not built power stations?)
    //public float powercellExpansion { get; private set; } // progress towards uncaptured power cells (0..1 + 0..1 + 0..1)
    public List<Vector2Int> powercellsUncaptured = new List<Vector2Int>(); //uncaptured power cells ranked by frontline progress towards them






    //isolation
    //public float buildingIsolation { get; private set; } // how many isolated buildings (some threshold of cell safety to count cell as isolated?)

    public List<Vector2Int> undefendedBuildingCells = new List<Vector2Int>(); //cells of isolated buildings (except citadel) ranked by safety index, power stations have a little higher priority?
    //(if failed to build harasser near first, move to next)
    private float isolationDangerThreshold = 20;
    //private float isolationSafetyThreshold = 20;

    //public float hiveIsolation { get; private set; } //how many hives around, place defender hives in more dangerous cells nearby

    //public List<Vector2Int> isolatedHiveCells;




    //citadel/main hive


    public List<Vector2Int> mainhiveDefenseCells = new List<Vector2Int>(); //available cells to build around main hive


    public float citadelDanger { get; private set; }
    public float citadelHealth { get; private set; }

    public int[,] frontlineDistances { get; private set; }
    public int frontlinePerimeter { get; private set; }
    public int[,] cellDanger; //how many player things can shoot this cell 


    public Vector2Int[,] cellTarget { get; private set; }
    public float[,] cellPressure { get; private set; } //how many hives atttack this cell
    public float[,] pathDanger { get; private set; } //accumulating cellDanger along the path to the target 
    public float[,] pathZombiePresence { get; private set; }
    public int[,] zombiePresence { get; private set; }
    public float[,] nearbyDanger { get; private set; }
    public const float maxNearbyDanger = 300;
    public float[,] pathLength { get; private set; }
    public float[,] pathDangerousPartLength { get; private set; }
    //private int[,] cellSafety; //how many hives around this cell

    public int[,] debugMap { get; private set; }
    private Graph worldGraph = MapGenerator.instance.worldGraph;


    public float citadelMainhiveDistance { get; private set; }


    public int zombieCount { get; private set; }
    public float territoryPlayerEnemyRatio { get; private set; }

    public void TakeSnapshot()
    {
        UpdateAIbuildableMap();
        playerBudget = GameHandler.instance.playerBudget;
        enemyBudget = GameHandler.instance.enemyBudget;
        UpdateFrontlineDistances(); //1
        UpdateFrontlinePerimeter(); //2
        UpdateNeutralFrontlineDistance();
        UpdateCellDanger(); //2


        UpdatePresence();
        //Graph.DrawProximityMap(hivePresence, true);
        UpdateResourcePresence();
        UpdateReclaimerPresence();


        UpdatePowercells();
        UpdateUndefendedBuildings();

        UpdatePaths(); //3


        UpdateCitadelState();
        zombieCount = ZombieUnit.allZombies.Count;
        territoryPlayerEnemyRatio = (float)TerritoryGrid.instance.GetAllyCellCount() / (float)TerritoryGrid.instance.GetEnemyCellCount();

        // UpdateTurtlingCoefficient();


        UpdateSurroundCoefficient(); //4
        UpdatePlayerDefenseCoefficient(); //5

        //UpdateFulfillment();
        UpdateHiveRemoteness();

        enemyTerritoryPercent = TerritoryGrid.instance.GetEnemyCellCount() / (float)MapGenerator.instance.playableAreaCellCount;
    }
    public void UpdateOnBuild()
    {
        UpdateMainhiveState();

        UpdatePresence();

        UpdateReclaimerPresence();
        UpdateAIbuildableMap();

        //UpdateFulfillment();

        UpdateHiveRemoteness();
    }
    // private void UpdateFulfillment()
    // {
    //     defenderHiveFulfilment = Mathf.Clamp01((float)AIdirector.instance.defenderHiveCount / AIdirector.instance.desiredDefenderCount);
    //     blockerHiveFulfilment = Mathf.Clamp01((float)AIdirector.instance.blockerHiveCount / AIdirector.instance.desiredBlockerCount);
    //     reclaimerHiveFulfilment = Mathf.Clamp01((float)AIdirector.instance.reclaimerHiveCount / AIdirector.instance.desiredReclaimerCount);
    //     turningHiveFulfilment = Mathf.Clamp01((float)AIdirector.instance.turningHiveCount / AIdirector.instance.desiredTurningCount);
    // }
    private void UpdateAIbuildableMap()
    {
        Array.Clear(isAIbuildable, 0, isAIbuildable.Length);
        worldGraph.ForEachPlayableCell((int i, int j) =>
        {
            isAIbuildable[i, j] = TerritoryGrid.instance.grid.GetValue(i, j) == TerritoryGrid.type.enemy && !BuildGrid.instance.GetIfBusy(i, j);
        });
    }
    private void UpdateCitadelState()
    {
        citadelHealth = GameHandler.instance.citadel.health;
        int x, y;
        BuildGrid.instance.grid.GetClampedXY(MapGenerator.instance.citadelPos, out x, out y);
        citadelDanger = cellDanger[x, y];


    }



    private void UpdateFrontlineDistances()
    {
        frontlineDistances = TerritoryGrid.instance.GetFrontlineDistances();
    }
    public int[,] neutralFrontlineDistances { get; private set; }
    private void UpdateNeutralFrontlineDistance()
    {
        neutralFrontlineDistances = TerritoryGrid.instance.GetNeutralFrontlineDistances();
    }

    private void UpdateFrontlinePerimeter()
    {
        frontlinePerimeter = 0;
        for (int x = 0; x < frontlineDistances.GetLength(0); ++x)
            for (int y = 0; y < frontlineDistances.GetLength(1); ++y)
            {
                if (frontlineDistances[x, y] == 10) frontlinePerimeter++;
            }
    }
    //private void UpdateCellSafety()
    //{
    //    List<Vector2Int> attackingHives = new List<Vector2Int>();
    //    Grid<Building> buildGrid = BuildGrid.instance.grid;
    //    int[,] distances = new int[buildGrid.width, buildGrid.height];
    //    MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
    //    {
    //        distances[x, y] = int.MaxValue;
    //        BuildGrid.type cellType = buildGrid.GetValue(x, y).type;
    //        if (cellType == BuildGrid.type.buildupHive || cellType == BuildGrid.type.harasserHive)
    //            attackingHives.Add(new Vector2Int(x, y));
    //    });



    //    //same weight for harasser and buildup for now. 
    //    MapGenerator.instance.worldGraph.BFSdistance(attackingHives, (int x, int y, int distance) => { distances[x, y] = distance; });
    //    //return distances;
    //    cellSafety = distances;
    //}
    private float soldierDanger = 0.5f;
    private int barricadeDanger = 3;
    private int turretDanger = 10;
    private int cannonDanger = 15;

    public int maxCellDanger { get; private set; } = 90;
    //public int avgCellDanger { get; private set; } = 10;
    private void UpdateCellDanger() //add potential threat from soldiers moving 
    {
        Array.Clear(cellDanger, 0, cellDanger.Length);
        Grid<Building> buildGrid = BuildGrid.instance.grid;

        worldGraph.ForEachPlayableCell((int i, int j) =>
        {
            Vector2Int center = new Vector2Int(i, j);
            Building b = buildGrid.GetValue(i, j);
            if (b.type == BuildGrid.type.turret) Fill(center, (int)(((b as ShooterBuilding).maxRange + 10) / MapGenerator.instance.cellSize), true, turretDanger);
            else if (b.type == BuildGrid.type.artillery) Fill(center, (int)(((b as ShooterBuilding).maxRange + 10) / MapGenerator.instance.cellSize), false, cannonDanger);
            else if (b.type == BuildGrid.type.barricade) cellDanger[i, j] += barricadeDanger;
            int soldierCount = SoldierGrid.instance.soldierGrid.GetValue(i, j).Count;
            if (soldierCount > 0) Fill(center, (int)SoldierUnit.attackRange, true, Mathf.FloorToInt(soldierCount * soldierDanger));
        });
        // void Fill(Vector2Int center, int maxRadius, bool isBlockable, int danger)
        // {
        //     HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        //     //Vector2 centerWorld = grid.GetCellCenter(center.x, center.y);


        //     float delta = 1f / (maxRadius * 2); //radians, more rays for bigger radius 

        //     for (float angleRad = 0f; angleRad < 2 * Mathf.PI; angleRad += delta)
        //     {
        //         for (int r = 1; r <= maxRadius; r++)
        //         {
        //             int x = Mathf.RoundToInt(center.x + r * Mathf.Cos(angleRad));
        //             int y = Mathf.RoundToInt(center.y + r * Mathf.Sin(angleRad));


        //             Building cell = buildGrid.GetValue(x, y);

        //             if (!MapGenerator.instance.IsAreaPlayable(x, y) || (BuildGrid.instance.GetIfBusy(x, y) && isBlockable)) //ray hits building, further area invisible
        //                 break;

        //             if (visited.Add(new Vector2Int(x, y)))
        //                 cellDanger[x, y] += danger;
        //         }
        //     }
        // }
        void Fill(Vector2Int center, int maxRadius, bool isBlockable, int danger)
        {
            Grid<Building> buildGrid = BuildGrid.instance.grid;
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                for (int dy = -maxRadius; dy <= maxRadius; dy++)
                {
                    int x = center.x + dx;
                    int y = center.y + dy;
                    if (!MapGenerator.instance.IsAreaPlayable(x, y))
                        continue;

                    if (dx * dx + dy * dy > maxRadius * maxRadius)
                        continue; // outside circle

                    if (worldGraph.RaycastBresenham(center, new Vector2Int(x, y), (int x, int y) =>
                    {
                        bool blocked = !(x == center.x && y == center.y) && BuildGrid.instance.GetIfBusy(x, y) && isBlockable;
                        if (visited.Add(new Vector2Int(x, y)) && !blocked)
                            cellDanger[x, y] += danger;
                        return blocked;
                    }))
                        continue;


                }
            }
        }
    }
    private int shortPathLength = 8;
    private void UpdatePaths()
    {
        //Array.Clear(debugMap, 0, debugMap.Length);
        Array.Clear(pathDanger, 0, pathDanger.Length);
        Array.Clear(cellPressure, 0, cellPressure.Length);
        Array.Clear(pathZombiePresence, 0, pathZombiePresence.Length);
        Array.Clear(pathDangerousPartLength, 0, pathDangerousPartLength.Length);
        for (int i = 0; i < worldGraph.width; ++i)
            for (int j = 0; j < worldGraph.height; ++j)
            {
                if (!isAIbuildable[i, j])
                    continue;

                List<PathNode> path = Pathfinding.GetSlopePathNodes(i, j);

                if (path == null || path.Count == 0)
                {
                    Debug.Log("not path");
                    pathDanger[i, j] = -1;
                    nearbyDanger[i, j] = -1;
                    cellTarget[i, j] = new Vector2Int(-1, -1);
                    //debugMap[i, j] = 10;
                    continue;
                }

                float dangerSum = cellDanger[path[0].x, path[0].y];
                float shortDangerSum = dangerSum;

                for (int k = 1; k < path.Count; k++)
                {
                    float penalty = 1;
                    var prev = path[k - 1];
                    var curr = path[k];
                    if (prev.x != curr.x && prev.y != curr.y)
                        penalty = 1.4f; // diagonal penalty

                    float cellD = cellDanger[path[k].x, path[k].y] * penalty;
                    dangerSum += cellD;
                    if (cellD > 0)
                        pathDangerousPartLength[i, j] += MapGenerator.instance.cellSize * penalty;
                    if (k < shortPathLength)
                        shortDangerSum += cellD;

                    pathZombiePresence[i, j] += hivePresence[path[k].x, path[k].y];
                }

                pathLength[i, j] = PathGrid.instance.zombiePreferenceMap[i, j];
                pathDanger[i, j] = dangerSum;
                nearbyDanger[i, j] = shortDangerSum;

                PathNode last = path[path.Count - 1];
                cellTarget[i, j] = new Vector2Int(last.x, last.y);
                if (BuildGrid.instance.grid.GetValue(cellTarget[i, j].x, cellTarget[i, j].y).type == BuildGrid.type.empty)
                    Debug.Log("empty target");
                if (BuildGrid.instance.grid.GetValue(i, j).type == BuildGrid.type.buildupHive) //not harasserHive, because they die on wave end and cannot be counted
                    cellPressure[last.x, last.y] += 1;
            }
    }


    private void UpdateUndefendedBuildings()
    {
        undefendedBuildingCells.Clear();
        //isolatedHiveCells.Clear();
        Grid<Building> buildGrid = BuildGrid.instance.grid;

        worldGraph.ForEachPlayableCell((int i, int j) =>
        {
            Building b = buildGrid.GetValue(i, j);
            switch (b.type)
            {
                case BuildGrid.type.turret:
                case BuildGrid.type.artillery:
                case BuildGrid.type.powerplant:
                case BuildGrid.type.terraformer:
                    if (cellDanger[i, j] <= isolationDangerThreshold)
                        undefendedBuildingCells.Add(new Vector2Int(i, j));
                    break;

                    //case BuildGrid.type.mainhive:
                    //case BuildGrid.type.harasserHive:
                    //case BuildGrid.type.reclaimerHive:
                    //    if (cellSafety[i, j] < isolationSafetyThreshold)
                    //        isolatedHiveCells.Add(new Vector2Int(i, j));
                    //    break;
            }
        });
        UtilContainers.Sort(undefendedBuildingCells, (Vector2Int cell) => { return cellDanger[cell.x, cell.y]; }); //cells of isolated buildings (except citadel) ranked by safety index
    }


    private void UpdatePowercells()
    {
        powercellsUncaptured.Clear();
        int count = 0;

        bool[,] isPowercell = MapGenerator.instance.isResourceCell;
        for (int i = 0; i < isPowercell.GetLength(0); ++i)
            for (int j = 0; j < isPowercell.GetLength(1); ++j)
                if (isPowercell[i, j])
                {
                    if (BuildGrid.instance.grid.GetValue(i, j).type == BuildGrid.type.powerplant)
                        count++;
                    else
                        powercellsUncaptured.Add(new Vector2Int(i, j));
                }
        powercellsCaptured = count;

        UtilContainers.Sort(powercellsUncaptured, (Vector2Int cell) => { return frontlineDistances[cell.x, cell.y]; }); //uncaptured power cells ranked by frontline progress towards them
    }
    public const int mainHiveDefenseRadius = 4;
    public const float maxMainhiveDefense = 6;
    public float mainhiveDefense { get; private set; } // how many hives/zombies around mainHive
    public float mainhiveHealth { get; private set; }
    public float mainhiveDanger { get; private set; } //how many things can attack mainHive
    public float mainHiveExpansion { get; private set; } // frontline to main hive distance 

    private void UpdateMainhiveState()
    {
        //mainhiveDefenseCells.Clear();
        mainhiveDefense = 0;
        int x, y;
        Grid<Building> grid = BuildGrid.instance.grid;
        grid.GetClampedXY(MapGenerator.instance.mainHivePos, out x, out y);
        worldGraph.CircularBFS(new Vector2Int(x, y), mainHiveDefenseRadius, (int x, int y) =>
        {
            Building b = grid.GetValue(x, y);
            if (b.type == BuildGrid.type.harasserHive || b.type == BuildGrid.type.buildupHive)
                mainhiveDefense++;
            // else
            //     mainhiveDefenseCells.Add(new Vector2Int(x, y));
        }
        );


        mainhiveHealth = GameHandler.instance.mainHive.health;
        mainhiveDanger = cellDanger[x, y];
        mainHiveExpansion = frontlineDistances[x, y];
    }


    public float[,] hivePresence;
    private int buildupHiveRadius = 4;
    private int harasserHiveRadius = 3;
    public float middlePresence = 3;
    public int maxHivePresence = 6;

    private void UpdatePresence()
    {
        Array.Clear(hivePresence, 0, hivePresence.Length);
        Array.Clear(zombiePresence, 0, zombiePresence.Length);
        Grid<Building> buildGrid = BuildGrid.instance.grid;

        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Building b = buildGrid.GetValue(x, y);
            if (b == null) return;

            int radius = 0;
            float strength = 0;

            switch (b.type)
            {
                case BuildGrid.type.buildupHive:
                    worldGraph.CircularBFS(new Vector2Int(x, y), 2, (int cx, int cy) => { zombiePresence[cx, cy] += 1; }); //2 is patrol radius
                    radius = buildupHiveRadius;
                    strength = 1;
                    break;

                case BuildGrid.type.harasserHive:
                    radius = harasserHiveRadius;
                    strength = 2;
                    break;
                case BuildGrid.type.reclaimerHive:
                    radius = 1;
                    strength = 1;
                    break;
                default:
                    return;
            }

            worldGraph.CircularBFS(new Vector2Int(x, y), radius, (int cx, int cy) =>
            {
                hivePresence[cx, cy] += strength;
            });

        });
    }
    public bool[,] isNearReclaimer { get; private set; }
    private void UpdateReclaimerPresence()
    {
        int radius = 3;
        Array.Clear(isNearReclaimer, 0, isNearReclaimer.Length);
        Grid<Building> buildGrid = BuildGrid.instance.grid;

        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Building b = buildGrid.GetValue(x, y);
            if (b == null) return;
            if (b.type != BuildGrid.type.reclaimerHive) return;

            worldGraph.CircularBFS(new Vector2Int(x, y), radius, (int cx, int cy) =>
            {
                isNearReclaimer[cx, cy] = true;
            });
        });
    }
    public bool[,] isNearResource { get; private set; }
    private void UpdateResourcePresence()
    {
        int radius = 3;
        Array.Clear(isNearResource, 0, isNearResource.Length);

        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            if (!MapGenerator.instance.isResourceCell[x, y]) return;

            worldGraph.CircularBFS(new Vector2Int(x, y), radius, (int cx, int cy) =>
            {
                isNearResource[cx, cy] = true;
            });
        });
    }
    public float[,] ResourceCitadelCostMap { get; private set; }


    // private float turtlingCoefficient = 0f;

    // private void UpdateTurtlingCoefficient()
    // {
    //     List<Vector2Int> allyCells = new List<Vector2Int>();
    //     TerritoryGrid.instance.ForEachAllyCell((int x, int y) =>
    //     {
    //         allyCells.Add(new Vector2Int(x, y));
    //     });

    //     if (allyCells.Count == 0)
    //     {
    //         turtlingCoefficient = 0f;
    //         return;
    //     }
    //     float mean = 0f;
    //     foreach (var cell in allyCells)
    //         mean += cellDanger[cell.x, cell.y];

    //     mean /= allyCells.Count;

    //     float variance = 0f;
    //     foreach (var cell in allyCells)
    //     {
    //         float d = cellDanger[cell.x, cell.y];
    //         variance += (d - mean) * (d - mean);
    //     }
    //     variance /= allyCells.Count;


    //     turtlingCoefficient = Mathf.Clamp01(Mathf.Sqrt(variance) / mean);
    //     Debug.Log("turtling coefficient: " + turtlingCoefficient + " mean danger: " + mean + " variance: " + variance);
    // }



    public float surroundCoefficient { get; private set; }

    private void UpdateSurroundCoefficient()
    {
        const int maxDirectionalCells = 3;
        int x, y;
        BuildGrid.instance.grid.GetClampedXY(MapGenerator.instance.citadelPos, out x, out y);
        int sectorCount = 16;
        int[] sectorOpen = new int[sectorCount];

        Vector2 citadelPos = new Vector2(x, y);

        TerritoryGrid.instance.ForEachEnemyCell((int cx, int cy) =>
        {
            Vector2 dir = new Vector2(cx - x, cy - y);
            if (Mathf.Min(neutralFrontlineDistances[cx, cy], frontlineDistances[cx, cy]) > maxDirectionalCells * 14) //14 is diagonal distance
                return;

            float angle = Mathf.Atan2(dir.y, dir.x);
            if (angle < 0) angle += 2 * Mathf.PI;

            int sector = Mathf.FloorToInt(angle / (2 * Mathf.PI) * sectorCount);
            sectorOpen[sector]++;
        });

        int total = 0;
        for (int i = 0; i < sectorCount; i++)
            total += Mathf.Clamp(sectorOpen[i], 0, maxDirectionalCells);

        surroundCoefficient = (float)total / (sectorCount * maxDirectionalCells);
        //Debug.Log("Surround coefficient: " + surroundCoefficient);
    }

    public float playerDefenseCoefficient { get; private set; }

    private void UpdatePlayerDefenseCoefficient()
    {
        float totalDefense = 0f;
        float count = 0f;

        TerritoryGrid.instance.ForEachEnemyCell((int cx, int cy) =>
        {
            if (Mathf.Min(neutralFrontlineDistances[cx, cy], frontlineDistances[cx, cy]) > 14)
                return;

            Vector2Int target = cellTarget[cx, cy];
            if (target.x == -1 || target.y == -1)
                return;

            float pathDanger_ = pathDanger[cx, cy];
            float cellPressure_ = cellPressure[target.x, target.y];

            float dangerFactor = cellPressure_ > 0 ? pathDanger_ / cellPressure_ : pathDanger_;

            dangerFactor = Mathf.Clamp01(dangerFactor / GetMaxPathDanger(cx, cy));

            totalDefense += dangerFactor;
            count++;
        });

        playerDefenseCoefficient = count > 0 ? totalDefense / count : 0f;




        //        Debug.Log("Player defense coefficient: " + playerDefenseCoefficient);
    }

    private float GetMaxPathDanger(int x, int y)
    {
        if (pathDangerousPartLength[x, y] <= 0)
            return maxCellDanger;
        return pathDangerousPartLength[x, y] / MapGenerator.instance.cellSize * maxCellDanger;
    }


    // public float defenderHiveFulfilment { get; private set; }
    // public float blockerHiveFulfilment { get; private set; }
    // public float reclaimerHiveFulfilment { get; private set; }
    // public float turningHiveFulfilment { get; private set; }

    private int maxSoldierCount = 16 * 10; //because 10
    public float soldierFulfilment
    {
        get
        {
            return Mathf.Clamp01((float)SoldierUnit.soldierCount / maxSoldierCount);
        }
    }

    public float enemyTerritoryPercent { get; private set; }




    public float[,] enclosureMap { get; private set; }

    public float[,] ComputeEnclosureMap(int maxRadius)
    {
        int width = worldGraph.width;
        int height = worldGraph.height;
        float[,] enclosureMap = new float[width, height];
        // Precompute  deltas 
        List<Vector2Int> deltas = new List<Vector2Int>();
        for (int dx = -maxRadius; dx <= maxRadius; dx++)
        {
            for (int dy = -maxRadius; dy <= maxRadius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (dx * dx + dy * dy > maxRadius * maxRadius) continue;
                deltas.Add(new Vector2Int(dx, dy));
            }
        }

        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Vector2Int center = new Vector2Int(x, y);
            int blockedCount = 0;

            foreach (var delta in deltas)
            {
                bool blocked = false;

                Vector2Int target = center + delta;

                blocked = worldGraph.RaycastBresenham(center, target, (int x, int y) => false);

                if (blocked) blockedCount++;
            }

            enclosureMap[x, y] = (float)blockedCount / deltas.Count;
        });

        return enclosureMap;
    }

    private float[,] hiveProximityMap;
    public float hiveRemoteness { get; private set; }
    private void UpdateHiveRemoteness()
    {
        Dictionary<Vector2Int, float> sources = new Dictionary<Vector2Int, float>();
        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Building b = BuildGrid.instance.grid.GetValue(x, y);
            if (b != null
            && MaskProcessing.instance.LayerInMask(b.transform.gameObject.layer, MaskProcessing.instance.enemy))
                sources[new Vector2Int(x, y)] = 0;
        });


        if (sources.Count <= 0)
        {
            Debug.Log("no sources for hive proximity map");
            //return;
        }
        hiveProximityMap = worldGraph.DijkstraMap(sources,
        (int i, int j) =>
        {
            var cell = BuildGrid.instance.grid.GetValue(i, j);
            return cell == null
                || cell.type == BuildGrid.type.highground
                || MaskProcessing.instance.LayerInMask(
                    cell.transform.gameObject.layer,
                    MaskProcessing.instance.ally
                );
        }
        );
        float maxHiveDistance = -1;
        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            if (hiveProximityMap[x, y] > maxHiveDistance)
            {
                maxHiveDistance = hiveProximityMap[x, y];
            }
        });
        hiveRemoteness = Mathf.Clamp01(maxHiveDistance / citadelMainhiveDistance);
    }
}
