using Arch.Core;
using Arch.System;

using Microsoft.Xna.Framework;
using Origin.Source.Model.NewWorld;

namespace Origin.Source.Model.NewWorld.Systems
{
    public class TickSystem : BaseSystem<Arch.Core.World, ulong>
    {
        public int Interval { get; private set; } = 1;

        protected Site _site;

        public TickSystem(Site site) : base(site.ArchWorld)
        {
            _site = site;
        }

        public virtual void LoadInit()
        {
        }

        public virtual void Draw(GameTime gameTime)
        {
        }
    }
}
