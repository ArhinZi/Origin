using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.World
{
    class MainWorld
    {
        public static MainWorld Instance { get; private set; }

        public List<Site> Sites { get; private set; }
        public Site ActiveSite { get; private set; }


        public MainWorld()
        {
            ActiveSite = new Site();
            Sites = new List<Site>();
            Sites.Add(ActiveSite);
        }

        public void Update()
        {
            foreach (var item in Sites)
            {
                item.Update();
            }
        }

        public void Draw()
        {
            ActiveSite.Draw();
        }

    }
}
