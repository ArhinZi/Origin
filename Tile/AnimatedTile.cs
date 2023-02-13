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
    class AnimatedTile : IDrawTile, IUpdate
    {
        Texture2D texture;
        List<Rectangle> sources;
        int currentTime = 0; // сколько времени прошло
        int period = 50; // частота обновления в миллисекундах
        int currentFrame = 0;
        int maxFrames;

        public float Depth { get; private set; } = 0;

        public AnimatedTile(Texture2D texture, List<Rectangle> sources, int period, float depth = 0)
        {
            this.texture = texture;
            this.sources = sources;
            this.period = period;
            this.Depth = depth;
            this.maxFrames = sources.Count();
        }

        public void Update(GameTime gameTime)
        {
            currentTime += gameTime.ElapsedGameTime.Milliseconds;
            if (currentTime > period)
            {
                currentTime -= period;
                currentFrame++;
                if (currentFrame >= maxFrames) currentFrame = 0;
            }
        }

        [Obsolete]
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Vector2 position)
        {
            spriteBatch.Draw(
                this.texture,
                position: position,
                sourceRectangle: sources[currentFrame],
                layerDepth: Depth);
        }

        
    }
}
