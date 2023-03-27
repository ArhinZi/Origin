using Microsoft.Xna.Framework;

using System.Collections.Generic;

namespace Origin.Source
{
    public class TerrainMaterial
    {
        public static Dictionary<string, TerrainMaterial> TerraMats { get; private set; } =
            new Dictionary<string, TerrainMaterial>();

        public static readonly Color UNKNOWN_COLOR = new Color(245, 66, 224);

        public string ID { get; private set; }
        public Dictionary<string, Sprite> Sprites { get; private set; }

        public Color TerraColor { get; private set; }

        public TerrainMaterial(
            string name,
            Dictionary<string, Sprite> sprites,
            Color c = default)
        {
            ID = name;
            Sprites = sprites;
            TerraColor = c.Equals(default(Color)) ? UNKNOWN_COLOR : c;

            //if (UseItemColor) TerraColor = GetItemColor();

            TerraMats.Add(ID, this);
        }

        public Color GetItemColor()
        {
            return UNKNOWN_COLOR;
        }
    }
}