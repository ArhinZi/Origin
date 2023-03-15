using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace Origin.ECS
{
    public class FpsCountComp : SimpleDrawableGameComponent
    {
        public double frames = 0;
        public double updates = 0;
        public double elapsed = 0;
        public double last = 0;
        public double now = 0;
        public double msgFrequency = 1.0f;
        public string msg = "";

        /// <summary>
        /// The msgFrequency here is the reporting time to update the message.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            now = gameTime.TotalGameTime.TotalSeconds;
            elapsed = (double)(now - last);
            if (elapsed > msgFrequency)
            {
                msg = " Fps: " + ((int)(frames / elapsed)).ToString() + "\n Elapsed time: " + elapsed.ToString("##.##") + "\n Updates: " + updates.ToString() + "\n Frames: " + frames.ToString() + "\n";
                //Console.WriteLine(msg);
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