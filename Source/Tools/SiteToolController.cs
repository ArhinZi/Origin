using Microsoft.Xna.Framework;

using MonoGame.Extended;

using System.Collections.Generic;
using System.Linq;

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
                new ToolDig(this),
                new ToolPathfind(this),
                new ToolPlaceDirt(this),
            };
            //SetToolByName("ToolDig");
        }

        public void SetToolByName(string name)
        {
            if (name == null)
            {
                CurrentTool.Reset();
                CurrentTool = null;
            }
            if (CurrentTool != null && CurrentTool.Name != name)
            {
                CurrentTool.Reset();
            }
            var tool = (from t in toolList where t.Name == name select t);
            if (tool.Any())
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