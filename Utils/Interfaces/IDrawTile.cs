using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Origin.Interfaces
{
    internal interface IDrawTile
    {
        float Depth { get; }

        void Draw(GameTime gameTime, SpriteBatch spriteBatch, Vector2 position);
    }
}