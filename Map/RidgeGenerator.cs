using System;
using System.Collections.Generic;
using UnityEngine;

public static class RidgeGenerator
{
    /// <summary>
    /// Generates a binary ridge map based on Voronoi edges and height peaks.
    /// </summary>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <param name="gridSpacing">Spacing of Voronoi generator grid</param>
    /// <param name="removalChance">Chance to remove Voronoi generators</param>
    /// <param name="ridgeFatnessDistance">Threshold for edge thickness</param>
    /// <param name="heightThreshold">Minimum height to consider a cell a ridge</param>
    /// <param name="minPeakHeight">Minimum random peak height</param>
    /// <param name="peakHeightFalloffFactor">Falloff factor for heights from peaks</param>
    /// <param name="seed">Random seed</param>
    /// <returns>Binary ridge map</returns>
    public static bool[,] GetRidgeMap(
    int width,
    int height,
    int seedCount,               // desired number of Voronoi seeds
    float removalChance,
    float ridgeFatnessDistance,
    float heightThreshold,
    float minPeakHeight,
    float peakHeightFalloffFactor,
    int peakCount,
    int seed = 0)
    {
        System.Random rng = new System.Random(seed);

        // 1. Compute grid dimensions respecting map aspect ratio
        float aspectRatio = width / (float)height;
        int gridCols = Mathf.CeilToInt(Mathf.Sqrt(seedCount * aspectRatio));
        int gridRows = Mathf.CeilToInt(seedCount / (float)gridCols);

        float cellWidth = width / (float)gridCols;
        float cellHeight = cellWidth;// height / (float)gridRows;

        // 2. Generate Voronoi generators on the uniform grid
        List<Vector2> generators = new List<Vector2>();
        for (int gx = 0; gx < gridCols; gx++)
        {
            for (int gy = 0; gy < gridRows; gy++)
            {
                if (rng.NextDouble() > removalChance)
                {
                    // Base cell center
                    float x = (gx + 0.5f) * cellWidth;
                    float y = (gy + 0.5f) * cellHeight;

                    // Apply small random distortion within the cell
                    float maxOffsetX = cellWidth * 0.4f; // 40% of cell width
                    float maxOffsetY = cellHeight * 0.4f; // 40% of cell height
                    x += (float)(rng.NextDouble() * 2 - 1) * maxOffsetX;
                    y += (float)(rng.NextDouble() * 2 - 1) * maxOffsetY;

                    generators.Add(new Vector2(x, y));
                }
            }
        }

        if (generators.Count < 2)
            throw new Exception("Too few Voronoi generators after removal.");

        // 3. Compute ridge edges
        bool[,] ridgeMap = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float nearestDistance = float.MaxValue;
                float secondNearestDistance = float.MaxValue;
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                foreach (var g in generators)
                {
                    float dist = Vector2.SqrMagnitude(p - g);
                    if (dist < nearestDistance)
                    {
                        secondNearestDistance = nearestDistance;
                        nearestDistance = dist;
                    }
                    else if (dist < secondNearestDistance)
                    {
                        secondNearestDistance = dist;
                    }
                }

                ridgeMap[x, y] = (secondNearestDistance - nearestDistance) < ridgeFatnessDistance;
            }
        }




        float[,] heightMap = GenerateHeightMap(ridgeMap, peakCount, minPeakHeight, peakHeightFalloffFactor, seed);


        bool[,] result = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                result[x, y] = ridgeMap[x, y] && heightMap[x, y] >= heightThreshold;


        RemoveDiagonalIslands(result);

        return result;
    }


    private static float[,] GenerateHeightMap(
     bool[,] ridgeMap,
     int peakCount,          // desired number of peaks
     float minPeakHeight,
     float falloffFactor,
     int seed = 0)
    {
        int width = ridgeMap.GetLength(0);
        int height = ridgeMap.GetLength(1);
        System.Random rng = new System.Random(seed);

        // Collect all ridge cells
        List<Vector2Int> ridgeCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (ridgeMap[x, y])
                    ridgeCells.Add(new Vector2Int(x, y));

        if (ridgeCells.Count == 0)
            return new float[width, height];

        // Track which cells have been visited by any peak
        bool[,] visited = new bool[width, height];
        float[,] heightMap = new float[width, height];

        int remainingPeaks = Math.Min(peakCount, ridgeCells.Count);

        for (int peakIdx = 0; peakIdx < remainingPeaks; peakIdx++)
        {
            // Choose a random unvisited peak
            List<Vector2Int> unvisitedCells = new List<Vector2Int>();
            foreach (var cell in ridgeCells)
                if (!visited[cell.x, cell.y])
                    unvisitedCells.Add(cell);

            if (unvisitedCells.Count == 0)
                break;

            Vector2Int peak = unvisitedCells[rng.Next(unvisitedCells.Count)];
            float peakHeight = minPeakHeight + (float)rng.NextDouble() * (1f - minPeakHeight);

            // BFS queue
            Queue<(Vector2Int cell, float height)> queue = new Queue<(Vector2Int, float)>();
            queue.Enqueue((peak, peakHeight));

            while (queue.Count > 0)
            {
                var (current, h) = queue.Dequeue();

                if (h < 0)
                    continue;

                if (visited[current.x, current.y])
                {
                    // keep max height
                    heightMap[current.x, current.y] = Mathf.Max(heightMap[current.x, current.y], h);
                    continue;
                }

                visited[current.x, current.y] = true;
                heightMap[current.x, current.y] = Mathf.Max(heightMap[current.x, current.y], h);

                // Enqueue 4 neighbors (cardinal directions)
                Vector2Int[] neighbors = new Vector2Int[]
                {
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x, current.y + 1),
                new Vector2Int(current.x, current.y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height)
                    {
                        if (ridgeMap[n.x, n.y])
                            queue.Enqueue((n, h - falloffFactor));
                    }
                }
            }
        }

        return heightMap;
    }




    private static void RemoveDiagonalIslands(bool[,] map)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        // Copy so changes do not affect neighboring checks in same pass
        bool[,] original = (bool[,])map.Clone();

        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                bool a = original[x, y];
                bool b = original[x + 1, y];
                bool c = original[x, y + 1];
                bool d = original[x + 1, y + 1];

                // 1 0 / 0 1  OR  0 1 / 1 0
                bool diagonal1 = a && !b && !c && d;
                bool diagonal2 = !a && b && c && !d;

                if (diagonal1 || diagonal2)
                {
                    map[x, y] = false;
                    map[x + 1, y] = false;
                    map[x, y + 1] = false;
                    map[x + 1, y + 1] = false;
                }
            }
        }
    }

}
