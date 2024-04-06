using System;
using System.Collections.Generic;

namespace Origin.Source.Model.Pathfind.old
{
    internal sealed class MinHeap<T>
        where T : IComparable<T>
    {
        private readonly List<T> Items;

        public MinHeap()
        {
            Items = [];
        }

        public int Count => Items.Count;

        public T Peek() => Items[0];

        public void Insert(T item)
        {
            Items.Add(item);
            SortItem(item);
        }

        public T Extract()
        {
            var node = Items[0];

            ReplaceFirstItemWithLastItem();
            Heapify(0);

            return node;
        }

        public void Remove(T item)
        {
            if (Count < 2)
            {
                Clear();
            }
            else
            {
                var index = Items.IndexOf(item);
                if (index >= 0)
                {
                    Items[index] = Items[Items.Count - 1];
                    Items.RemoveAt(Items.Count - 1);

                    Heapify(0);
                }
            }
        }

        public void Clear() => Items.Clear();

        private void ReplaceFirstItemWithLastItem()
        {
            Items[0] = Items[Items.Count - 1];
            Items.RemoveAt(Items.Count - 1);
        }

        private void SortItem(T item)
        {
            var index = Items.Count - 1;

            while (HasParent(index))
            {
                var parentIndex = GetParentIndex(index);
                if (ItemAIsSmallerThanItemB(item, Items[parentIndex]))
                {
                    Items[index] = Items[parentIndex];
                    index = parentIndex;
                }
                else
                {
                    break;
                }
            }

            Items[index] = item;
        }

        private void Heapify(int startIndex)
        {
            var bestIndex = startIndex;

            if (HasLeftChild(startIndex))
            {
                var leftChildIndex = GetLeftChildIndex(startIndex);
                if (ItemAIsSmallerThanItemB(Items[leftChildIndex], Items[bestIndex]))
                {
                    bestIndex = leftChildIndex;
                }
            }

            if (HasRightChild(startIndex))
            {
                var rightChildIndex = GetRightChildIndex(startIndex);
                if (ItemAIsSmallerThanItemB(Items[rightChildIndex], Items[bestIndex]))
                {
                    bestIndex = rightChildIndex;
                }
            }

            if (bestIndex != startIndex)
            {
                var temp = Items[bestIndex];
                Items[bestIndex] = Items[startIndex];
                Items[startIndex] = temp;
                Heapify(bestIndex);
            }
        }

        private static bool ItemAIsSmallerThanItemB(T a, T b) => a.CompareTo(b) < 0;

        private static bool HasParent(int index) => index > 0;

        private bool HasLeftChild(int index) => GetLeftChildIndex(index) < Items.Count;

        private bool HasRightChild(int index) => GetRightChildIndex(index) < Items.Count;

        private static int GetParentIndex(int i) => (i - 1) / 2;

        private static int GetLeftChildIndex(int i) => 2 * i + 1;

        private static int GetRightChildIndex(int i) => 2 * i + 2;
    }
}