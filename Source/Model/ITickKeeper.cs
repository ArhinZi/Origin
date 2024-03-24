using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model
{
    internal interface ITickKeeper
    {
        public void BeforeTick();

        public void AfterTick();

        public void TickTricky(int mult);
    }
}