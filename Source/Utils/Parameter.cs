using System;

namespace Origin.Source.Utils
{
    public interface IMetaParameter
    {
        public IMetaParameter Clone();

        /*public static T GetValue<T>()
        {
            return default(T);
        }*/
    }

    public class Parameter<T> : IMetaParameter where T : IComparable<T>
    {
        public string Name { get; private set; }
        private T _value;

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                if (_value.CompareTo(Min) < 0)
                    _value = Min;
                if (_value.CompareTo(Max) > 0)
                    _value = Max;
            }
        }

        public T Min { get; private set; }
        public T Max { get; private set; }

        public Parameter(T value, T min, T max, string name)
        {
            Min = min;
            Max = max;
            Value = value;

            Name = name;
        }

        public IMetaParameter Clone()
        {
            return new Parameter<T>(Value, Min, Max, Name);
        }

        /*public static T GetValue<T>(Parameter<T> parameter)
        {
            return (parameter).Value;
        }*/
    }
}