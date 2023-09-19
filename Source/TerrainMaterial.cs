using Microsoft.Xna.Framework;

using System.Collections.Generic;

namespace Origin.Source
{
    public class TerrainMaterial
    {
        public static readonly Color UNKNOWN_COLOR = new Color(245, 66, 224);
        public static readonly string HIDDEN_MAT_ID = "HIDDEN";

        public string ID { get; private set; }
        public Dictionary<string, List<Sprite>> Sprites { get; private set; }
        public Color @Color { get; private set; }

        public TerrainMaterial(
            string name,
            Dictionary<string, List<Sprite>> sprites,
            Color c = default)
        {
            ID = name;
            Sprites = sprites;
            Color = c.Equals(default(Color)) ? UNKNOWN_COLOR : c;
        }
    }
}