using System.Collections.Generic;

namespace Origin.Source.Events
{
    public class DebugValueChanged
    {
        public int order = 0;
        public Dictionary<string, string> values;

        public DebugValueChanged(int order, Dictionary<string, string> keyValuePairs)
        {
            this.order = order;
            values = keyValuePairs;
        }
    }
}