using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class PathNode : IHeapItem<PathNode>
{
    public int heapIndex { get; set; }
    //a* values
    public int g = 0;
    public int h = 0;
    public int f = 0;
    public void CalculateFCost() { f = g + h; }


    public Vector2 position;


    //public bool isBusy = false;
    //public bool isWalkable = true;
    public PathNode previousCell;



    public int x { get; set; }
    public int y { get; set; }
    public List<PathNode> neighbors { get; } = new List<PathNode>();


    public PathNode(int x, int y, Vector3 position, bool isBusy, bool isWalkable)
    {
        this.position = position;

        this.x = x;
        this.y = y;

    }


    public static int CompareFcost(PathNode node1, PathNode node2)
    {
        int compare = node1.f.CompareTo(node2.f);
        if (compare == 0)
            compare = node1.h.CompareTo(node2.h);

        return -compare;
    }

    public static int CalculateHdistance(PathNode node1, PathNode node2)
    {
        int xDistance = Mathf.Abs(node2.x - node1.x);
        int yDistance = Mathf.Abs(node2.y - node1.y);

        int diagonalSteps = Mathf.Min(xDistance, yDistance);
        int straightSteps = Mathf.Abs(xDistance - yDistance);

        return diagonalSteps * diagCost + straightSteps * straightCost;
    }


    public static int straightCost = 10;
    public static int diagCost = 14;

}






