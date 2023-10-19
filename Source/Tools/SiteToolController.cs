using Microsoft.Xna.Framework;

using MonoGame.Extended;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Tools
{
    public class SiteToolController : IUpdate
    {
        public Site Site;
        private List<Tool> toolList;
        public Tool CurrentTool;

        public SiteToolController(Site site)
        {
            Site = site;
            toolList = new List<Tool>()
            {
                new ToolDig(this)
            };
            SetToolByName("ToolDig");
        }

        public void SetToolByName(string name)
        {
            if (CurrentTool != null && CurrentTool.Name != name)
            {
                CurrentTool.Reset();
            }
            var tool = (from t in toolList where t.Name == name select t);
            CurrentTool = tool.First();
        }

        public void Update(GameTime gameTime)
        {
            if (CurrentTool != null)
            {
                CurrentTool.Update(gameTime);
            }
        }
    }
}