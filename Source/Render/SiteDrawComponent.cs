using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Origin.Source.ECS.Construction;
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
using Origin.Source.Model.Site;
using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.ECS.Fluid;
using Origin.Source.ECS.Render;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    public partial class SiteDrawComponent
    {
        private Sprite lborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "RightBorder");
        private Color borderColor = new(0, 0, 0, 255);

        private Site site;
        private Random random;
        private SpriteBatch spriteBatch = new(Global.GraphicsDevice);

        public RenderTarget2D RenderTarget2D { get; private set; }
        public SiteRenderer SiteRenderer { get; private set; }

        public SiteDrawComponent(Site site)
        {
            this.site = site;
            SiteRenderer = new(site, Global.GraphicsDevice);

            RenderTarget2D = new RenderTarget2D(Global.GraphicsDevice,
                Global.Game.Window.ClientBounds.Width, Global.Game.Window.ClientBounds.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 8, RenderTargetUsage.PreserveContents);

            random = site.World.Random;

            InitTerrainHiddence();
            Hook();
        }

        [Event]
        public void OnScreenBoundsChanged(ScreenBoundsChanged bounds)
        {
            RenderTarget2D = new RenderTarget2D(Global.GraphicsDevice,
                bounds.screenBounds.Width, bounds.screenBounds.Height,
                false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 8, RenderTargetUsage.PreserveContents);
        }

        public void InitTerrainHiddence()
        {
            for (int z = 0; z < site.Size.Z; z++)
                for (int x = 0; x < site.Size.X; x++)
                    for (int y = 0; y < site.Size.Y; y++)
                    {
                        Point3 tilePos = new(x, y, z);
                        Entity tile = site.Map[tilePos];

                        if (tile == Entity.Null)
                        {
                            SiteRenderer.HiddenDrawer.MakeHidden(tilePos);
                        }
                    }
            SiteRenderer.HiddenDrawer.Set();
        }

        public void Update(GameTime gameTime)
        {
        }

        public void Draw(GameTime gameTime)
        {
            if (site.Tools.CurrentTool != null && site.Tools.CurrentTool.DrawDirty)
            {
                site.Tools.CurrentTool.DrawDirty = false;
                SiteRenderer.StaticDrawer.ClearLayer(DrawBufferLayer.BackInteractives);
                SiteRenderer.StaticDrawer.ClearLayer(DrawBufferLayer.FrontInteractives);
                foreach (var sprite in site.Tools.CurrentTool.sprites)
                {
                    byte LAYER = (byte)site.Tools.CurrentTool.RenderLayer;
                    SiteRenderer.StaticDrawer.AddTileSprite(new RenderData(
                        LAYER, sprite.position, sprite.sprite, sprite.color, new Vector3(sprite.offset.ToVector2(), 0)));
                }
                SiteRenderer.StaticDrawer.SetChunks();
            }

            Global.GraphicsDevice.SetRenderTarget(RenderTarget2D);
            Global.GraphicsDevice.Clear(Color.CornflowerBlue);
            SiteRenderer.Draw(gameTime);
            Global.GraphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin();
            spriteBatch.Draw(RenderTarget2D, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}