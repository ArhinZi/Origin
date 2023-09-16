using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Origin.Source.GameComponentsServices
{
    public class InfoDrawerGC : SimpleDrawableGameComponent, IGameInfoMonitor
    {
        public SpriteFont font;
        private Point position;
        private Color color;
        private string msg;

        public SpriteBatch spriteBatch;

        private bool visible = true;

        private SortedDictionary<uint, KeyValuePair<string, string>> data = new();

        public InfoDrawerGC(Point displayPosition, Color textColor, SpriteFont font, SpriteBatch batch)
        {
            position = displayPosition;
            color = textColor;
            this.font = font;
            spriteBatch = batch;
        }

        public override void Draw(GameTime gameTime)
        {
            if (visible)
            {
                msg = "";
                foreach (var item in data)
                {
                    msg += string.Format("{0}: {1}\n", item.Value.Key, item.Value.Value);
                }
                spriteBatch.Begin();
                spriteBatch.DrawString(font, msg, position.ToVector2(), color);
                spriteBatch.End();
            }
        }

        public override void Update(GameTime gameTime)
        {
        }

        public void Set(string name, string value, uint order)
        {
            /*if (data.ContainsKey(order))
                throw new Exception("This order already exist.");*/
            data[order] = KeyValuePair.Create(name, value);
        }

        public void Unset(string name)
        {
            foreach (var item in data)
            {
                if (item.Value.Key == name)
                {
                    data.Remove(item.Key);
                    return;
                }
            }
        }

        public void Switch(bool value)
        {
            visible = value;
        }

        public void Switch()
        {
            visible = !visible;
        }
    }
}