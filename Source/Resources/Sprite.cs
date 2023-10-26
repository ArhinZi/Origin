using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;

namespace Origin.Source.Resources
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

        L,
        R,
        T,
        B
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
        public string ID { get; }
        public Texture2D Texture { get; }
        public Rectangle RectPos { get; }
        public IsometricDirection Direction { get; }
        public MySpriteEffect Effect { get; }

        public Sprite(string id, Texture2D texture, Rectangle pos,
            IsometricDirection dir = IsometricDirection.NONE,
            MySpriteEffect effs = MySpriteEffect.None)
        {
            ID = id;
            Texture = texture;
            RectPos = pos;
            Direction = dir;
            Effect = effs;
        }
    }
}