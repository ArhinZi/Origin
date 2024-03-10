using ImGuiNET;

using Microsoft.Xna.Framework;

using MonoGame.Extended;

using Origin.Source.Resources;

using System.Collections.Generic;
using System.Linq;

namespace Origin.Source.Model.Site.Tools
{
    public class SiteToolsComponent : IUpdate
    {
        public Site Site;
        private List<Tool> toolList;
        public Tool CurrentTool;

        public SiteToolsComponent(Site site)
        {
            Site = site;
            toolList = new List<Tool>()
            {
                new ToolDig(this),
                new ToolPathfind(this),
                new ToolPlaceDirt(this),
                new ToolInfo(this),
            };
            //SetToolByName("ToolDig");
        }

        public void SetToolByName(string name)
        {
            if (name == null && CurrentTool != null)
            {
                CurrentTool.Reset();
                CurrentTool = null;
            }
            if (CurrentTool != null && CurrentTool.Name != name)
            {
                CurrentTool.Reset();
            }
            var tool = from t in toolList where t.Name == name select t;
            if (tool.Any())
                CurrentTool = tool.First();
        }

        public void Update(GameTime gameTime)
        {
            if (CurrentTool != null)
            {
                var io = ImGui.GetIO();
                if (io.WantCaptureMouse || !Global.Game.IsActive) return;

                CurrentTool.Update(gameTime);
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (CurrentTool != null)
            {
                CurrentTool.Draw(gameTime);
            }
        }
    }
}