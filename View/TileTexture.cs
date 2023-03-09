using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.View
{
    class TileTexture
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
