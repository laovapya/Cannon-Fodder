using System;
using UnityEngine;
using System.Collections.Generic;
public class TerraformerBuilding : Building
{
    private bool isHive => type == BuildGrid.type.reclaimerHive;
    [SerializeField] private int territoryRadiusAdjacent = 5;
    [SerializeField] private int territoryRadiusisolated = 2;
    private int maxCellsProcessed;
    private TerritoryGrid.type[] stageToTerritory;

    protected override void Start()
    {
        base.Start();
        corruptionTime = 999;
        nextCaptureTime = Time.time + cooldown;

        if (!isHive)
        {

            stageToTerritory = new TerritoryGrid.type[]
          {
            TerritoryGrid.type.ally,
            TerritoryGrid.type.neutral,
            TerritoryGrid.type.enemy
          };
            ordering = new Dictionary<TerritoryGrid.type, int>()
    {
        { TerritoryGrid.type.ally, 1 },
        { TerritoryGrid.type.neutral, 2 },
        { TerritoryGrid.type.enemy, 3 }};
        }
        else
        {
            stageToTerritory = new TerritoryGrid.type[]
                      {
            TerritoryGrid.type.enemy,
            TerritoryGrid.type.neutral,
            TerritoryGrid.type.ally
                      };
            ordering = new Dictionary<TerritoryGrid.type, int>()
    {
        { TerritoryGrid.type.ally, 3 },
        { TerritoryGrid.type.neutral, 2 },
        { TerritoryGrid.type.enemy, 1 }};
        }
        captureRadius = TerritoryGrid.instance.grid.GetValue(cell.x, cell.y) == TerritoryGrid.type.ally ? territoryRadiusAdjacent : territoryRadiusisolated; //set equal for reclaimer hive

        maxCellsProcessed = CountCellsInRadius(captureRadius, 7f);





        var sources = new Dictionary<Vector2Int, float> { { new Vector2Int(cell.x, cell.y), 0f } };
        distanceMap = MapGenerator.instance.worldGraph.DijkstraMap(sources, (x, y) => false);
        layers = MapGenerator.instance.worldGraph.GetDistanceLayers(
           distanceMap,
           new HashSet<Vector2Int>(),
           layerStep: 10f,
           fudge: 0f,
       maxDistance: 200
       );


        healthUseDelta = Mathf.CeilToInt(maxHealth / (GetLayerCount() * 2));

        int GetLayerCount()
        {
            int cellsProcessed = 0;
            int layerCount = 0;
            foreach (var layer in layers)
            {
                layerCount++;
                foreach (var pos in layer)
                    cellsProcessed++;
                if (cellsProcessed > maxCellsProcessed)
                    break;
            }
            return layerCount;
        }


        // PrecomputeDistanceOrdering();


        int CountCellsInRadius(int radius, float fudge = 0)
        {
            int count = 0;
            int maxOffset = captureRadius; // maximum dx or dy to consider

            for (int dx = -maxOffset; dx <= maxOffset; dx++)
            {
                for (int dy = -maxOffset; dy <= maxOffset; dy++)
                {
                    int absX = Mathf.Abs(dx);
                    int absY = Mathf.Abs(dy);

                    // Octile / linear combination distance
                    int distance = 14 * Mathf.Min(absX, absY) + 10 * Mathf.Abs(absX - absY);

                    if (distance <= captureRadius * 10 + fudge) // captureRadius scaled by 10
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }


    private int captureRadius;
    private float nextCaptureTime = 0;
    private int healthUseDelta;
    protected override void Update()
    {
        base.Update();
        if (Time.time >= nextCaptureTime)
        {
            nextCaptureTime = Time.time + cooldown;
            FillArea(CheckBorders() ? stageToTerritory[1] : stageToTerritory[0]); //
            if (!isHive) AddHealth(-healthUseDelta);
        }
    }

    float[,] distanceMap;
    List<List<Vector2Int>> layers;

    private Dictionary<TerritoryGrid.type, int> ordering;

    //HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    private void FillArea(TerritoryGrid.type targetStage)
    {
        int cellsProcessed = 0;
        //int newlyVisitedCellsCount = 0;
        foreach (var layer in layers)
        {
            bool capturedThisLayer = false;

            foreach (var pos in layer)
            {
                // if (!visited.Contains(pos))
                //     newlyVisitedCellsCount++;
                // visited.Add(pos);
                // if (visited.Count >= maxCellsProcessed)
                //     visited.Clear();

                cellsProcessed++;
                if (cellsProcessed > maxCellsProcessed)
                    return;

                var currentType = TerritoryGrid.instance.grid.GetValue(pos.x, pos.y);

                if (ordering[currentType] > ordering[targetStage])
                {
                    TerritoryGrid.instance.CaptureCell(pos.x, pos.y, targetStage);
                    capturedThisLayer = true;
                }

                //visitedCells.Add(pos);
            }

            if (capturedThisLayer)
                return; // Stop after capturing one layer
        }
    }
    private bool CheckBorders()
    {
        int cellsProcessed = 0;
        foreach (var layer in layers)
        {
            foreach (var pos in layer)
            {
                cellsProcessed++;
                if (cellsProcessed > maxCellsProcessed)
                    return false; // Stop if we reach the process limit

                var currentType = TerritoryGrid.instance.grid.GetValue(pos.x, pos.y);

                // Check border: enemy for normal, ally for hive
                if ((!isHive && currentType == TerritoryGrid.type.enemy) ||
                    (isHive && currentType == TerritoryGrid.type.ally))
                {
                    return true;
                }
            }
        }
        return false;
    }


}
