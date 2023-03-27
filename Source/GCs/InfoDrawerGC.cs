using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;

namespace Origin.Source.GCs
{
    public class InfoDrawerGC : SimpleDrawableGameComponent
    {
        public SpriteFont font;
        private Point position;
        private Color color;
        private string msg;

        public SpriteBatch spriteBatch;

        public InfoDrawerGC(Point displayPosition, Color textColor)
        {
            position = displayPosition;
            color = textColor;
        }

        public void Set(string str)
        {
            msg = str;
        }

        public void Clear()
        {
            msg = "";
        }

        public void Add(string str)
        {
            msg += str + "\n";
        }

        public override void Draw(GameTime gameTime)
        {
            //if (spriteBatch != null)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(font, msg, position.ToVector2(), color);
                spriteBatch.End();
            }
        }

        public override void Update(GameTime gameTime)
        {
        }
    }
}