using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class SelectionGrid : MonoBehaviour
{
    public static SelectionGrid instance;
    private Graph worldGraph;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        //textColor = new Color(15f / 255f, 101f / 255f, 42f / 255f, 1f);
        worldGraph = MapGenerator.instance.worldGraph;


        grid = BuildGrid.instance.grid;

        width = grid.width;
        height = grid.height;
        cellSize = grid.cellSize;


        InitHighlightTiles();
        InitTextTiles();

        selectionMask = new bool[width, height];

        SoldierGrid.instance.onSoldierGridUpdate += UpdateSoldierCounts;
    }

    private int mousePosX, mousePosY;
    private void Update()
    {
        Vector2 mousePos = Util.GetMouseWorldPosition();

        grid.GetClampedXY(mousePos, out mousePosX, out mousePosY);

        HighlightTiles(mousePos);
        //HighlightSoldierCounts(mousePos);


        StartDragSelect(mousePos, mousePosX, mousePosY);
        DragSelect(mousePos);
    }
    private void LateUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
            StopDragSelect();
    }
    private int width, height;
    private float cellSize;

    private Grid<Building> grid; //do not use the grid's array

    [SerializeField] private Transform highlightTile;

    private void InitHighlightTiles()
    {
        highlightedTiles = new SpriteRenderer[width, height];
        for (int i = 0; i < width; ++i)
            for (int j = 0; j < height; ++j)
            {
                if (!MapGenerator.instance.IsAreaPlayable(i, j))
                    continue;
                Transform tile;
                if (!(tile = Instantiate(highlightTile, grid.GetCellCenter(i, j), Quaternion.identity, PrefabReference.instance.folderDynamicObjects)
                    ).TryGetComponent<SpriteRenderer>(out highlightedTiles[i, j]))
                    Debug.LogError("highlightTile doesnt have spriteRenderer component");
                tile.localScale = new Vector3(cellSize, cellSize, 1);
            }
    }


    private void InitTextTiles()
    {
        soldierTexts = new TextMeshProUGUI[width, height];
        soldierCounts = new int[width, height];
        worldGraph.ForEachPlayableCell((int i, int j) =>
        {
            soldierTexts[i, j] = Util.CreateWorldText(PrefabReference.instance.folderDynamicObjects, "", grid.GetCellCenter(i, j) + Vector2.up * cellSize / 2, 36, new Color(15f / 255f, 101f / 255f, 42f / 255f, 1f));
        });

    }
    private void UpdateSoldierCounts(int x, int y, int count)
    {
        if (!MapGenerator.instance.IsAreaPlayable(x, y))
            return;
        soldierCounts[x, y] = count;

        soldierTexts[x, y].text = count.ToString();


    }

    private SpriteRenderer[,] highlightedTiles;
    private TextMeshProUGUI[,] soldierTexts;
    public int[,] soldierCounts;
    //public Color textColor { get; private set; }

    private SpriteRenderer highlightedTile = null;
    private TextMeshProUGUI highlightedSoldierCount = null;
    private void HighlightTiles(Vector2 pos)
    {
        int x, y;
        grid.GetClampedXY(pos, out x, out y);
        MapGenerator.instance.worldGraph.ForEachPlayableCell((int i, int j) =>
        {
            //highlightedTiles[i, j].gameObject.SetActive((i == x && j == y) || selectionMask[i, j]);
            //Color colorTile = highlightedTiles[i, j].color;

            //Color colorSoldierText = soldierTexts[i, j].color;
            if ((i == x && j == y) || selectionMask[i, j])
            {

                highlightedTile = highlightedTiles[i, j];
                highlightedTile.gameObject.SetActive(true);
                //Debug.Log(pos + " " + x + " " + y + " activated");
                //highlightedTile.color = new Color(colorTile.r, colorTile.g, colorTile.b, 0.3f);


            }
            else
            {
                //highlightedTiles[i, j].color = new Color(colorTile.r, colorTile.g, colorTile.b, 0f);
                highlightedTiles[i, j].gameObject.SetActive(false);
                //soldierTexts[i, j].color = new Color(colorSoldierText.r, colorSoldierText.g, colorSoldierText.b, 0f);
            }
        });


        if (grid.GetValue(pos) == null && !isDragSelecting && highlightedTile != null) //disable highlight when cursour outside of grid 
        {
            highlightedTile.gameObject.SetActive(false);
            // if (highlightedTile != null) //shouldnt be null 
            // {

            //     //Color color = highlightedTile.color;
            //     //Color colorSoldierText = highlightedSoldierCount.color;
            //     //highlightedTile.color = new Color(color.r, color.g, color.b, 0f);
            //     //highlightedSoldierCount.color = new Color(colorSoldierText.r, colorSoldierText.g, colorSoldierText.b, 0f);
            // }
        }
    }

    // private void HighlightSoldierCounts(Vector2 pos)
    // {
    //     MapGenerator.instance.worldGraph.ForEachPlayableCell((int i, int j) =>
    //     {
    //         soldierTexts[i, j].transform.parent.gameObject.SetActive(SoldierGrid.instance.chosenUnitMask[i, j]);
    //     });


    //     int x, y;
    //     grid.GetClampedXY(pos, out x, out y);
    //     if (!MapGenerator.instance.IsAreaPlayable(x, y)) return;
    //     soldierTexts[x, y].transform.parent.gameObject.SetActive(soldierCounts[x, y] > 0);
    // }

    private bool isDragSelecting = false;
    private Vector2Int dragSelectStart;
    private Vector2Int currentDragSelectEnd = Vector2Int.zero;
    private Vector2Int dragSelectEnd;
    public bool[,] selectionMask { get; private set; }
    private void StartDragSelect(Vector2 pos, int x, int y) //dont start drag selecting in UI 
    {
        if (!isDragSelecting && Input.GetKeyDown(KeyCode.Mouse0))
        {

        }
        if (!isDragSelecting && Input.GetKeyDown(KeyCode.Mouse0) && BuildGrid.instance.buildMode == BuildGrid.type.empty && !SoldierGrid.instance.isSelectingRoute)
        {
            isDragSelecting = true;
            grid.GetClampedXY(pos, out x, out y);
            dragSelectStart = new Vector2Int(x, y);

        }
    }
    private int selectedTileCount;
    private void DragSelect(Vector2 pos)
    {
        if (isDragSelecting)
        {



            pos = grid.GetClampedPosition(pos);

            int x, y;
            grid.GetClampedXY(pos, out x, out y);
            //Debug.DrawRay(grid.GetCellCenter(x,y), Vector2.up, Color.blue, 2);
            dragSelectEnd = new Vector2Int(x, y);

            if (currentDragSelectEnd == dragSelectEnd)
                return;
            currentDragSelectEnd = dragSelectEnd;

            int minX = Mathf.Min(dragSelectStart.x, dragSelectEnd.x);
            int maxX = Mathf.Max(dragSelectStart.x, dragSelectEnd.x);
            int minY = Mathf.Min(dragSelectStart.y, dragSelectEnd.y);
            int maxY = Mathf.Max(dragSelectStart.y, dragSelectEnd.y);

            selectedTileCount = 0;
            for (int i = 0; i < width; ++i)
                for (int j = 0; j < height; ++j)
                {
                    selectionMask[i, j] = i >= minX && i <= maxX && j >= minY && j <= maxY;
                    if (selectionMask[i, j])
                        selectedTileCount++;
                }



        }
    }
    private void StopDragSelect()
    {
        if (isDragSelecting)
        {

            isDragSelecting = false;
            for (int i = 0; i < width; ++i)
                for (int j = 0; j < height; ++j)
                    selectionMask[i, j] = false;
        }
    }
    public bool IsOneTileSelected()
    {
        return selectedTileCount <= 1;
    }
    public bool isTileHighlighted(int x, int y)
    {
        if (x > width - 1 || y > height - 1 || x < 0 || y < 0)
            return false;
        return selectionMask[x, y] || (x == mousePosX && y == mousePosY);
    }
}
