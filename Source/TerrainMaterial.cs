using Microsoft.Xna.Framework;

using System.Collections.Generic;

namespace Origin.Source
{
    public class TerrainMaterial
    {
        public static Dictionary<string, TerrainMaterial> TerraMats { get; private set; } =
            new Dictionary<string, TerrainMaterial>();

        public static readonly Color UNKNOWN_COLOR = new Color(245, 66, 224);
        public static readonly string AIR_NULL_MAT_ID = "Air";
        public static readonly string HIDDEN_MAT_ID = "Hidden";

        public string ID { get; private set; }
        public bool IsEmbedded { get; private set; }
        public Dictionary<string, List<Sprite>> Sprites { get; private set; }

        public Color TerraColor { get; private set; }

        public TerrainMaterial(
            string name,
            Dictionary<string, List<Sprite>> sprites,
            Color c = default,
            bool isEmbedded = false)
        {
            ID = name;
            IsEmbedded = isEmbedded;
            Sprites = sprites;
            TerraColor = c.Equals(default(Color)) ? UNKNOWN_COLOR : c;

            //if (UseItemColor) TerraColor = GetItemColor();

            //if(TerraMats.Keys)
            TerraMats.Add(ID, this);
        }

        public Color GetItemColor()
        {
            return UNKNOWN_COLOR;
        }
    }
}