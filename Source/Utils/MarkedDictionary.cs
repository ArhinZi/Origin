using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    public class MarkedDictionary<K, T> : Dictionary<K, T>
    {
        private Func<T, K> GetMark;

        public MarkedDictionary(Func<T, K> GetMark)
        {
            this.GetMark = GetMark;
        }

        public K Add(T obj)
        {
            K id = GetMark(obj);
            base.TryAdd(id, obj);
            return id;
        }
    }
}