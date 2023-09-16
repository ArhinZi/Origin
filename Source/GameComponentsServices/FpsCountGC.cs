using Microsoft.Xna.Framework;

using MonoGame.Extended;

using System;

namespace Origin.Source.GameComponentsServices
{
    public class FpsCountGC : SimpleDrawableGameComponent
    {
        public uint Fps { get; private set; }
        public uint Ups { get; private set; }
        public float ElapsedTimeSec { get; private set; }

        private double frames = 0;
        private double updates = 0;
        private double elapsed = 0;
        private double last = 0;
        private double now = 0;

        public override void Update(GameTime gameTime)
        {
            now = gameTime.TotalGameTime.TotalSeconds;
            elapsed = now - last;
            if (elapsed > 1)
            {
                Fps = (uint)Math.Floor(frames / elapsed);
                Ups = (uint)Math.Floor(updates / elapsed);
                ElapsedTimeSec = (float)elapsed;

                elapsed = 0;
                frames = 0;
                updates = 0;
                last = now;
            }

            updates++;
        }

        public override void Draw(GameTime gameTime)
        {
            frames++;
        }
    }
}