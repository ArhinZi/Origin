using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Origin.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Draw
{
    class TileSet
    {
        public static readonly int WALL_COUNT = 32;
        public static readonly int FLOOR_COUNT = 32;

        public static readonly Point TILE_SIZE = new Point(32, 16);
        public static readonly int FLOOR_YOFFSET = 4;

        public static TileTexture[] WallSet { get; private set; }
        public static TileTexture[] FloorSet { get; private set; }

        public static Texture2D texture;
        public TileSet()
        {
            texture = MainGame.instance.Content.Load<Texture2D>("default");

            WallSet = new TileTexture[WALL_COUNT];
            WallSet[1] = new TileTexture(0, "RoughStoneWall", texture, new Rectangle(32, 72, 32, 32));
            WallSet[0] = new TileTexture(1, "BlankWall", texture, new Rectangle(256, 20, 32, 32));

            FloorSet = new TileTexture[FLOOR_COUNT];
            FloorSet[1] = new TileTexture(0, "RoughStoneFloor", texture, new Rectangle(32, 52, 32, 20));
            FloorSet[0] = new TileTexture(1, "BlankFloor", texture, new Rectangle(256, 0, 32, 20));

        }
    }
}
