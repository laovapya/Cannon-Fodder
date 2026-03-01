using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator instance;

    private Graph generatorGraph;
    public Graph worldGraph { get; private set; }
    private Grid<bool> playableAreaGrid; //holds if area is playable

    [field: SerializeField] private int initWidth = 32;
    [field: SerializeField] private int initHeight = 32;

    [field: SerializeField] public int cellSize = 10;
    public int playableAreaWidth { get; private set; }
    public int playableAreaHeight { get; private set; }


    public Vector2 origin { get; private set; }
    public Vector2 mainDiag { private set; get; }

    //private bool[,] isAreaPlayable; //temporary, use gridGraph bool values after. 


    public Vector2 mainHivePos;
    public Vector2 citadelPos;

    public int playableAreaCellCount { get; private set; }
    public static Grid<TGridObject> CreateGridBase<TGridObject>()
    {
        return new Grid<TGridObject>(
            instance.playableAreaWidth,
            instance.playableAreaHeight,
            instance.cellSize,
            instance.origin
        );
    }

    private void Awake()
    {
        instance = this;


        Regenerate();
        // ChooseStartingPoints();
        // bool[,] isAreaPlayable = GeneratePlayableArea();
        // isAreaPlayable = BlurPlayableArea(isAreaPlayable);
        // InitPlayableGrid(isAreaPlayable);

        // worldGraph = new Graph(playableAreaWidth, playableAreaHeight, (int x, int y) => { return IsAreaPlayable(x, y); }); ///!!!!!


        mainHivePos = GetFurthestBlobPosition(GetCenter());
        citadelPos = GetCitadelPosition(mainHivePos);




        worldGraph.ForEachPlayableCell((int x, int y) => playableAreaCellCount++);




        isResourceCell = GetRandomPointMask(powercellDistance, (int x, int y) =>
        {
            Vector2 pos = playableAreaGrid.GetCellCenter(x, y);
            foreach (MapBlob blob in blobCenters)
            {
                if (Vector2.Distance(GetBlobPosition(blob), pos) < powercellCenterDistance * playableAreaGrid.cellSize) return true;
            }
            return Vector2.Distance(citadelPos, pos) < powercellCitadelDistance * playableAreaGrid.cellSize;
        });
        isBorderRingMap = CreateBorderRingMap(playableAreaGrid.array);
        isBorderSpriteMap = CreateBorderRingMap(AddedShiftMap(playableAreaGrid.array, isBorderRingMap, 2));

        CreateRuinMask();

        FillLandscape();
    }

    public Vector2[,] backToMapFlowField;
    private void Start()
    {
        BuildBackToMapFlowField();
    }
    private void BuildBackToMapFlowField()
    {
        // sources: all playable cells

        backToMapFlowField = new Vector2[playableAreaWidth, playableAreaHeight];
        bool[,] visited = new bool[playableAreaWidth, playableAreaHeight];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();


        for (int x = 0; x < playableAreaWidth; x++)
            for (int y = 0; y < playableAreaHeight; y++)
            {
                if (!IsAreaPlayable(x, y)) continue;
                Vector2Int src = new Vector2Int(x, y);
                visited[src.x, src.y] = true;
                backToMapFlowField[src.x, src.y] = Vector2.zero;
                queue.Enqueue(src);
            }
        int[] dx = { -1, 0, 1 };
        int[] dy = { -1, 0, 1 };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (int ix in dx)
                foreach (int iy in dy)
                {
                    int nx = current.x + ix;
                    int ny = current.y + iy;
                    if (nx < 0 || nx >= playableAreaWidth || ny < 0 || ny >= playableAreaHeight) continue;
                    if (visited[nx, ny]) continue;

                    visited[nx, ny] = true;

                    backToMapFlowField[nx, ny] = (new Vector2(current.x, current.y) - new Vector2(nx, ny)).normalized;

                    queue.Enqueue(new Vector2Int(nx, ny));
                }
        }

        //Graph.DrawProximityMap(backToMapFlowField);
    }

    [Header("Ridge Generation Settings")]
    [SerializeField]
    private int ridgeGeneratorCount = 11 * 11;
    [SerializeField, Range(0f, 1f)]
    private float ridgeRemovalChanse = 0.25f;         // probability of removing Voronoi points

    [SerializeField]
    private float ridgeFatnessDistance = 4f;

    [SerializeField, Range(0f, 1f)]
    private float ridgehHeightThreshold = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float ridgeMinPeakHeight = 0.7f;

    [SerializeField]
    private float ridgeHeightFalloffFactor = 0.1f;
    [SerializeField]
    private int ridgePeakCount = 16;
    private void Regenerate()
    {
        playableAreaGrid = new Grid<bool>(initWidth, initHeight, cellSize, origin);
        generatorGraph = new Graph(initWidth, initHeight, (int x, int y) => true);

        const int maxAttempts = 200;
        int attempts = 0;
        bool connected = false;
        bool[,] isAreaPlayable = new bool[initWidth, initHeight];


        while (!connected && attempts++ < maxAttempts)
        {
            ChooseStartingPoints();
            //isAreaPlayable = GeneratePlayableArea();

            // bool[,] blurredMap = BlurPlayableArea(isAreaPlayable);

            // for (int i = 0; i < initWidth; ++i)
            //     for (int j = 0; j < initHeight; ++j)
            //         isAreaPlayable[i, j] = blurredMap[i, j]; //!isHighgroundCell[i, j] && 
            isAreaPlayable = BlurPlayableArea(GeneratePlayableArea());
            InitPlayableGrid(isAreaPlayable);


            worldGraph = new Graph(playableAreaWidth, playableAreaHeight, (x, y) => IsAreaPlayable(x, y));
            connected = worldGraph.IsConnected();
        }
        //fill foothill Map
        isFoothillCell = new bool[playableAreaWidth, playableAreaHeight];
        worldGraph.ForEachPlayableCell((int x, int y) => { isFoothillCell[x, y] = worldGraph.graph[x, y].neighbors.Count < 7; }); //if (isFoothillCell[x, y]) Debug.DrawRay(playableAreaGrid.GetCellCenter(x, y), Vector2.up * 5, Color.green, 100f);


        if (!connected)
            Debug.LogError("Failed to generate a fully connected map after " + maxAttempts + " attempts!");
    }
    //public bool[,] isHighgroundCell { get; private set; }
    [SerializeField] public int blobMinRadius = 5;
    [SerializeField] public int blobMaxRadius = 10;


    public List<MapBlob> blobCenters = new List<MapBlob>();
    //private List<Vector2Int> blobMiniCenters = new List<Vector2Int>();

    [SerializeField] private int blobMinCenters = 7;
    [SerializeField] private int blobMaxCenters = 9;

    [SerializeField] private float spread = 0.5f;
    private void ChooseStartingPoints()
    {
        Vector2 center = new Vector2(initWidth / 2f, initHeight / 2f);
        int count = UnityEngine.Random.Range(blobMinCenters, blobMaxCenters + 1);

        Vector2Int pos = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(center.x), 0, initWidth - 1),
                Mathf.Clamp(Mathf.RoundToInt(center.y), 0, initHeight - 1));
        blobCenters.Add(new MapBlob(pos, blobMinRadius));


        float maxDisplacement = blobMinRadius + blobMaxRadius * spread;
        for (int i = 0; i < count; i++)
        {
            float gx = Mathf.Clamp(UnityEngine.Random.value, 0f, 1f);
            float gy = Mathf.Clamp(UnityEngine.Random.value, 0f, 1f);

            float px = center.x + (UnityEngine.Random.value * 2f - 1f) * maxDisplacement * Mathf.Exp(-Mathf.Pow(gx, 2));
            float py = center.y + (UnityEngine.Random.value * 2f - 1f) * maxDisplacement * Mathf.Exp(-Mathf.Pow(gy, 2));

            pos = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(px), 0, initWidth - 1),
                Mathf.Clamp(Mathf.RoundToInt(py), 0, initHeight - 1)
            );

            int radius = UnityEngine.Random.Range(blobMinRadius, blobMaxRadius + 1);
            blobCenters.Add(new MapBlob(pos, radius));
        }
    }

    private bool[,] GeneratePlayableArea()
    {
        bool[,] isAreaPlayable = new bool[initWidth, initHeight];

        foreach (MapBlob blob in blobCenters)
        {
            generatorGraph.CircularBFS(new Vector2Int(blob.cords.x, blob.cords.y), blob.radius,
            (int x, int y) =>
            {
                isAreaPlayable[x, y] = true;
            }
           );
        }

        return isAreaPlayable;
    }
    [SerializeField] private int blurIterations = 6;
    [SerializeField] private int blurNeighborsChecked = 5;

    private bool[,] BlurPlayableArea(bool[,] isAreaPlayable)
    {
        for (int k = 0; k < blurIterations; ++k)
        {
            bool[,] newValues = new bool[initWidth, initHeight];
            for (int i = 0; i < initWidth; ++i)
                for (int j = 0; j < initHeight; ++j)
                {
                    bool current = isAreaPlayable[i, j];
                    int trueCount = 0;
                    //List<Vector2Int> neighbors = GetNeighbors(new Vector2Int(i, j), true, initWidth, initHeight);
                    foreach (GraphCell v in generatorGraph.graph[i, j].neighbors)
                        if (isAreaPlayable[v.x, v.y]) trueCount++;


                    if (trueCount < blurNeighborsChecked) newValues[i, j] = false;
                    else newValues[i, j] = current;
                }
            for (int i = 0; i < initWidth; ++i)
                for (int j = 0; j < initHeight; ++j)
                    isAreaPlayable[i, j] = newValues[i, j];
        }
        return isAreaPlayable;
    }
    [SerializeField] private int borderWidth = 1;
    private void InitPlayableGrid(bool[,] isAreaPlayable)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        for (int i = 0; i < initWidth; i++)
        {
            for (int j = 0; j < initHeight; j++)
            {
                if (isAreaPlayable[i, j])
                {
                    if (i < minX) minX = i;
                    if (j < minY) minY = j;
                    if (i > maxX) maxX = i;
                    if (j > maxY) maxY = j;
                }
            }
        }
        minX = Mathf.Max(0, minX - borderWidth);
        minY = Mathf.Max(0, minY - borderWidth);
        maxX = Mathf.Min(initWidth - 1, maxX + borderWidth);
        maxY = Mathf.Min(initHeight - 1, maxY + borderWidth);

        finalOriginOffset = new Vector2Int(minX, minY);
        playableAreaWidth = maxX - minX + 1;
        playableAreaHeight = maxY - minY + 1;


        mainDiag = new Vector2(playableAreaWidth, playableAreaHeight) * cellSize;

        playableAreaGrid = new Grid<bool>(playableAreaWidth, playableAreaHeight, cellSize, Vector2.zero);
        isHighgroundCell = new bool[playableAreaWidth, playableAreaHeight];


        bool[,] highgroundMap = RidgeGenerator.GetRidgeMap(initWidth, initHeight, ridgeGeneratorCount, ridgeRemovalChanse, ridgeFatnessDistance, ridgehHeightThreshold, ridgeMinPeakHeight, ridgeHeightFalloffFactor, ridgePeakCount,
          UnityEngine.Random.Range(int.MinValue, int.MaxValue));


        for (int i = minX, x = 0; i <= maxX; i++, x++)
            for (int j = minY, y = 0; j <= maxY; j++, y++)
            {
                isHighgroundCell[x, y] = highgroundMap[i, j] && isAreaPlayable[i, j]; //cut ridges outside of map blob
                playableAreaGrid.SetValue(x, y, isAreaPlayable[i, j] && !isHighgroundCell[x, y]);
            }
    }

    public bool[,] isFoothillCell { get; private set; }
    public bool[,] isHighgroundCell { get; private set; }




    private Vector2Int finalOriginOffset;
    public Vector2 GetBlobPosition(MapBlob blob)
    {
        return playableAreaGrid.GetCellCenter(blob.cords.x - finalOriginOffset.x, blob.cords.y - finalOriginOffset.y);
    }

    // private Vector2 GetLargestBlob()
    // {
    //     Vector2 pos = GetBlobPosition(UtilContainers.FirstOf(blobCenters, (MapBlob blob) => blob.radius));

    //     //Debug.DrawRay(pos, Vector2.up * 50, Color.red, 10f);
    //     //Debug.Log("blob positio " + pos);
    //     return playableAreaGrid.GetCellCenter(pos);
    // }
    private Vector2 GetFurthestBlobPosition(Vector2 position)
    {
        Vector2 furthestBlobPos = GetBlobPosition(UtilContainers.FirstOf(blobCenters, (MapBlob blob) => -Vector2.Distance(GetBlobPosition(blob), position)));

        return GetClosestPlayablePos(furthestBlobPos);
    }
    private Vector2 GetClosestPlayablePos(Vector2 position)
    {
        float bestScore = float.MinValue;
        Vector2Int bestCell = default;

        for (int x = 0; x < playableAreaGrid.width; x++)
        {
            for (int y = 0; y < playableAreaGrid.height; y++)
            {
                if (!playableAreaGrid.GetValue(x, y))
                    continue;
                Vector2 worldPos = playableAreaGrid.GetCellCenter(x, y);

                float distToBlob = Vector2.Distance(worldPos, position);

                float score = -distToBlob;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = new Vector2Int(x, y);
                }
            }
        }

        return playableAreaGrid.GetCellCenter(bestCell.x, bestCell.y);
    }

    private Vector2 GetCitadelPosition(Vector2 mainHivePos, int bestCount = 10)
    {
        Vector2 mapCenter = new Vector2(
           playableAreaWidth * 0.5f * cellSize,
           playableAreaHeight * 0.5f * cellSize
        );

        List<(Vector2Int cell, float score)> candidates = new List<(Vector2Int, float)>();
        float bestScore = float.MinValue;

        for (int x = 0; x < playableAreaGrid.width; x++)
        {
            for (int y = 0; y < playableAreaGrid.height; y++)
            {
                if (!playableAreaGrid.GetValue(x, y))
                    continue;

                Vector2 worldPos = playableAreaGrid.GetCellCenter(x, y);

                float distToHive = Vector2.Distance(worldPos, mainHivePos);
                float distToCenter = Vector2.Distance(worldPos, mapCenter);

                float ringRadius = 5 * 10;
                float score = distToHive * 0.8f - Mathf.Abs(distToCenter - ringRadius) * 1f;

                if (score > bestScore) bestScore = score;

                candidates.Add((new Vector2Int(x, y), score));
            }
        }

        var topCandidates = candidates.Where(c => c.score >= bestScore).OrderByDescending(c => c.score).Take(bestCount).ToList();
        var cell = topCandidates[UnityEngine.Random.Range(0, topCandidates.Count)].cell;
        return playableAreaGrid.GetCellCenter(cell.x, cell.y);
    }



    public Vector2 GetCenter()
    {
        Vector2 pos = GetBlobPosition(blobCenters[0]);
        return playableAreaGrid.GetCellCenter(pos);
    }

    public bool[,] isResourceCell { get; private set; }
    public bool[,] isRuinCell { get; private set; }

    [SerializeField] private float ruinPercent = 0.3f;

    private int desiredPowercells = 6;
    private int powercellDistance = 5;
    private int powercellCitadelDistance = 7;
    private int powercellCenterDistance = 4;
    public bool[,] GetRandomPointMask(int distanceBetween, Func<int, int, bool> IsNearSomething)
    {
        bool[,] marked = new bool[playableAreaGrid.width, playableAreaGrid.height];
        int tries = 0;


        int maxTries = 1000;
        while (tries < maxTries)
        {
            int x = UnityEngine.Random.Range(2, playableAreaGrid.width - 2);
            int y = UnityEngine.Random.Range(2, playableAreaGrid.height - 2);
            if (F(x, y))
            {
                marked[x, y] = true;
            }
            tries++;
        }

        bool F(int x, int y)
        {
            if (!playableAreaGrid.GetValue(x, y)) return false;
            for (int dx = -distanceBetween; dx <= distanceBetween; dx++)
                for (int dy = -distanceBetween; dy <= distanceBetween; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx == x && ny == y) continue;
                    if (nx >= 0 && nx < playableAreaGrid.width && ny >= 0 && ny < playableAreaGrid.height)
                    {
                        if (marked[nx, ny] || (new Vector2(dx, dy).magnitude < 2 && !playableAreaGrid.GetValue(nx, ny))) return false;
                    }
                }
            return !IsNearSomething(x, y);
        }

        return marked;
    }

    public bool GetIfPowercell(Vector2 pos)
    {
        int x, y;
        playableAreaGrid.GetClampedXY(pos, out x, out y);
        return isResourceCell[x, y];
    }
    public bool GetIfPowercell(int x, int y)
    {
        return isResourceCell[x, y];
    }

    public bool IsAreaPlayable(int x, int y)
    {
        return playableAreaGrid.GetValue(x, y);
    }

    public bool IsAreaPlayable(Vector2 worldPos)
    {
        return playableAreaGrid.GetValue(worldPos);
    }
    private void CreateRuinMask()
    {
        isRuinCell = new bool[playableAreaGrid.width, playableAreaGrid.height];
        List<Vector2Int> resourceCells = new();
        int width = isResourceCell.GetLength(0);
        int height = isResourceCell.GetLength(1);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (isResourceCell[x, y])
                    resourceCells.Add(new Vector2Int(x, y));

        resourceCells.Shuffle();

        int ruinCount = Mathf.FloorToInt(resourceCells.Count * ruinPercent);
        for (int i = 0; i < ruinCount; i++)
        {
            var pos = resourceCells[i];
            isRuinCell[pos.x, pos.y] = true;
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector2(initWidth * cellSize, initHeight * cellSize));
    }

    [Header("Landscape")]
    [SerializeField] private float rockChance = 0.05f;
    [SerializeField] private float grassChance = 0.1f;
    [SerializeField] private float stubChance = 0.01f;
    [Header("Landscape Prefabs")]
    [SerializeField] private List<GameObject> rockPrefabs = new();
    [SerializeField] private List<GameObject> grassPrefabs = new();
    [SerializeField] private List<GameObject> stubPrefabs = new();
    [SerializeField] private int tilePassCount = 3;
    private void FillLandscape()
    {
        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            for (int i = 0; i < tilePassCount; i++)
            {
                Vector3 cellCenter = playableAreaGrid.GetCellCenter(x, y);

                // Stub
                // if (UnityEngine.Random.value < stubChance && stubPrefabs.Count > 0)
                // {
                //     Vector2 stubPos = UtilMath.GetRandomTilePosition(cellCenter, cellSize / 2f);
                //     GameObject stubPrefab = stubPrefabs[UnityEngine.Random.Range(0, stubPrefabs.Count)];
                //     Instantiate(stubPrefab, stubPos, Quaternion.identity, transform);
                // }

                // Rock
                // if (UnityEngine.Random.value < rockChance && rockPrefabs.Count > 0)
                // {
                //     Vector2 rockPos = UtilMath.GetRandomTilePosition(cellCenter, cellSize / 2f);
                //     GameObject rockPrefab = rockPrefabs[UnityEngine.Random.Range(0, rockPrefabs.Count)];
                //     Instantiate(rockPrefab, rockPos, Quaternion.identity, transform);
                // }

                // Grass
                if (UnityEngine.Random.value < grassChance && grassPrefabs.Count > 0)
                {
                    Vector2 grassPos = UtilMath.GetRandomTilePosition(cellCenter, cellSize / 2f);
                    GameObject grassPrefab = grassPrefabs[UnityEngine.Random.Range(0, grassPrefabs.Count)];
                    Instantiate(grassPrefab, grassPos, Quaternion.identity, transform);
                }
            }

        });
    }


    public bool[,] isBorderRingMap { get; private set; }
    public bool[,] isBorderSpriteMap { get; private set; }
    public bool[,] CreateBorderRingMap(bool[,] aroundMap)
    {
        int w = aroundMap.GetLength(0);
        int h = aroundMap.GetLength(1);


        int widthHighground = isHighgroundCell.GetLength(0);
        int heightHighground = isHighgroundCell.GetLength(1);

        bool[,] map = new bool[w, h];
        //int borderShift = 2;
        //isBorderSpriteMap = new bool[w, h + borderShift];

        int[] dx = { -1, 0, 1 };
        int[] dy = { -1, 0, 1 };

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // must be non-playable
                if (aroundMap[x, y])
                    continue;

                // exclude highground explicitly
                if (x < widthHighground && y < heightHighground && isHighgroundCell[x, y])
                    continue;

                bool hasPlayableNeighbor = false;

                foreach (int ix in dx)
                    foreach (int iy in dy)
                    {
                        if (ix == 0 && iy == 0)
                            continue;

                        int nx = x + ix;
                        int ny = y + iy;

                        if (nx < 0 || nx >= w || ny < 0 || ny >= h)
                            continue;

                        if (aroundMap[nx, ny])
                        {
                            hasPlayableNeighbor = true;
                            break;
                        }
                    }

                map[x, y] = hasPlayableNeighbor;
                //isBorderSpriteMap[x, y] = isBorderRingMap[x, y];
                // if (map[x, y])
                //     Debug.DrawRay(playableAreaGrid.GetCellCenter(x, y), Vector3.up * 2f, Color.red, 10f);

            }
        }
        //ShiftRingUp(ref isBorderSpriteMap, borderShift);
        return map;
    }
    public bool[,] addedShiftMap;
    private bool[,] AddedShiftMap(bool[,] playableAreaMap, bool[,] borderRingMap, int shift = 3)
    {
        int width = playableAreaMap.GetLength(0);
        int height = playableAreaMap.GetLength(1);

        // Start by copying the playable area map into the new map
        bool[,] newMap = new bool[width, height + shift];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                newMap[x, y] = playableAreaMap[x, y];

        // For each playable cell, check if neighbor above is in the border ring
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!playableAreaMap[x, y]) continue; // skip non-playable cells

                int aboveY = y + 1;
                if (aboveY >= height) continue; // skip if above is out of bounds

                if (borderRingMap[x, aboveY])
                {
                    // Add "shift" playable cells above this cell
                    for (int i = 1; i <= shift; i++)
                    {
                        int newY = y + i;
                        if (newY >= height + shift) break;
                        newMap[x, newY] = true;
                    }
                }
            }
        }
        addedShiftMap = newMap;
        return newMap;
    }

    // private bool[,] AddedShiftMap(bool[,] playableAreaMap, bool[,] borderRingMap, int shift = 3)
    // {
    //     shift -= 1;
    //     int width = playableAreaMap.GetLength(0);
    //     int height = playableAreaMap.GetLength(1);
    //     bool[,] newMap = new bool[width, height + shift];

    //     for (int x = 0; x < width; x++)
    //     {
    //         for (int y = 0; y < height; y++)
    //         {
    //             newMap[x, y] = playableAreaMap[x, y];
    //         }
    //     }

    //     for (int x = 0; x < width; x++)
    //     {
    //         for (int y = 0; y < height; y++)
    //         {

    //             if (!borderRingMap[x, y]) continue;
    //             newMap[x, y] = true;



    //             bool hasNeighborBelow = y <= 0 || (y > 0 && !IsAreaPlayable(x, y - 1));

    //             if (!hasNeighborBelow)
    //             {
    //                 // Add "shift" playable cells above this one
    //                 for (int i = 1; i <= shift; i++)
    //                 {
    //                     int newY = y + i;
    //                     if (newY >= height + shift) break;
    //                     newMap[x, newY] = true;
    //                     if (newMap[x, y])
    //                         Debug.DrawRay(playableAreaGrid.GetCellCenter(x, y), Vector3.up * 2f, Color.magenta, 10f);
    //                 }
    //             }

    //         }
    //     }

    //     return newMap;
    // }

}
