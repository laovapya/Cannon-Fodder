using System.Collections.Generic;
using UnityEngine;
using System;

public class TerritoryGrid : MonoBehaviour
{
    public static TerritoryGrid instance;
    public enum type
    {
        ally,
        neutral,
        enemy,
    }
    //public class TerritoryNode : IGraphNode<TerritoryNode>
    //{
    //    public type type;
    //    public int x { get; set; }
    //    public int y { get; set; }
    //    public List<TerritoryNode> neighbors { get; } = new List<TerritoryNode>();
    //}
    //public Graph<TerritoryNode> gridGraph { get; private set; }

    public Grid<type> grid { get; private set; }
    [SerializeField] private Sprite tile_ally;
    [SerializeField] private Sprite tile_neutral;
    [SerializeField] private Sprite tile_enemy;
    //private Grid<int> contestGrid;
    private void Awake()
    {
        instance = this;
    }
    //private int contestGridDefault = 999;
    void Start()
    {
        sprite_ally = tile_ally;
        sprite_neutral = tile_neutral;
        sprite_enemy = tile_enemy;



        grid = MapGenerator.CreateGridBase<type>();
        //marchingVertices = new int[grid.width + 1, grid.height + 1];

        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) => { grid.SetValue(x, y, type.enemy); });


        CaptureAllyInitial(MapGenerator.instance.citadelPos, allyTerritoryStartRadius);

        InitTerritoryTiles();

        ToEnemyTerritoryFlowField = new Vector2[grid.width, grid.height];
        UpdateToEnemyFlowField();



        onTerritoryUpdate += UpdateDrawnGrid;
        onTerritoryUpdate += UpdateToEnemyFlowField;

        //GameHandler.instance.onWaveEnd += ApplyContests;


        // Texture2D texAlly = Resources.Load<Texture2D>("Tiles/ally_tile");
        // Texture2D texNeutral = Resources.Load<Texture2D>("Tiles/neutral_tile");
        // Texture2D texEnemy = Resources.Load<Texture2D>("Tiles/enemy_tile");
        // Debug.Log(texAlly != null ? "Ally texture loaded successfully." : "Ally texture FAILED to load!");
        // Debug.Log(texNeutral != null ? "Neutral texture loaded successfully." : "Neutral texture FAILED to load!");
        // Debug.Log(texEnemy != null ? "Enemy texture loaded successfully." : "Enemy texture FAILED to load!");

        // sprite_ally = Sprite.Create(
        //     texAlly,
        //     new Rect(0, 0, texAlly.width, texAlly.height),
        //     new Vector2(0.5f, 0.5f),
        //     6.4f
        // );
        // sprite_neutral = Sprite.Create(
        //     texNeutral,
        //     new Rect(0, 0, texNeutral.width, texNeutral.height),
        //     new Vector2(0.5f, 0.5f),
        //     6.4f
        // );
        // sprite_enemy = Sprite.Create(
        //     texEnemy,
        //     new Rect(0, 0, texEnemy.width, texEnemy.height),
        //     new Vector2(0.5f, 0.5f),
        //     6.4f
        // );


        UpdateDrawnGrid();


    }
    private Sprite sprite_ally;
    private Sprite sprite_neutral;
    private Sprite sprite_enemy;
    [SerializeField] private int allyTerritoryStartRadius = 5;
    public Action onTerritoryUpdate;

    // private void CaptureAllyTerritory(Vector2 position, int radiusInCells)
    // {
    //     int x, y;
    //     grid.GetClampedXY(position, out x, out y);
    //     Vector2Int coords = new Vector2Int(x, y);
    //     CaptureTerritory(coords, radiusInCells, type.ally);
    // }
    [SerializeField] private int neutralNodeCellRadius = 7;
    [SerializeField] private int neutralNodeCount = 10;
    public void CaptureCell(int x, int y, type type)
    {
        grid.SetValue(x, y, type);
        onTerritoryUpdate?.Invoke();
    }
    // private void CaptureNeutralTerritory()
    // {
    //     // first, mark the random neutral points
    //     foreach (Vector2Int coords in MapGenerator.instance.GetNeutralTerritoryPoints(neutralNodeCount, 4, neutralNodeCellRadius))
    //     {
    //         CaptureRandomTerritory(coords, neutralNodeCellRadius, type.neutral);
    //     }

    //     int width = TerritoryGrid.instance.grid.width;
    //     int height = TerritoryGrid.instance.grid.height;
    //     bool[,] mask = new bool[width, height];
    //     int x1, y1;
    //     grid.GetClampedXY(MapGenerator.instance.mainHivePos, out x1, out y1);
    //     Vector2Int start = new Vector2Int(x1, y1);
    //     // bfs starting from mainHivePos, only through enemy territory
    //     MapGenerator.instance.worldGraph.BFS(
    //         start,
    //         (x, y) =>
    //         {
    //             mask[x, y] = true;
    //         },
    //         (toCell) =>
    //         {
    //             return grid.GetValue(toCell.x, toCell.y) == type.enemy;
    //         }
    //     );

    //     for (int x = 0; x < width; x++)
    //         for (int y = 0; y < height; y++)
    //             if (!mask[x, y] && grid.GetValue(x, y) == type.enemy)
    //                 grid.SetValue(x, y, type.neutral);

    //     onTerritoryUpdate?.Invoke();
    // }
    private void CaptureAllyInitial(Vector2 pos, int radiusInCells)
    {
        int x, y;
        grid.GetClampedXY(pos, out x, out y);
        Vector2Int center = new Vector2Int(x, y);

        List<(Vector2Int coords, int radius)> points = new List<(Vector2Int, int)>();
        points.Add((center, radiusInCells));

        int auxilaryPointsCount = 4;
        for (int i = 0; i < auxilaryPointsCount; i++)
        {
            int rx = Mathf.Clamp(center.x + UnityEngine.Random.Range(-radiusInCells + 1, radiusInCells - 1), 0, grid.width);
            int ry = Mathf.Clamp(center.y + UnityEngine.Random.Range(-radiusInCells + 1, radiusInCells - 1), 0, grid.height);
            points.Add((new Vector2Int(rx, ry), radiusInCells - 1));
        }

        foreach (var p in points)
        {
            int neutralRadius = p.radius + UnityEngine.Random.Range(1, 4);
            MapGenerator.instance.worldGraph.CircularBFS(p.coords, neutralRadius, (int nx, int ny) =>
            {
                if (grid.GetValue(nx, ny) == type.enemy)
                    grid.SetValue(nx, ny, type.neutral);
            }, 0.7f);
        }

        foreach (var p in points)
        {
            MapGenerator.instance.worldGraph.CircularBFS(p.coords, p.radius, (int ax, int ay) =>
            {
                grid.SetValue(ax, ay, type.ally);
            }, 0.7f);
        }

        onTerritoryUpdate?.Invoke();
    }


    [SerializeField] private Transform territoryTile;
    [SerializeField] private Transform highground_border_tile;

    private SpriteRenderer[,] territoryTiles;


    private void InitTerritoryTiles()
    {
        territoryTiles = new SpriteRenderer[grid.width, grid.height];

        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            Transform tile;
            if (!(tile = Instantiate(territoryTile, grid.GetCellCenter(x, y), Quaternion.identity, PrefabReference.instance.folderDynamicObjects)
                ).TryGetComponent<SpriteRenderer>(out territoryTiles[x, y]))
                Debug.LogError("highlightTile doesnt have spriteRenderer component");

            //territoryTiles[x, y].color = allyColor;

        });

        Transform border_tile;
        SpriteRenderer border_sr;
        for (int x = 0; x < MapGenerator.instance.playableAreaWidth; ++x)
        {
            for (int y = 0; y < MapGenerator.instance.playableAreaHeight; ++y)
            {

                if (MapGenerator.instance.isBorderRingMap[x, y])
                    if ((border_tile = Instantiate(territoryTile, grid.GetCellCenter(x, y), Quaternion.identity, PrefabReference.instance.folderDynamicObjects)
                ).TryGetComponent<SpriteRenderer>(out border_sr))
                    {
                        border_sr.sprite = sprite_enemy;
                    }
            }
        }

        //====================================================================== INIT BORDER TILES


        int w = MapGenerator.instance.isBorderSpriteMap.GetLength(0);
        int h = MapGenerator.instance.isBorderSpriteMap.GetLength(1);
        MarchingSquares borderSquared = new MarchingSquares(w, h);
        borderSquared.FillVertices((x, y) =>
    !MapGenerator.instance.addedShiftMap[x, y]);
        //|| (x < MapGenerator.instance.isHighgroundCell.GetLength(0)
        //&& y < MapGenerator.instance.isHighgroundCell.GetLength(1)
        //&& MapGenerator.instance.isHighgroundCell[x, y]));

        for (int x = 0; x < w; ++x)
        {
            for (int y = 0; y < h; ++y)
            {
                // Skip playable tiles
                if (!MapGenerator.instance.isBorderSpriteMap[x, y])
                    continue;


                int index = borderSquared.GetCellIndex(x, y);
                Transform border = Instantiate(
                    highground_border_tile,
                    new Vector2(x, y) * grid.cellSize + new Vector2(grid.cellSize, grid.cellSize) / 2 + grid.originPosition,
                    Quaternion.identity,
                    PrefabReference.instance.folderDynamicObjects
                );
                border.GetComponent<SpriteRenderer>().sprite = marchingSpritesBorders[index];

            }
        }

        //====================================================================== INIT HIGHGROUND TILES
        MarchingSquares highgroundSquared = new MarchingSquares(grid.width, grid.height);
        highgroundSquared.FillVertices((x, y) => MapGenerator.instance.isHighgroundCell[x, y]);// || MapGenerator.instance.isBorderRingMap[x, y]);
        for (int x = 0; x < grid.width; ++x)
        {
            for (int y = 0; y < grid.height; ++y)
            {
                // Skip non-highground tiles
                if (!MapGenerator.instance.isHighgroundCell[x, y])
                    continue;
                Transform highground = Instantiate(
                highground_border_tile,
                new Vector2(x, y) * grid.cellSize + new Vector2(grid.cellSize, grid.cellSize) / 2 + grid.originPosition,
                Quaternion.identity,
                PrefabReference.instance.folderDynamicObjects
                );
                highground.GetComponent<SpriteRenderer>().sprite = marchingSpritesHighground[highgroundSquared.GetCellIndex(x, y)];
            }
        }
    }
    // private bool HasPlayableNeighbor(int x, int y)
    // {
    //     int width = grid.width;
    //     int height = grid.height;

    //     // Check all 8 neighbors (cardinal + diagonal)
    //     int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
    //     int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

    //     for (int i = 0; i < dx.Length; i++)
    //     {
    //         int nx = x + dx[i];
    //         int ny = y + dy[i];

    //         if (nx >= 0 && nx < width && ny >= 0 && ny < height)
    //         {
    //             if (MapGenerator.instance.IsAreaPlayable(nx, ny))
    //                 return true;
    //         }
    //     }

    //     return false;
    // }



    public int[,] GetFrontlineDistances()
    {
        int[,] distances = new int[grid.width, grid.height];
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                distances[x, y] = int.MaxValue;
        MapGenerator.instance.worldGraph.BFSdistance(GetCapturedCells(), (int x, int y, int distance) => { distances[x, y] = distance; });
        return distances;
    }
    public int[,] GetNeutralFrontlineDistances()
    {
        int[,] distances = new int[grid.width, grid.height];
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                distances[x, y] = int.MaxValue;
        MapGenerator.instance.worldGraph.BFSdistance(GetNeutralCells(), (int x, int y, int distance) => { distances[x, y] = distance; });
        return distances;
    }
    private List<Vector2Int> GetCapturedCells()
    {
        List<Vector2Int> captured = new List<Vector2Int>();
        //foreach(TerritoryNode node in gridGraph.grid.array)
        //    if (node.type == type.ally)
        //        captured.Add(node);
        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            if (grid.GetValue(x, y) == type.ally)
                captured.Add(new Vector2Int(x, y));

        });
        return captured;
    }
    private List<Vector2Int> GetNeutralCells()
    {
        List<Vector2Int> neutral = new List<Vector2Int>();

        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            if (grid.GetValue(x, y) == type.neutral)
                neutral.Add(new Vector2Int(x, y));

        });
        return neutral;
    }
    private void UpdateDrawnGrid() //===============================================================================================================================================================================
    {
        for (int i = 0; i < grid.width; ++i)
            for (int j = 0; j < grid.height; ++j)
            {
                if (!MapGenerator.instance.IsAreaPlayable(i, j))
                    continue;

                SpriteRenderer sr = territoryTiles[i, j];

                if (MapGenerator.instance.GetIfPowercell(i, j))
                {
                    sr.sprite = sprite_neutral;
                    sr.color = Color.blue;
                    continue;
                }

                sr.color = Color.white;
                //int idx = GetTerritorySpriteIndex(i, j);
                //sr.sprite = marchingSprites[idx];

                switch (grid.GetValue(i, j))
                {
                    case type.neutral:
                        //sr.color = neutralColor;
                        sr.sprite = sprite_neutral;

                        break;
                    case type.ally:

                        //sr.color = allyColor;
                        sr.sprite = sprite_ally;

                        break;

                    case type.enemy:
                        //sr.color = enemyColor;
                        sr.sprite = sprite_enemy;
                        break;
                }

            }
    }

    public Vector2[,] ToEnemyTerritoryFlowField { get; private set; }
    private void UpdateToEnemyFlowField()
    {
        Graph worldGraph = MapGenerator.instance.worldGraph;

        Dictionary<Vector2Int, float> sources = new();


        for (int i = 0; i < grid.width; ++i)
            for (int j = 0; j < grid.height; ++j)
            {
                if (grid.GetValue(i, j) == type.enemy)
                    sources[new Vector2Int(i, j)] = 0;
            }
        float[,] costMap = worldGraph.DijkstraMap(sources, (int x, int y) => false);
        ToEnemyTerritoryFlowField = worldGraph.BuildDirectionMap(costMap, (int x, int y) => false);

        for (int i = 0; i < grid.width; ++i) //combine with backToMapFlowField 
            for (int j = 0; j < grid.height; ++j)
            {
                if (!MapGenerator.instance.IsAreaPlayable(i, j))
                    ToEnemyTerritoryFlowField[i, j] = MapGenerator.instance.backToMapFlowField[i, j];

            }
    }
    public List<Vector2> GetEnemyPositions()
    {
        List<Vector2> list = new List<Vector2>();
        for (int i = 0; i < grid.width; ++i)
            for (int j = 0; j < grid.height; ++j)
            {
                if (grid.GetValue(i, j) == type.enemy)
                    list.Add(grid.GetCellCenter(i, j));
            }
        return list;
    }


    public int GetAllyCellCount()
    {
        int count = 0;
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                if (grid.GetValue(x, y) == type.ally)
                    count++;
        return count;
    }

    public int GetEnemyCellCount()
    {
        int count = 0;
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                if (grid.GetValue(x, y) == type.enemy)
                    count++;
        return count;
    }

    public int GetNeutralCellCount()
    {
        int count = 0;
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                if (grid.GetValue(x, y) == type.neutral)
                    count++;
        return count;
    }
    public void ForEachAllyCell(Action<int, int> action)
    {
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                if (grid.GetValue(x, y) == type.ally)
                    action(x, y);
    }
    public void ForEachEnemyCell(Action<int, int> action)
    {
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                if (grid.GetValue(x, y) == type.enemy)
                    action(x, y);
    }

    public int capturedResourceCells
    {
        get
        {
            int count = 0;
            MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
            {
                if (MapGenerator.instance.isResourceCell[x, y] && grid.GetValue(x, y) == type.ally)
                    count++;
            });
            return count;
        }
    }


    [SerializeField] private Color allyColor = Color.white;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color enemyColor = Color.grey;





    [Header("Marching Squares Sprites")]

    [SerializeField] private Sprite[] marchingSpritesHighground;
    [SerializeField] private Sprite[] marchingSpritesBorders;
    //MarchingSquares marchingSquares;




}
