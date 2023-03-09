using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin
{
    public class InfoDrawer
    {
        
        SpriteFont font;
        Point position;
        Color color;
        string msg;

        public InfoDrawer(SpriteFont font, Point displayPosition, Color textColor)
        {
            this.font = font;
            
            this.position = displayPosition;
            this.color = textColor;
        }

        public void Set(string str)
        {
            msg = str;
        }

        public void Add(string str)
        {
            msg += str+"\n";
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(font, msg, position.ToVector2(), color);
        }
    }
}
