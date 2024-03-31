using Microsoft.Xna.Framework;

using MonoGame.Extended;
using Arch.System;

using System.Collections.Generic;
using Origin.Source.Model;

namespace Origin.Source.ECS
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
    }
}