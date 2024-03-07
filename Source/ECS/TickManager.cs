using Microsoft.Xna.Framework;

using MonoGame.Extended;

using System.Collections.Generic;

namespace Origin.Source.ECS
{
    public class TickManager : IUpdate
    {
        public List<TickSystem> Systems = new();
        public long ticks { get; private set; } = 0;

        public void Init()
        {
            foreach (var system in Systems)
            {
                system.Init();
            }
        }

        public void Update(GameTime gameTime)
        {
            ticks++;
            foreach (var system in Systems)
            {
                system.Tick(ticks);
            }
        }
    }
}