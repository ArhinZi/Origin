using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

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
        public static readonly Point TILE_SIZE = new Point(32, 16);
        public static readonly Point SPRITE_SIZE = new Point(32, 32);
        public static readonly int FLOOR_YOFFSET = 4;

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

        public static void LoadSpritesFromXML(string xmlPath)
        {
            var xml = XDocument.Load(xmlPath);
            string textureName = xml.Root.Attribute("TextureName").Value;
            Texture2D texture = Source.Texture.GetTextureByName(textureName);
            if (texture == null)
            {
                Debug.WriteLine(string.Format("ERROR: No texture with name {}", textureName));
                return;
            }

            var spriteElements = xml.Root.Elements("Sprite");
            foreach (var spriteElement in spriteElements)
            {
                var id = spriteElement.Element("SpriteID").Value;
                var sourceRect = XMLoaders.ParseRectangle(spriteElement.Element("SourceRect").Value);
                var dir = spriteElement.Element("SpriteDir") != null ?
                    (IsometricDirection)Enum.Parse(typeof(IsometricDirection), spriteElement.Element("SpriteDir").Value) :
                    IsometricDirection.NONE;

                var effect = MySpriteEffect.None;
                if (spriteElement.Element("SpriteEffect") != null)
                {
                    string s = spriteElement.Element("SpriteEffect").Value;
                    var ss = s.Split('|');
                    foreach (var item in ss)
                    {
                        effect |= (MySpriteEffect)Enum.Parse(typeof(MySpriteEffect), item);
                    }
                }

                new Sprite(id, texture, sourceRect, dir, effect);
            }
        }
    }
}