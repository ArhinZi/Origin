using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;

namespace Origin.Source
{
    public class MainWorld : IDisposable
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

        public void Update(GameTime gameTime)
        {
            foreach (var item in Sites)
            {
                item.Update(gameTime);
            }
        }

        public void Draw()
        {
            ActiveSite.Draw();
        }

        public void Dispose()
        {
            foreach (var item in Sites)
            {
                item.Dispose();
            }
        }
    }
}