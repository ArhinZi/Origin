namespace Origin.Source.Utils
{
    /// <summary>
    /// Not common circular array. Have ability to add elements to tail and to head of circle.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CircleSliceArray<T>
    {
        private T[] items;

        private int head;
        private int tail;

        private int size;

        public int Start { get; set; }

        public int Count
        {
            get
            {
                return (tail - head + size) % size;
            }
        }

        public CircleSliceArray(int size)
        {
            items = new T[size + 1];
            head = 0;
            tail = 0;
            this.size = size + 1;
        }

        public void SetStart(int s)
        {
            Start = s;
        }

        public void AddHead(T item)
        {
            head = (head - 1 + size) % size;
            items[head] = item;

            if (tail == head)
            {
                tail = (tail - 1 + size) % size;
            }
            Start--;
        }

        public void AddTail(T item)
        {
            items[tail] = item;
            tail = (tail + 1) % size;

            if (tail == head)
            {
                head = (head + 1) % size;
                Start++;
            }
        }

        public T this[int index]
        {
            set
            {
                if (index == Start + Count)
                {
                    AddTail(value);
                }
                else if (index == Start - 1)
                {
                    AddHead(value);
                }
                else if (index >= Start && index < Start + Count)
                {
                    items[index % size] = value;
                }
                else
                {
                    Start = index;
                    head = tail = index % size;
                    AddTail(value);
                }
            }
            get
            {
                return items[index % size];
            }
        }

        public override string ToString()
        {
            string s = "";
            for (int i = Start; i < Start + Count; i++)
            {
                s += i.ToString() + ",";
            }
            s.Remove(s.Length - 1, 1);
            return s;
        }
    }
}