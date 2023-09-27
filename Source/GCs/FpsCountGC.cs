using Arch.Bus;

using Microsoft.Xna.Framework;

using MonoGame.Extended;

using Origin.Source.Events;

namespace Origin.Source.GCs
{
    public class FpsCountGC : SimpleDrawableGameComponent
    {
        public double frames = 0;
        public double updates = 0;
        public double elapsed = 0;
        public double avgTickTime = 0;
        public double last = 0;
        public double last2 = 0;
        public double now = 0;
        public double msgFrequency = 1.0f;
        public string msg = "";

        private int fps = 0;
        private int min = 0;
        private int max = 0;

        /// <summary>
        /// The msgFrequency here is the reporting time to update the message.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            now = gameTime.TotalGameTime.TotalSeconds;
            elapsed = now - last;
            avgTickTime = (avgTickTime + now - last2) / 2;

            min = min < fps ? min : fps;
            max = max > fps ? max : fps;

            if (elapsed > msgFrequency)
            {
                EventBus.Send(new UpdateFps(fps, min, max));
                elapsed = 0;
                frames = 0;
                updates = 0;
                last = now;
                min = max = fps;
            }
            last2 = now;
            updates++;
        }

        public override void Draw(GameTime gameTime)
        {
            fps = (int)(1f / (float)gameTime.ElapsedGameTime.TotalSeconds);
            frames++;
        }
    }
}