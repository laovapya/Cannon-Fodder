using UnityEngine;
using System.Collections;
using System;

public class BinHeap<T> where T : IHeapItem<T>
{
	private bool isMinHeap;
	private T[] items;
	private int currentItemCount;
	private Func<T, float> keySelector;

	public BinHeap(int maxHeapSize, Func<T, float> keySelector, bool isMinHeap = true)
	{
		items = new T[maxHeapSize];
		this.isMinHeap = isMinHeap;
		this.keySelector = keySelector;
	}

	private int Compare(T a, T b)
	{
		float ka = keySelector(a);
		float kb = keySelector(b);
		if (ka == kb) return 0;
		return (ka < kb) == isMinHeap ? 1 : -1;
	}
	public void UpdateItem(T item)
	{
		SortUp(item);
	}
	public void Add(T item)
	{
		item.heapIndex = currentItemCount;
		items[currentItemCount] = item;
		SortUp(item);
		currentItemCount++;
	}

	public T RemoveFirst()
	{
		T firstItem = items[0];
		currentItemCount--;
		items[0] = items[currentItemCount];
		items[0].heapIndex = 0;
		SortDown(items[0]);
		return firstItem;
	}

	public int Count { get { return currentItemCount; } }

	public bool Contains(T item)
	{
		return Equals(items[item.heapIndex], item);
	}

	void SortDown(T item)
	{
		while (true)
		{
			int childIndexLeft = item.heapIndex * 2 + 1;
			int childIndexRight = item.heapIndex * 2 + 2;
			int swapIndex = 0;

			if (childIndexLeft < currentItemCount)
			{
				//Debug.Log("heap item count " + currentItemCount);
				swapIndex = childIndexLeft;

				if (childIndexRight < currentItemCount)
				{
					//Debug.Log("SortDown " + childIndexRight);
					if (Compare(items[childIndexLeft], items[childIndexRight]) < 0)
						swapIndex = childIndexRight;
				}


				if (Compare(item, items[swapIndex]) < 0)
					Swap(item, items[swapIndex]);
				else
					return;
			}
			else
				return;
		}
	}

	void SortUp(T item)
	{
		int parentIndex = (item.heapIndex - 1) / 2;

		while (true)
		{
			T parentItem = items[parentIndex];
			//Debug.Log("SortUp " + parentIndex);
			if (Compare(item, parentItem) > 0)
				Swap(item, parentItem);
			else
				break;
			parentIndex = (item.heapIndex - 1) / 2;
		}
	}

	void Swap(T itemA, T itemB)
	{
		items[itemA.heapIndex] = itemB;
		items[itemB.heapIndex] = itemA;
		int itemAIndex = itemA.heapIndex;
		itemA.heapIndex = itemB.heapIndex;
		itemB.heapIndex = itemAIndex;
	}
	public void Clear()
	{
		Array.Clear(items, 0, currentItemCount);
		currentItemCount = 0;

		// Note: We don't need to reset heapIndex on items because:
		// 1. When items are added back, their heapIndex will be set correctly
		// 2. The heap doesn't maintain references to cleared items
	}
	// public void SetComparator(Func<T, T, int> compare)
	// {
	// 	this.compare = compare;
	// }
}

public interface IHeapItem<T>// : IComparable<T> // lower f cost
{
	int heapIndex { get; set; }
}