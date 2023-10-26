using Microsoft.Xna.Framework;

using Origin.Source.Utils;

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace Origin.Source
{
    public class Material
    {
        public static Dictionary<string, Material> Materials { get; private set; } = new();

        public static HashSet<string> Types { get; private set; } = new();

        public string ID { get; private set; }
        public string Name { get; private set; }
        public Color Color { get; private set; }
        public string Type { get; private set; }
        public float Value { get; private set; }

        public Material(string ID, string Name, Color Color, string Type, float Value)
        {
            this.ID = ID;
            this.Name = Name;
            this.Color = Color;
            this.Type = Type;
            this.Value = Value;

            Types.Add(Type);
            Materials.Add(this.ID, this);
        }
    }
}