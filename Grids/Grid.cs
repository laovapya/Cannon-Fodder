using System;
using System.Collections.Generic;
using UnityEngine;


public class Grid<TGridObject>
{
    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int y;
    }

    public int width { get; private set; }
    public int height { get; private set; }
    public float cellSize { get; private set; }
    public Vector2 originPosition { get; private set; }
    public TGridObject[,] array { get; private set; }

    public Grid(int width, int height, float cellSize, Vector2 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        array = new TGridObject[width, height];

        //bool showDebug = true;
        //if (showDebug)
        //{
        //    TextMesh[,] debugTextArray = new TextMesh[width, height];

        //    for (int x = 0; x < array.GetLength(0); x++)
        //    {
        //        for (int y = 0; y < array.GetLength(1); y++)
        //        {
        //            debugTextArray[x, y] = Util.CreateWorldText(PrefabReference.instance.folderDynamicObjects, array[x, y].ToString(), GetWorldPosition(x, y) + new Vector2(cellSize, cellSize) * .5f, 15);
        //            Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
        //            Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
        //        }
        //    }
        //    Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
        //    Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

        //    OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) =>
        //    {
        //        debugTextArray[eventArgs.x, eventArgs.y].text = array[eventArgs.x, eventArgs.y].ToString();
        //    };
        //}
    }
    //public static Grid<TGridObject> CreateFrom<TOther>(Grid<TOther> sourceGrid) 
    //{
    //    return new Grid<TGridObject>(
    //        sourceGrid.width,
    //        sourceGrid.height,
    //        sourceGrid.cellSize,
    //        sourceGrid.originPosition
    //    );
    //}

    //public static Grid<T> CreateGraphGridBase<T>() where T : IGraphNode<TGridObject>, new()
    //{
    //    Grid<T> grid = new Grid<T>(
    //        MapGenerator.instance.playableAreaWidth,
    //        MapGenerator.instance.playableAreaHeight,
    //        MapGenerator.instance.cellSize,
    //        MapGenerator.instance.origin
    //    );


    //    for (int x = 0; x < grid.width; x++)
    //        for (int y = 0; y < grid.height; y++)
    //            grid.SetValue(x, y, new T());

    //    return grid;
    //}
    //public static Grid<T> CreateGraphGridBase<T>(int width, int height, int cellSize, Vector2 origin) where T : IGraphNode<TGridObject>, new()
    //{
    //    Grid<T> grid = new Grid<T>(
    //        width,
    //        height,
    //        cellSize,
    //        origin
    //    );


    //    for (int x = 0; x < grid.width; x++)
    //        for (int y = 0; y < grid.height; y++)
    //            grid.SetValue(x, y, new T());

    //    return grid;
    //}
    public Vector2 GetCellCenter(Vector2 worldPosition)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return new Vector2(x, y) * cellSize + new Vector2(cellSize, cellSize) / 2 + originPosition;
    }
    public Vector2 GetCellCenter(int x, int y)
    {
        return new Vector2(x, y) * cellSize + new Vector2(cellSize, cellSize) / 2 + originPosition;
    }
    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(x, y) * cellSize + originPosition;
    }

    private void GetXY(Vector2 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }
    public void GetClampedXY(Vector2 worldPosition, out int x, out int y)
    {
        GetXY(worldPosition, out x, out y);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
    }
    //public TGridObject GetClosestCell(Vector2 worldPosition)
    //{
    //    Vector3 localPosition = (worldPosition - originPosition) / cellSize;

    //    float percentX = localPosition.x / width;
    //    float percentY = localPosition.y / height;


    //    percentX = Mathf.Clamp01(percentX);
    //    percentY = Mathf.Clamp01(percentY);


    //    int x = Mathf.RoundToInt((width - 1) * percentX);
    //    int y = Mathf.RoundToInt((height - 1) * percentY);
    //    return array[x, y];
    //}
    public Vector2 GetClosestPosition(Vector2 worldPosition) // ?????????????????????????????????????????????????
    {
        //float x = Mathf.Clamp(worldPosition.x, originPosition.x, originPosition.x + width * cellSize);
        //float y = Mathf.Clamp(worldPosition.y, originPosition.y, originPosition.y + height * cellSize);
        //int cellsProcessed = 0;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        int x, y;
        GetClampedXY(worldPosition, out x, out y);
        Vector2Int start = new Vector2Int(x, y);
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0) //&& cellsProcessed < sideSize * sideSize
        {
            Vector2Int current = queue.Dequeue();
            //if (MapGenerator.instance.grid.array[current.x, current.y]) //if walkable
            return GetCellCenter(current.x, current.y);


            //foreach (Vector2Int neighbor in MapGenerator.instance.GetNeighbors(current, true, width, height))
            //{
            //    if (!visited.Contains(neighbor))
            //    {
            //        visited.Add(neighbor);
            //        queue.Enqueue(neighbor);
            //    }
            //}
        }
        return new Vector2(x, y);
    }
    public Vector2 GetClampedPosition(Vector2 worldPosition)
    {
        float x = Mathf.Clamp(worldPosition.x, originPosition.x, originPosition.x + width * cellSize);
        float y = Mathf.Clamp(worldPosition.y, originPosition.y, originPosition.y + height * cellSize);
        return new Vector2(x, y);
    }

    public void SetValue(int x, int y, TGridObject value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            array[x, y] = value;
            if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
        }
    }

    public void SetValue(Vector2 worldPosition, TGridObject value)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetValue(x, y, value);
    }
    public TGridObject GetValue(Vector2 worldPosition)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetValue(x, y);
    }
    public TGridObject GetValue(int x, int y)
    {
        if (IsInBounds(x, y)) // && MapGenerator.instance.grid.array[x,y]
        {
            return array[x, y];
        }
        else
        {
            return default(TGridObject);
        }
    }
    public bool IsInBounds(int x, int y)
    {
        return (x >= 0 && y >= 0 && x < width && y < height);
    }


    public List<Vector2Int> GetNeighbors(Vector2Int current, bool includeCorners)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0 || (!includeCorners && (dx + dy) % 2 == 0))
                    continue;

                int nx = current.x + dx;
                int ny = current.y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    neighbors.Add(new Vector2Int(nx, ny));

            }
        }
        return neighbors;
    }



    //remove mapgenerator from here? 
    //public void ForEachCell(Action<int, int> action)
    //{
    //    for (int x = 0; x < width; x++)
    //        for (int y = 0; y < height; y++)
    //            if (MapGenerator.instance.IsAreaPlayable(x, y)) action(x, y);
    //}
    //public void ForEachCell(Action<TGridObject> action)
    //{
    //    for (int x = 0; x < width; x++)
    //        for (int y = 0; y < height; y++)
    //            if (MapGenerator.instance.IsAreaPlayable(x, y)) action(GetValue(x, y));
    //}
}
