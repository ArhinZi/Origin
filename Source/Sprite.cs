using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System.Collections.Generic;

namespace Origin.Source
{
    public enum SpriteDirection
    {
        /// <summary>
        /// No defined direction
        /// </summary>
        NONE,

        /// <summary>
        /// Top Left direction
        /// </summary>
        TL,

        /// <summary>
        /// Top Right direction
        /// </summary>
        TR,

        /// <summary>
        /// Bottom Left direction
        /// </summary>
        BL,

        /// <summary>
        /// Bottom Right direction
        /// </summary>
        BR
    }

    public class Sprite
    {
        public static readonly Point TILE_SIZE = new Point(32, 16);
        public static readonly int FLOOR_YOFFSET = 3;

        public static Dictionary<string, Sprite> SpriteSet { get; private set; } = new Dictionary<string, Sprite>();

        public readonly string ID;
        public readonly Texture2D Texture;
        public readonly Rectangle RectPos;
        public SpriteDirection Direction;
        public SpriteEffects Effect;

        public Sprite(string id, Texture2D texture, Rectangle pos,
            SpriteDirection dir = SpriteDirection.NONE,
            SpriteEffects effs = SpriteEffects.None)
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