using Microsoft.Xna.Framework;
using Origin.Source.Model.NewWorld;
using System.Collections.Generic;

namespace Origin.Source.Model.NewWorld.Systems
{
    public class SystemGroupsManager
    {
        public List<TickSystem> Systems = [];
        private WorldTickManager WorldTimeManager;
        private int _initializedCount;

        public int InitializedCount => _initializedCount;
        public string PendingInitSystemName => _initializedCount < Systems.Count ? Systems[_initializedCount].GetType().Name : string.Empty;

        public SystemGroupsManager(WorldTickManager wtm)
        {
            WorldTimeManager = wtm;
        }

        public void Init()
        {
            _initializedCount = 0;
            while (!InitNext(false))
            {
            }
        }

        public void LoadInit()
        {
            _initializedCount = 0;
            while (!InitNext(true))
            {
            }
        }

        public bool InitNext(bool load)
        {
            if (_initializedCount >= Systems.Count)
                return true;

            var system = Systems[_initializedCount];
            if (load)
                system.LoadInit();
            else
                system.Initialize();

            _initializedCount++;
            return _initializedCount >= Systems.Count;
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
