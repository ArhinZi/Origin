using MonoGame.Extended.Sprites;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Resources
{
    public class Component
    {
        public string ItemID { get; set; }
        public int Amount { get; set; }
    }

    public class Construction
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public bool ConstructAble { get; set; }
        public bool RotateAble { get; set; }
        public bool HasMaterialColor { get; set; }
        public string Type { get; set; }
        public string WallRemovedConstruction { get; set; }
        public string Category { get; set; }
        public Dictionary<string, List<string>> Sprites { get; set; }
        public List<Component> Components { get; set; }

        public override string ToString()
        {
            return ID;
        }
    }
}