using Microsoft.Xna.Framework;

using Origin.Source.Model.Site;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    public class TickSystem
    {
        public int Interval { get; private set; } = 1;
        public bool Debug = true;
        public long LastElapsedMilliseconds = 0;

        private Stopwatch watch;
        protected Site _site;

        public TickSystem(Site site)
        {
            _site = site;
        }

        public virtual void Init()
        { }

        protected virtual void DoTick()
        {
        }

        public virtual void Tick(long ticks)
        {
            if (ticks % Interval == 0)
            {
                if (Debug)
                {
                    watch = Stopwatch.StartNew();
                }

                DoTick();

                if (Debug)
                {
                    watch.Stop();
                    LastElapsedMilliseconds = watch.ElapsedMilliseconds;
                }
            }
        }
    }
}