using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;

namespace Origin.Source
{
    public enum IsometricDirection
    {
        /// <summary>
        /// No defined direction
        /// </summary>
        NONE,

        /// <summary>
        /// Bottom Left direction
        /// </summary>
        BL,

        /// <summary>
        /// Bottom Right direction
        /// </summary>
        BR,

        /// <summary>
        /// Top Left direction
        /// </summary>
        TL,

        /// <summary>
        /// Top Right direction
        /// </summary>
        TR,
    }

    [Flags]
    public enum MySpriteEffect
    {
        None = 0,

        //(-)
        FlipHorizontally = 0b1,

        //(|)
        FlipVertically = 0b10,

        //(/)
        FlipBLTR = 0b100,

        //(\)
        FlipTLBR = 0b1000
    }

    public class Sprite
    {
        public static readonly Point TILE_SIZE = new Point(64, 32);
        public static readonly Point SPRITE_SIZE = new Point(64, 64);
        public static readonly int FLOOR_YOFFSET = 8;

        public static Dictionary<string, Sprite> SpriteSet { get; private set; } = new Dictionary<string, Sprite>();

        public readonly string ID;
        public readonly Texture2D Texture;
        public readonly Rectangle RectPos;
        public IsometricDirection Direction;
        public MySpriteEffect Effect;

        public Sprite(string id, Texture2D texture, Rectangle pos,
            IsometricDirection dir = IsometricDirection.NONE,
            MySpriteEffect effs = MySpriteEffect.None)
        {
            ID = id;
            Texture = texture;
            RectPos = pos;
            Direction = dir;
            Effect = effs;

            SpriteSet.Add(ID, this);
        }
    }
}