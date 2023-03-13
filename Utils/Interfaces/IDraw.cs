using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Origin.Interfaces
{
    internal interface IDraw
    {
        void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    }
}