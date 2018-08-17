using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heap<T> where T : IHeapItem<T>
{
	T[] items;
	int currentItemCount;

	public Heap(int maxHeapSize)
	{
		items = new T[maxHeapSize];
	}

	public void Add(T item)
	{
		item.HeapIndex = currentItemCount;
		items[currentItemCount] = item;
		SortUp(item);
		currentItemCount++;
	}

	public T RemoveFirst()
	{
		T firstItem = items[0];
		currentItemCount--;
		items[0] = items[currentItemCount];
		items[0].HeapIndex = 0;
		SortDown(items[0]);
		return firstItem;
	}

	public void UpdateItem(T item)
	{
		SortUp(item);
	}

	public bool Contains(T item)
	{
		return Equals(items[item.HeapIndex], item);
	}

	public int Count
	{
		get {
			return currentItemCount;
		}
	}

	// Called when removing the top item
	void SortDown(T item)
	{
		while (true)
		{
			int childIndexLeft = item.HeapIndex * 2 + 1;
			int childIndexRight = item.HeapIndex * 2 + 2;
			int swapIndex = 0;

			if (childIndexLeft < currentItemCount) // Has at least one child
			{
				swapIndex = childIndexLeft;

				if (childIndexRight < currentItemCount) // Has second child
				{
					if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
					{
						swapIndex = childIndexRight;
					} // 2nd child bigger
				} // 2 child

				// Swap required to maintain proper ordering
				if (item.CompareTo(items[swapIndex]) < 0)
				{
					Swap(item, items[swapIndex]);
				}
				else // Parent in correct position
				{
					return;
				}
			} // 1 child
			else // No children, already in correct position
			{
				return;
			}
		}
	}

	// Called when inserting a new item
	void SortUp(T item)
	{
		// Formula to calculate index of parent from a child's index
		int parentIndex = (item.HeapIndex - 1) / 2;

		while (true)
		{
			// Parent should always be bigger than both of its children
			T parentItem = items[parentIndex];
			if (item.CompareTo(parentItem) > 0) // Child bigger than parent
			{
				// so swap them
				Swap(item, parentItem);
			}
			else
			{
				// Already in its correct place, no reason to continue checking
				break;
			}
			// New parent index
			parentIndex = (item.HeapIndex - 1) / 2;
		}
	}

	// Swap indices
	void Swap(T itemA, T itemB)
	{
		items[itemA.HeapIndex] = itemB;
		items[itemB.HeapIndex] = itemA;
		int itemAIndex = itemA.HeapIndex; // Temporarily holds index of A
		itemA.HeapIndex = itemB.HeapIndex;
		itemB.HeapIndex = itemAIndex;
	}
}

public interface IHeapItem<T> : IComparable<T>
{
	int HeapIndex
	{
		get;
		set;
	}
}