using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class UtilContainers
{
    public static void Spiral<T>(T[,] a, Func<int, int, bool> f)
    {
        int w = a.GetLength(0);
        int h = a.GetLength(1);

        int cx = w / 2;
        int cy = h / 2;

        int x = cx;
        int y = cy;

        int dx = 1, dy = 0;
        int step = 1;
        int visited = 0;
        int total = w * h;

        // visit center
        if (x >= 0 && x < w && y >= 0 && y < h)
            if (f(x, y)) return;
        visited++;

        // continue spiral until all visited
        while (visited < total)
        {
            for (int side = 0; side < 2; ++side)
            {
                for (int i = 0; i < step; ++i)
                {
                    x += dx;
                    y += dy;

                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        if (f(x, y)) return;
                        visited++;
                        if (visited >= total) return;
                    }
                }

                // rotate direction clockwise
                (dx, dy) = (dy, -dx);
            }

            step++;
        }
    }



    public static T GetRandomItem<T>(IReadOnlyCollection<T> array)
    {
        if (array == null || array.Count <= 0) return default(T);
        int index = UnityEngine.Random.Range(0, array.Count());
        return array.ElementAt(index);
    }
    public static List<T> FilteredList<T>(IReadOnlyCollection<T> array, Func<T, bool> isFiltered)
    {
        if (array == null)
            return null;
        List<T> filteredList = new List<T>();
        foreach (T item in array)
        {
            if (isFiltered(item))
                filteredList.Add(item);
        }
        return filteredList;
    }
    //public static bool Contains<T>(IReadOnlyCollection<T> array, Func<T, bool> isFiltered)
    //{
    //    if (array == null)
    //        return false;
    //    foreach (T item in array)
    //    {
    //        if (item != null && isFiltered(item))
    //            return true;
    //    }
    //    return false;
    //}

    public static void Sort<T>(List<T> list, Func<T, float> filter)
    {
        list.Sort((T t1, T t2) => { return (filter(t1)).CompareTo(filter(t2)); });
    }

    public static T FirstOf<T>(IReadOnlyList<T> array, Func<T, float> filter)
    {
        if (array == null)
            return default(T);
        List<T> list = new List<T>(array);
        //list.Sort((T t1, T t2) => { return (filter(t1)).CompareTo(filter(t2)); });
        Sort(list, filter);
        if (list.Count == 0)
            return default(T);
        return list[0];
    }


    //public static List<T> ShallowCopy<T>(IReadOnlyCollection<T> source) => new List<T>(source);

    //public static List<T> DeepCopy<T>(this List<T> source) where T : ICloneable
    //{
    //    List<T> copy = new List<T>();
    //    for (int i = 0; i < source.Count; ++i)
    //        copy.Add((T)source[i].Clone());
    //    return copy;
    //}
    //public static T[] DeepCopy<T>(this T[] source) where T : ICloneable
    //{
    //    int length = source.Length;
    //    T[] copy = new T[length];

    //    for (int i = 0; i < length; ++i)
    //        copy[i] = (T)source[i].Clone();

    //    return copy;
    //}
    //public static T[,,] DeepCopy<T>(this T[,,] source) where T : ICloneable
    //{
    //    int length = source.GetLength(0);
    //    int height = source.GetLength(1);
    //    int width = source.GetLength(2);
    //    T[,,] copy = new T[length, height, width];

    //    for (int i = 0; i < length; ++i)
    //        for (int j = 0; j < height; ++j)
    //            for (int k = 0; k < width; ++k)
    //                copy[i, j, k] = (T)source[i, j, k].Clone();

    //    return copy;
    //}


    //public static List<T> BreadthFirstSearch<T>(List<T> graph, T node, Func<T, T, bool> f)
    //{
    //    List<T> visited = new List<T>();
    //    Queue<T> queue = new Queue<T>();


    //    visited.Add(node);
    //    queue.Enqueue(node);

    //    while (queue.Count != 0)
    //    {
    //        T current = queue.Dequeue();
    //        Func<T, bool> f1 = (neighbor) => { return f(current, neighbor); };

    //        foreach (T n in FilteredList(graph, f1))
    //        {
    //            if (!visited.Contains(n))
    //            {
    //                visited.Add(n);
    //                queue.Enqueue(n);
    //            }
    //        }
    //    }

    //    return visited;
    //}

    public static void Shuffle<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }


}

