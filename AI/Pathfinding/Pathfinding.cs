using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public static class Pathfinding
{
    //A*
    public static List<PathNode> GetPathNodes(Vector3 pos1, Vector3 pos2)
    {
        PathNode startNode = PathGrid.instance.grid.GetValue(pos1);
        PathNode endNode = PathGrid.instance.grid.GetValue(pos2);

        if (startNode == null || endNode == null)
        {
            Debug.Log("incorrect start/end position for pathfinding");
            return null;
        }

        BinHeap<PathNode> openList = new BinHeap<PathNode>(MapGenerator.instance.worldGraph.width * MapGenerator.instance.worldGraph.height, (PathNode node) => node.f, true);
        List<PathNode> closedList = new List<PathNode>();

        MapGenerator.instance.worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            PathNode node = PathGrid.instance.grid.GetValue(x, y);
            if (node == null) return;
            node.h = int.MaxValue;
            node.g = int.MaxValue;
            node.f = int.MaxValue;
            node.previousCell = null;
        });



        startNode.g = 0;
        startNode.h = PathNode.CalculateHdistance(startNode, endNode);
        startNode.CalculateFCost();

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            PathNode currentNode = openList.RemoveFirst();
            if (currentNode.position == endNode.position) //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                return GetNodes(currentNode);


            closedList.Add(currentNode);
            foreach (PathNode node in currentNode.neighbors)
            {
                bool isDiagonal = (node.x != currentNode.x) && (node.y != currentNode.y);
                if (isDiagonal && (!BuildGrid.instance.GetIfWalkable(currentNode.x, node.y) || !BuildGrid.instance.GetIfWalkable(node.x, currentNode.y)))
                    continue; //can't move diagonally past corners
                if (!BuildGrid.instance.GetIfWalkable(node.x, node.y) || closedList.Contains(node))
                    continue;

                int tentativeG = currentNode.g + PathNode.CalculateHdistance(currentNode, node);
                bool isInOpenList = openList.Contains(node);
                if (tentativeG < node.g || !isInOpenList)
                {
                    node.previousCell = currentNode;
                    node.g = tentativeG;
                    node.h = PathNode.CalculateHdistance(node, endNode);
                    node.CalculateFCost();

                    if (!isInOpenList)
                        openList.Add(node);
                    else
                        openList.UpdateItem(node);
                }
            }
        }
        return null;


        List<PathNode> GetNodes(PathNode endNode)
        {
            List<PathNode> pathList = new List<PathNode>();
            PathNode currentNode = endNode;
            pathList.Add(currentNode);

            while (currentNode.previousCell != null)
            {
                pathList.Add(currentNode.previousCell);
                currentNode = currentNode.previousCell;
            }

            // If path contains only one node, duplicate it so movement logic sees 2 nodes
            if (pathList.Count == 1)
                pathList.Add(currentNode);

            pathList.Reverse();
            return pathList;
        }

    }
    public static Vector2[] GetPathVectors(Vector2 pos1, Vector2 pos2)
    {
        List<PathNode> path = GetPathNodes(pos1, pos2);
        if (path == null || path.Count == 0)
            return null;
        return GetPathVectors(path);
    }
    public static Vector2[] GetPathVectors(List<PathNode> path)
    {
        if (path == null)
            return null;
        int amount = path.Count;
        Vector2[] pathPoints = new Vector2[amount];
        for (int i = 0; i < amount; ++i)
            pathPoints[i] = path[i].position;
        return pathPoints;
    }
    public static Vector2[] GetStraightenedPath(Vector2 pos1, Vector2 pos2)
    {
        return GetPathVectors(GetStraightenedPathNodes(GetPathNodes(pos1, pos2)));
    }
    public static Vector2[] GetStraightenedPathVectors(List<PathNode> path)
    {
        return GetPathVectors(GetStraightenedPathNodes(path));
    }

    private static List<PathNode> GetStraightenedPathNodes(List<PathNode> path)
    {
        if (path == null)
            return null;
        if (path.Count <= 2)
            return path;

        List<PathNode> reducedPath = new List<PathNode>();
        reducedPath.Add(path[0]);

        int lastindex = path.Count - 1;

        Func<int, int, bool> F = (int x, int y) =>
        {
            PathNode node = PathGrid.instance.grid.GetValue(x, y);
            if (node == null) return false;

            if ((x == path[0].x && y == path[0].y) || (x == path[lastindex].x && y == path[lastindex].y)) return false; //treat last,first nodes as walkable/not busy


            if (node.neighbors.Count < 8)
                return true; //edge of map is considered busy

            int walkableNeighbors = 0; //dynamic corners 
            foreach (PathNode neighbor in node.neighbors)
            {
                if (BuildGrid.instance.GetIfWalkable(neighbor.x, neighbor.y))
                    walkableNeighbors++;
            }

            return BuildGrid.instance.GetIfBusy(node.x, node.y) || walkableNeighbors < 8;
        };

        int curIndex = 0;
        while (true)
        {
            bool isOK = false;
            for (int i = lastindex; i > curIndex; --i)
            {
                Vector2Int start = new Vector2Int(path[curIndex].x, path[curIndex].y);
                Vector2Int end = new Vector2Int(path[i].x, path[i].y);
                if (!MapGenerator.instance.worldGraph.RaycastBresenham(start, end, F))
                {
                    reducedPath.Add(path[i]);
                    curIndex = i;
                    isOK = true;
                    break;
                }
            }
            if (curIndex >= lastindex)
                break;
            if (!isOK)
            {

                reducedPath.Add(path[++curIndex]);
                //curIndex++;

                if (curIndex >= lastindex)
                    break;
            }
        }

        return reducedPath;
    }
    public static void DistortPath(Vector2[] path, float distortionRadius)
    {
        if (path == null) return;
        for (int i = 0; i < path.Length; ++i)
            path[i] += UtilMath.GetRandomCirclePoint(0, distortionRadius);
    }


    public static List<PathNode> GetSlopePathNodes(int x, int y, int maxSteps = 250)
    {
        PathNode current = PathGrid.instance.grid.GetValue(x, y);
        if (current == null) return null;

        var path = new List<PathNode>();
        var nextMap = PathGrid.instance.nextMap;
        var costMap = PathGrid.instance.zombiePreferenceMap;
        var visited = new HashSet<PathNode>();

        int steps = 0;
        while (true)
        {
            if (visited.Contains(current))
            {
                Debug.Log("loop detected in slope path");
                return null;
            }

            path.Add(current);
            visited.Add(current);

            if (costMap[current.x, current.y] == 0)
                break;

            Vector2Int next = nextMap[current.x, current.y];
            if (next == new Vector2Int(current.x, current.y))
                break;

            current = PathGrid.instance.grid.GetValue(next.x, next.y);
            if (current == null) break;

            steps++;
            if (steps > maxSteps)
            {
                Debug.Log("max steps exceeded");
                return null;
            }
        }
        return path;
    }
    public static List<PathNode> GetSlopePathNodes(Vector2 position, int maxSteps = 40)
    {
        int x, y;
        PathGrid.instance.grid.GetClampedXY(position, out x, out y);
        return GetSlopePathNodes(x, y, maxSteps);
    }

    public static Vector2[] GetSlopePathVectors(Vector2 position)
    {
        return GetPathVectors(GetSlopePathNodes(position));
    }
    public static void DrawPathGizmos(Vector2[] path, Color color) //debug
    {
        if (path == null || path.Length < 2) return;

        Gizmos.color = color;
        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 start = new Vector3(path[i].x, path[i].y, 0);
            Vector3 end = new Vector3(path[i + 1].x, path[i + 1].y, 0);
            Debug.DrawLine(start, end, color, 10f);
        }
    }


}




