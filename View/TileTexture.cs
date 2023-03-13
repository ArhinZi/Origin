using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Origin.View
{
    internal class TileTexture
    {
        public readonly uint ID;
        public readonly Texture2D Texture;
        public readonly Rectangle RectPos;
        public readonly string Name;

        public TileTexture(uint id, string name, Texture2D texture, Rectangle pos)
        {
            ID = id;
            Name = name;
            Texture = texture;
            RectPos = pos;
        }
    }
}