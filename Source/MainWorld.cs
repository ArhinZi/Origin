using System.Collections.Generic;

namespace Origin.Source
{
    public class MainWorld
    {
        public static MainWorld Instance { get; private set; }

        public List<Site> Sites { get; private set; }
        public Site ActiveSite { get; private set; }

        public MainWorld()
        {
            Instance = this;
            ActiveSite = new Site();
            Sites = new List<Site>
            {
                ActiveSite
            };
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