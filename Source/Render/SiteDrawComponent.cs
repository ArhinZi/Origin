using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Origin.Source.Events;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Resources.Global;
using Origin.Source.Model.NewWorld;

namespace Origin.Source.Render
{
    public class SiteDrawComponent
    {
        private Site site;

        public bool HalfWallMode;

        public SiteRenderer SiteRenderer { get; private set; }

        public SiteDrawComponent(Site site)
        {
            this.site = site;
            SiteRenderer = new(site, Global.GraphicsDevice);
        }

        public void Update(GameTime gameTime)
        {
        }

        public void Draw(GameTime gameTime)
        {
            SiteRenderer.StaticDrawer.ClearLayer(DrawBufferLayer.BackInteractives);
            SiteRenderer.StaticDrawer.ClearLayer(DrawBufferLayer.FrontInteractives);
            if (site.Tools.CurrentTool != null)
            {
                foreach (var sprite in site.Tools.CurrentTool.sprites)
                {
                    byte LAYER = (byte)site.Tools.CurrentTool.RenderLayer;
                    SiteRenderer.StaticDrawer.AddTileSprite(new RenderData(
                        LAYER, sprite.position, sprite.sprite, sprite.color, new Vector3(sprite.offset.ToVector2(), 0)));
                }
                SiteRenderer.StaticDrawer.SetChunks();
                site.Tools.CurrentTool.DrawDirty = false;
            }

            Global.GraphicsDevice.Clear(Color.CornflowerBlue);
            SiteRenderer.Draw(gameTime);
        }
    }
}