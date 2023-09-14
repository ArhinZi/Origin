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

        /// <summary>
        /// The msgFrequency here is the reporting time to update the message.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            now = gameTime.TotalGameTime.TotalSeconds;
            elapsed = now - last;
            avgTickTime = (avgTickTime + now - last2) / 2;
            if (elapsed > msgFrequency)
            {
                EventBus.Send(new UpdateFps((float)(frames / elapsed)));
                elapsed = 0;
                frames = 0;
                updates = 0;
                last = now;
            }
            last2 = now;
            updates++;
        }

        public override void Draw(GameTime gameTime)
        {
            frames++;
        }
    }
}