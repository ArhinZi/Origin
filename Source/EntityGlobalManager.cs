using System.Collections.Generic;

namespace Origin.Source
{
    public class EntityGlobalManager
    {
        public static List<string> MaterialTypes { get; private set; }

        public EntityGlobalManager()
        {
            MaterialTypes = new List<string>()
            {
                ""
            };
        }
    }
}