using Microsoft.Xna.Framework;
using Origin.Source.Model.NewWorld;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld.Systems
{
    public class SystemGroupsManager
    {
        public List<TickSystem> Systems = [];
        private WorldTickManager WorldTimeManager;

        public SystemGroupsManager(WorldTickManager wtm)
        {
            WorldTimeManager = wtm;
        }

        public void Init()
        {
            foreach (var system in Systems)
            {
                system.Initialize();
            }
        }

        public void LoadInit()
        {
            foreach (var system in Systems)
            {
                system.LoadInit();
            }
        }

        public void Tick(GameTime gameTime)
        {
            foreach (var system in Systems)
            {
                system.BeforeUpdate(WorldTimeManager.Ticks);
            }
            foreach (var system in Systems)
            {
                system.Update(WorldTimeManager.Ticks);
            }
            foreach (var system in Systems)
            {
                system.AfterUpdate(WorldTimeManager.Ticks);
            }
        }

        public void Draw(GameTime gameTime)
        {
            foreach (var system in Systems)
            {
                system.Draw(gameTime);
            }
        }
    }
}
