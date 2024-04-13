using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;

using Origin.Source.Resources.Converters;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;

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

    public class Sprite : IDKeeper
    {
        public Texture2D Texture { get; }
        public Rectangle RectPos { get; }
        public IsometricDirection Direction { get; }
        public MySpriteEffect Effect { get; }

        public Dictionary<string, List<Sprite>> Rotations { get; }

        public Sprite(string id, Texture2D texture, Rectangle pos,
            IsometricDirection dir = IsometricDirection.NONE,
            MySpriteEffect effs = MySpriteEffect.None, Dictionary<string, List<Sprite>> rotations = null)
        {
            ID = id;
            Texture = texture;
            RectPos = pos;
            Direction = dir;
            Effect = effs;
            Rotations = rotations;

            //if (rotations != null)
            //    foreach (var rot in rotations)
            //    {
            //        var list = new List<Sprite>();
            //        Rotations.Add(rot.Key, list);
            //        foreach (var strs in rot.Value)
            //        {
            //            list.Add(GlobalResources.Sprites[strs]);
            //        }
            //    }
        }

        public override string ToString()
        {
            return $"{ID}, {Direction.ToString()}";
        }
    }
}