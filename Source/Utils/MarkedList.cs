using Arch.LowLevel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    public class MarkedList<T> : List<T> where T : IDKeeper
    {
        private Func<T> GetMark;
        private Dictionary<string, int> cache = [];

        public MarkedList()
        {
        }

        public MarkedList(int capacity) : base(capacity)
        {
        }

        public MarkedList(IEnumerable<T> collection) : base(collection)
        {
            for (int i = 0; i < this.Count(); i++)
            {
                cache.Add(this[i].ID, i);
            }
        }

        public new void Add(T item)
        {
            base.Add(item);
            cache.Add(this.Last().ID, this.Count - 1);
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            base.AddRange(collection);
            var list = collection.ToArray();
            for (int i = 0; i < collection.Count(); i++)
            {
                cache.Add(list[i].ID, i);
            }
        }

        public new void Clear()
        {
            base.Clear();
            cache.Clear();
        }

        public new bool Remove(T item)
        {
            bool res = base.Remove(item);
            if (res)
                cache.Remove(item.ID);
            return res;
        }

        public T this[string ID]
        {
            get
            {
                //return this[cache[ID]];
                if (cache.TryGetValue(ID, out int index))
                {
                    return this[index];
                }
                throw new KeyNotFoundException($"ID '{ID}' not found in MarkedList.");
            }
        }

        public int IndexOf(string ID)
        {
            //return cache[ID];
            if (cache.TryGetValue(ID, out int index))
            {
                return index;
            }
            throw new KeyNotFoundException($"ID '{ID}' not found in MarkedList.");
        }
    }
}