using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Origin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin
{
    class Tile:IDrawTile
    {
        public Texture2D texture;
        public Rectangle source;

        public Tile(Texture2D texture, Rectangle source, float depth = 0)
        {
            this.texture = texture;
            this.source = source;
            this.Depth = depth;
        }

        public float Depth { get; private set; } = 0;

        [Obsolete]
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Vector2 position)
        {
            spriteBatch.Draw(
                this.texture, 
                position: position,
                sourceRectangle: source,
                layerDepth: Depth);
        }
    }
}
