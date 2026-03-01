using UnityEngine;
using System;
using System.Collections.Generic;

public class GraphCell : IHeapItem<GraphCell>
{
    public int heapIndex { get; set; }
    public int x;
    public int y;
    public List<GraphCell> neighbors = new List<GraphCell>();
    public GraphCell(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2Int coords => new Vector2Int(x, y);
    public static int diagCost = 14;
    public static int straightCost = 10;

    public int GetDistanceCost(GraphCell neighbor)
    {
        int xDistance = Mathf.Abs(neighbor.x - x);
        int yDistance = Mathf.Abs(neighbor.y - y);

        int diagonalSteps = Mathf.Min(xDistance, yDistance);
        int straightSteps = Mathf.Abs(xDistance - yDistance);

        return diagonalSteps * diagCost + straightSteps * straightCost;
    }
}

public class Graph
{
    public readonly int width;
    public readonly int height;
    public readonly GraphCell[,] graph;

    public void ForEachPlayableCell(Action<int, int> action)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (isCellPlayable(x, y)) action(x, y);
    }


    private Func<int, int, bool> isCellPlayable;

    public Graph(int width, int height, Func<int, int, bool> isCellPlayable)
    {
        this.width = width;
        this.height = height;
        this.isCellPlayable = isCellPlayable;

        graph = new GraphCell[width, height];

        // init cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                graph[x, y] = new GraphCell(x, y);
            }
        }

        // build neighbors
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (isCellPlayable(x, y))
                    SetNeighbors(graph[x, y], true);
            }
        }
    }

    private void SetNeighbors(GraphCell current, bool includeCorners)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                bool isDiagonal = dx != 0 && dy != 0;
                if ((dx == 0 && dy == 0) || (!includeCorners && isDiagonal)) //(dx + dy) % 2 == 0)
                    continue;

                int nx = current.x + dx;
                int ny = current.y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height && isCellPlayable(nx, ny) && (!isDiagonal || (isCellPlayable(nx, current.y) && isCellPlayable(current.x, ny))))
                {
                    current.neighbors.Add(graph[nx, ny]);
                }
            }
        }
    }

    public void BFS(Vector2Int start, Action<int, int> processCell)
    {
        Queue<GraphCell> queue = new Queue<GraphCell>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        var startCell = graph[start.x, start.y];
        queue.Enqueue(startCell);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            processCell(current.x, current.y);

            foreach (var neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor.coords))
                {
                    visited.Add(neighbor.coords);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
    public bool IsConnected()
    {
        int playableCount = 0;
        Vector2Int? start = null;

        ForEachPlayableCell((x, y) =>
        {
            playableCount++;
            if (start == null)
                start = new Vector2Int(x, y);
        });

        if (start == null)
            return false;

        int visitedCount = 0;

        BFS(start.Value, (x, y) =>
        {
            visitedCount++;
        });

        return visitedCount == playableCount;
    }

    public void BFS(Vector2Int start, Action<int, int> processCell, Func<GraphCell, bool> neighborCondition)
    {
        Queue<GraphCell> queue = new Queue<GraphCell>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        var startCell = graph[start.x, start.y];
        queue.Enqueue(startCell);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            processCell(current.x, current.y);

            foreach (var neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor.coords) && neighborCondition(neighbor))
                {
                    visited.Add(neighbor.coords);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }



    public void BFSdistance(List<Vector2Int> sources, Action<int, int, int> processCell)
    {
        Queue<GraphCell> queue = new Queue<GraphCell>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (var src in sources)
        {
            var startCell = graph[src.x, src.y];
            queue.Enqueue(startCell);
            visited.Add(src);
            distances[src] = 0;
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currPos = current.coords;
            int dist = distances[currPos];
            processCell(current.x, current.y, dist);

            foreach (var neighbor in current.neighbors)
            {
                var np = neighbor.coords;
                if (!visited.Contains(np))
                {
                    visited.Add(np);
                    int newDist = current.GetDistanceCost(neighbor) + dist;

                    distances[np] = newDist;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
    public void CircularBFS(Vector2Int start, int radius, Action<int, int> processCell, float fudge = 1)
    {
        Queue<GraphCell> queue = new Queue<GraphCell>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        if (start.x > graph.GetLength(0) || start.y > graph.GetLength(1) || start.x < 0 || start.y < 0) return;
        var startCell = graph[start.x, start.y];
        queue.Enqueue(startCell);
        visited.Add(start);


        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if ((current.coords - start).magnitude <= radius - fudge)
            {
                processCell(current.x, current.y);

                foreach (var neighbor in current.neighbors)
                {
                    if (!visited.Contains(neighbor.coords))
                    {
                        visited.Add(neighbor.coords);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
    }
    public void ConcentricBFS(Vector2Int start, Action<int, int> processCell, int radiusInCells)
    {
        if (radiusInCells < 2) radiusInCells = 2;

        Queue<GraphCell> queue = new Queue<GraphCell>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        var startCell = graph[start.x, start.y];
        queue.Enqueue(startCell);
        visited.Add(start);

        for (int currentRadius = 2; currentRadius <= radiusInCells; currentRadius++)
        {
            int count = queue.Count;
            for (int i = 0; i < count; i++)
            {
                var current = queue.Dequeue();
                if (Vector2Int.Distance(start, current.coords) > currentRadius)
                    continue;

                processCell(current.x, current.y);

                foreach (var neighbor in current.neighbors)
                {
                    if (!visited.Contains(neighbor.coords))
                    {
                        visited.Add(neighbor.coords);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
    }


    public static bool ConcentricBFS_WithLimit(
        Graph graph,
        Vector2Int start,
        int maxCellCount,
        float fudge,
        Func<int, int, bool> predicate)
    {
        Queue<GraphCell> queue = new Queue<GraphCell>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        var startCell = graph.graph[start.x, start.y];
        queue.Enqueue(startCell);
        visited.Add(start);

        int processedCells = 0;

        for (int currentRadius = 1; processedCells < maxCellCount; currentRadius++)
        {
            int count = queue.Count;

            for (int i = 0; i < count; i++)
            {
                var current = queue.Dequeue();
                processedCells++;

                if ((start - current.coords).magnitude > currentRadius - fudge)
                {
                    queue.Enqueue(current);
                    continue;
                }


                if (predicate(current.x, current.y))
                    return true;

                foreach (var neighbor in current.neighbors)
                {
                    if (!visited.Contains(neighbor.coords))
                    {
                        visited.Add(neighbor.coords);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return false;
    }

    // public float[,] DijkstraMap(HashSet<Vector2Int> sources, Action<int, int, float> processCell, Func<int, int, bool> shouldSkipNeighbor)
    // {
    //     float[,] distances = new float[width, height];
    //     bool[,] visited = new bool[width, height];

    //     for (int x = 0; x < width; ++x)
    //         for (int y = 0; y < height; ++y)
    //             distances[x, y] = float.MaxValue;


    //     BinHeap<Cell> open = new BinHeap<Cell>(width * height * 10, (Cell cell) => distances[cell.x, cell.y], true);

    //     foreach (var source in sources)
    //     {
    //         distances[source.x, source.y] = 0;
    //         open.Add(graph[source.x, source.y]);
    //     }

    //     while (open.Count > 0)
    //     {
    //         Cell current = open.RemoveFirst();
    //         visited[current.x, current.y] = true;

    //         float currentDist = distances[current.x, current.y];
    //         processCell(current.x, current.y, currentDist);

    //         foreach (var neighbor in current.neighbors)
    //         {
    //             if (visited[neighbor.x, neighbor.y] || shouldSkipNeighbor(neighbor.x, neighbor.y))
    //                 continue;

    //             float newDist = current.GetDistanceCost(neighbor) + currentDist;

    //             if (newDist < distances[neighbor.x, neighbor.y])
    //             {
    //                 distances[neighbor.x, neighbor.y] = newDist;
    //                 if (!open.Contains(neighbor))
    //                     open.Add(neighbor);
    //                 else
    //                     open.UpdateItem(neighbor);
    //             }
    //         }
    //     }
    //     return distances;
    // }
    public float[,] DijkstraMap(Dictionary<Vector2Int, float> sources, Func<int, int, bool> shouldSkipNeighbor)
    {
        float[,] distances = new float[width, height];
        bool[,] visited = new bool[width, height];
        Vector2Int?[,] cameFromSource = new Vector2Int?[width, height];

        for (int x = 0; x < width; ++x)
            for (int y = 0; y < height; ++y)
                distances[x, y] = float.MaxValue;

        BinHeap<GraphCell> open = new BinHeap<GraphCell>(width * height * 10, (GraphCell cell) => distances[cell.x, cell.y], true);

        foreach (var kvp in sources)
        {
            var source = kvp.Key;
            float cost = kvp.Value;
            distances[source.x, source.y] = 0; // heap key = path cost only
            cameFromSource[source.x, source.y] = source;
            open.Add(graph[source.x, source.y]);
        }

        while (open.Count > 0)
        {
            GraphCell current = open.RemoveFirst();
            visited[current.x, current.y] = true;

            Vector2Int origin = cameFromSource[current.x, current.y].Value;
            float totalDist = distances[current.x, current.y] + sources[origin]; // add source cost when reporting
            //processCell(current.x, current.y, totalDist);

            foreach (var neighbor in current.neighbors)
            {
                Building b = BuildGrid.instance.grid.GetValue(neighbor.x, neighbor.y);
                if (b == null)
                {
                    Debug.Log(neighbor.x + ", " + neighbor.y + " is null");
                }
                if (visited[neighbor.x, neighbor.y] || shouldSkipNeighbor(neighbor.x, neighbor.y))
                    continue;

                float newDist = current.GetDistanceCost(neighbor) + distances[current.x, current.y];

                if (newDist < distances[neighbor.x, neighbor.y])
                {
                    distances[neighbor.x, neighbor.y] = newDist;
                    cameFromSource[neighbor.x, neighbor.y] = origin;

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                    else
                        open.UpdateItem(neighbor);
                }
            }
        }

        // add source cost to all distances before returning
        for (int x = 0; x < width; ++x)
            for (int y = 0; y < height; ++y)
                if (cameFromSource[x, y].HasValue)
                    distances[x, y] += sources[cameFromSource[x, y].Value];

        return distances;
    }


    public void BuildDirectionAndNextMaps(
    float[,] costMap,
    Func<int, int, bool> isBlocked,
    out Vector2[,] directionMap,
    out Vector2Int[,] nextMap)
    {
        directionMap = new Vector2[width, height];
        nextMap = new Vector2Int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float cost = costMap[x, y];
                if (cost == float.MaxValue || isBlocked(x, y))
                {
                    directionMap[x, y] = Vector2.zero;
                    nextMap[x, y] = new Vector2Int(x, y);
                    continue;
                }

                float bestCost = cost;
                Vector2Int bestNext = new Vector2Int(x, y);

                foreach (var n in graph[x, y].neighbors)
                {

                    float nCost = costMap[n.x, n.y];
                    if (nCost < bestCost)
                    {
                        bestCost = nCost;
                        bestNext = new Vector2Int(n.x, n.y);
                    }
                }

                nextMap[x, y] = bestNext;
                directionMap[x, y] = (bestNext - new Vector2(x, y)).normalized;
            }
        }
    }
    public Vector2[,] BuildDirectionMap(
        float[,] costMap,
        Func<int, int, bool> isBlocked)
    {
        Vector2[,] directionMap = new Vector2[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float cost = costMap[x, y];
                // if (cost == float.MaxValue)
                // {
                //     directionMap[x, y] = Vector2.zero;

                //     continue;
                // }

                float bestCost = cost;
                Vector2Int bestNext = new Vector2Int(x, y);

                foreach (var n in graph[x, y].neighbors)
                {
                    //if (isBlocked(n.x, n.y)) continue;
                    float nCost = costMap[n.x, n.y];
                    if (nCost < bestCost)
                    {
                        bestCost = nCost;
                        bestNext = new Vector2Int(n.x, n.y);
                    }
                }


                directionMap[x, y] = (bestNext - new Vector2(x, y)).normalized;
            }
        }
        return directionMap;
    }
    public Vector2Int[,] BuildNextMap(
    float[,] costMap,
    Func<int, int, bool> isBlocked)
    {
        Vector2Int[,] nextMap = new Vector2Int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float cost = costMap[x, y];

                if (cost == float.MaxValue || isBlocked(x, y))
                {
                    nextMap[x, y] = new Vector2Int(x, y);
                    continue;
                }

                float bestCost = cost;
                Vector2Int bestNext = new Vector2Int(x, y);

                foreach (var n in graph[x, y].neighbors)
                {

                    float nCost = costMap[n.x, n.y];
                    if (nCost < bestCost)
                    {
                        bestCost = nCost;
                        bestNext = new Vector2Int(n.x, n.y);
                    }
                }

                nextMap[x, y] = bestNext;
            }
        }
        return nextMap;
    }


    public bool RaycastBresenham(Vector2Int start, Vector2Int end, Func<int, int, bool> F)
    {
        int x1 = start.x, y1 = start.y;
        int x2 = end.x, y2 = end.y;

        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int sx = (x1 < x2) ? 1 : -1;
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (!isCellPlayable(x1, y1))
                return true; // exited playable area

            if (F(x1, y1))
                return true;

            if (x1 == x2 && y1 == y2)
                break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x1 += sx; }
            if (e2 < dx) { err += dx; y1 += sy; }
        }

        return false;
    }
    // public bool RaycastBresenham(Vector2Int start, Vector2Int direction, int maxRadius, Func<int, int, bool> F)
    // {
    //     int x = start.x;
    //     int y = start.y;

    //     int dx = Mathf.Abs(direction.x);
    //     int dy = Mathf.Abs(direction.y);

    //     int sx = (direction.x >= 0) ? 1 : -1;
    //     int sy = (direction.y >= 0) ? 1 : -1;

    //     int err = dx - dy;

    //     for (int step = 0; step <= maxRadius; step++)
    //     {
    //         if (!isCellPlayable(x, y))
    //             return true;

    //         if (F(x, y))
    //             return true;

    //         if (dx == 0 && dy == 0)
    //         {
    //             x += sx;
    //             y += sy;
    //             continue;
    //         }

    //         int e2 = 2 * err;
    //         if (e2 > -dy) { err -= dy; x += sx; }
    //         if (e2 < dx) { err += dx; y += sy; }
    //     }

    //     return false;
    // }

    public bool RaycastBresenham(Vector2Int start, Vector2Int end, out Vector2Int hit, Func<int, int, bool> F)
    {
        int x1 = start.x, y1 = start.y;
        int x2 = end.x, y2 = end.y;
        hit = default;

        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int sx = (x1 < x2) ? 1 : -1;
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (!isCellPlayable(x1, y1))
                return true; // exited playable area

            if (F(x1, y1))
            {
                hit = new Vector2Int(x1, y1);
                return true;
            }

            if (x1 == x2 && y1 == y2)
                break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x1 += sx; }
            if (e2 < dx) { err += dx; y1 += sy; }
        }

        return false;
    }
    public static void DrawProximityMap(float[,] costs, bool inverted)
    {
        if (costs == null)
            return;


        float foundMax = 0;
        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            if (costs[x, y] < 9999) // ignore unvisited/infinity costs[x, y] != float.MaxValue
                foundMax = Mathf.Max(foundMax, costs[x, y]);
        });
        float maxCost = foundMax > 0 ? foundMax : 1;

        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Transform preview = GameObject.Instantiate(PrefabReference.instance.preview, BuildGrid.instance.grid.GetCellCenter(x, y), Quaternion.identity);
            float t = Mathf.Clamp01((float)costs[x, y] / maxCost);
            if (!inverted) t = 1 - t;

            float hue = Mathf.Lerp(0f, 0.83f, t);
            float value = Mathf.Lerp(1f, 0.2f, t);

            Color color = Color.HSVToRGB(hue, 1f, value);
            preview.GetComponent<Renderer>().material.color = color;
            preview.position = new Vector3(preview.position.x, preview.position.y, 100);
        });
    }



    public static float[,] Invert(float[,] map)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);
        float[,] result = new float[w, h];

        float max = 0f;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (map[x, y] < float.MaxValue)
                    max = Mathf.Max(max, map[x, y]);

        if (max <= 0) max = 1f;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                result[x, y] = (map[x, y] >= float.MaxValue) ? float.MaxValue : (max - map[x, y]);

        return result;
    }
    public static Vector2[,] Invert(Vector2[,] map)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        Vector2[,] result = new Vector2[w, h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                result[x, y] = -map[x, y];

        return result;
    }

    public static float[,] Add(float[,] a, float[,] b)
    {
        int w = a.GetLength(0);
        int h = a.GetLength(1);
        float[,] result = new float[w, h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float av = a[x, y];
                float bv = b[x, y];

                if (av >= float.MaxValue || bv >= float.MaxValue)
                    result[x, y] = float.MaxValue;
                else
                    result[x, y] = av + bv;
            }

        return result;
    }
    public static Vector2[,] Add(Vector2[,] a, Vector2[,] b)
    {
        int w = a.GetLength(0);
        int h = a.GetLength(1);

        Vector2[,] result = new Vector2[w, h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                result[x, y] = a[x, y] + b[x, y];

        return result;
    }
    public static void DrawProximityMap(int[,] costs, bool inverted)
    {
        if (costs == null)
            return;


        float foundMax = 0;
        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            if (costs[x, y] < 9999) // ignore unvisited/infinity costs[x, y] != float.MaxValue
                foundMax = Mathf.Max(foundMax, costs[x, y]);
        });
        float maxCost = foundMax > 0 ? foundMax : 1;

        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Transform preview = GameObject.Instantiate(PrefabReference.instance.preview, BuildGrid.instance.grid.GetCellCenter(x, y), Quaternion.identity);
            float t = Mathf.Clamp01((float)costs[x, y] / maxCost);
            if (!inverted) t = 1 - t;

            float hue = Mathf.Lerp(0f, 0.83f, t);
            float value = Mathf.Lerp(1f, 0.2f, t);

            Color color = Color.HSVToRGB(hue, 1f, value);
            preview.GetComponent<Renderer>().material.color = color;
            preview.position = new Vector3(preview.position.x, preview.position.y, 100);
        });
    }
    public static void DrawProximityMap(Vector2[,] vectors)
    {
        if (vectors == null)
        {
            Debug.LogWarning("DrawProximityMap: vectors is null");
            return;
        }

        int w = vectors.GetLength(0);
        int h = vectors.GetLength(1);
        Debug.Log($"DrawProximityMap: size {w} x {h}");

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Vector2 v = vectors[x, y];
                Transform preview = GameObject.Instantiate(
                    PrefabReference.instance.preview,
                    BuildGrid.instance.grid.GetCellCenter(x, y),
                    Quaternion.identity
                );

                if (v == Vector2.zero)
                {
                    preview.GetComponent<Renderer>().material.color = Color.black; // no direction
                }
                else
                {
                    float angle = Mathf.Atan2(v.y, v.x); // -pi..pi
                    float hue = (angle + Mathf.PI) / (2 * Mathf.PI); // map to 0..1
                    Color color = Color.HSVToRGB(hue, 1f, 1f);
                    preview.GetComponent<Renderer>().material.color = color;

                    // optional: rotate visual
                    preview.up = v.normalized;
                }

                preview.position = new Vector3(preview.position.x, preview.position.y, 100);
            }
        }
    }


    public List<List<Vector2Int>> GetDistanceLayers(
       float[,] distanceMap,
       HashSet<Vector2Int> visited = null,
       float layerStep = 10f,
       float fudge = 0.1f,
       float maxDistance = float.PositiveInfinity)
    {
        int width = distanceMap.GetLength(0);
        int height = distanceMap.GetLength(1);


        float maxFiniteDistance = 0f;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float d = distanceMap[x, y];
                if (!float.IsInfinity(d) && d > maxFiniteDistance)
                    maxFiniteDistance = d;
            }
        }

        float actualMaxDistance = Mathf.Min(maxDistance, maxFiniteDistance);

        int numLayers = Mathf.CeilToInt(actualMaxDistance / layerStep);
        List<List<Vector2Int>> layers = new List<List<Vector2Int>>(numLayers + 1);

        // Initialize empty lists for each layer
        for (int i = 0; i <= numLayers; i++)
            layers.Add(new List<Vector2Int>());

        // Assign cells to layers, skip visited, respect fudge
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2Int(x, y);
                if (visited != null && visited.Contains(pos))
                    continue;

                float d = distanceMap[x, y];
                if (float.IsInfinity(d) || d > actualMaxDistance + fudge)
                    continue;

                int layerIndex = Mathf.RoundToInt(d / layerStep);
                if (layerIndex >= 0 && layerIndex < layers.Count)
                    layers[layerIndex].Add(pos);
            }
        }

        return layers;
    }



    public float[,] cellAdjacency;

    public void UpdateCellAdjacency(int adjacencyDistance)
    {
        Graph worldGraph = MapGenerator.instance.worldGraph;
        int width = worldGraph.width;
        int height = worldGraph.height;
        cellAdjacency = new float[width, height];

        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Vector2Int start = new Vector2Int(x, y);
            List<Vector2Int> boundaryCells = new List<Vector2Int>();

            // Step 1: collect all cells at max distance
            worldGraph.CircularBFS(start, adjacencyDistance, (int cx, int cy) =>
            {
                if (Vector2Int.Distance(start, new Vector2Int(cx, cy)) >= adjacencyDistance - 0.5f)
                {
                    boundaryCells.Add(new Vector2Int(cx, cy));
                }
            });

            // Step 2: count reachable boundary cells
            int reachableCount = 0;
            foreach (var bc in boundaryCells)
            {
                bool reached = false;
                worldGraph.CircularBFS(start, adjacencyDistance, (int cx, int cy) =>
                {
                    if (cx == bc.x && cy == bc.y)
                        reached = true;
                });
                if (reached) reachableCount++;
            }

            // Step 3: compute fraction of blockage
            int totalBoundary = Mathf.Max(1, boundaryCells.Count);
            cellAdjacency[x, y] = 1f - (float)reachableCount / totalBoundary;
        });
    }

}
