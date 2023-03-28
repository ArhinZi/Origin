﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace Origin.Source
{
    public static class XMLoader
    {
        public static void LoadSprites(string xmlPath)
        {
            var xml = XDocument.Load(xmlPath);
            string textureName = xml.Root.Attribute("TextureName").Value;
            Texture2D texture = Texture.GetTextureByName(textureName);
            if (texture == null)
            {
                Debug.WriteLine(String.Format("ERROR: No texture with name {}", textureName));
                return;
            }

            var spriteElements = xml.Root.Elements("Sprite");
            foreach (var spriteElement in spriteElements)
            {
                var id = spriteElement.Element("SpriteID").Value;
                var sourceRect = ParseRectangle(spriteElement.Element("SourceRect").Value);

                new Sprite(id, texture, sourceRect);
            }
        }

        public static void LoadTerraMats(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);

            foreach (var matElem in doc.Root.Elements("Material"))
            {
                string matId = matElem.Element("MaterialID").Value;

                string matColorStr = matElem.Element("MaterialColor").Value;
                Color matColor;
                try
                {
                    matColor = ParseColor(matColorStr);
                    //matColor = new Color(12, 12, 12, 12);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Error '{0}' in {1} TerraMat", ex.Message, matId));
                    matColor = default;
                }

                Dictionary<string, Sprite> matSprites = new Dictionary<string, Sprite>();
                if (matElem.Element("SpriteDict") != null)
                {
                    foreach (var entryElem in matElem.Element("SpriteDict").Elements("Entry"))
                    {
                        string key = entryElem.Element("Key").Value;
                        string value = entryElem.Element("Value").Value;
                        Sprite sprite = Sprite.SpriteSet[value];
                        matSprites.Add(key, sprite);
                    }
                }

                new TerrainMaterial(matId, matSprites, matColor);
            }
        }

        private static Color ParseColor(string s)
        {
            string[] components = s.Split(' ');
            try
            {
                byte r = byte.Parse(components[0]);
                byte g = byte.Parse(components[1]);
                byte b = byte.Parse(components[2]);
                byte a = byte.Parse(components[3]);
                return new Color(r, g, b, a);
            }
            catch (Exception)
            {
                throw new FormatException(String.Format("Color parse error: {0}", s));
            }
        }

        private static Rectangle ParseRectangle(string s)
        {
            var values = s.Split(' ');
            var x = int.Parse(values[0]);
            var y = int.Parse(values[1]);
            var w = int.Parse(values[2]);
            var h = int.Parse(values[3]);
            return new Rectangle(x, y, w, h);
        }
    }
}