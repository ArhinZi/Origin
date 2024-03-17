using Microsoft.Xna.Framework;

using MonoGame.Extended;
using Arch.System;

using System.Collections.Generic;
using Origin.Source.Model;

namespace Origin.Source.ECS
{
    public class SystemGroupsManager
    {
        public List<Group<ulong>> Groups = new();
        private WorldTickManager WorldTimeManager;

        public SystemGroupsManager(WorldTickManager wtm)
        {
            WorldTimeManager = wtm;
        }

        public void Init()
        {
            foreach (var group in Groups)
            {
                group.Initialize();
            }
        }

        public void Tick(GameTime gameTime)
        {
            foreach (var group in Groups)
            {
                group.BeforeUpdate(WorldTimeManager.Ticks);
            }
            foreach (var group in Groups)
            {
                group.Update(WorldTimeManager.Ticks);
            }
            foreach (var group in Groups)
            {
                group.AfterUpdate(WorldTimeManager.Ticks);
            }
        }
    }
}