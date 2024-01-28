using Arch.Core;
using Arch.Core.Extensions;

using info.lundin.math;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Sprites;

using Origin.Source.ECS;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Origin.Source.Render.GpuAcceleratedSpriteSystem.SpriteChunk;
using static System.Reflection.Metadata.BlobBuilder;

using Sprite = Origin.Source.Resources.Sprite;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    public class SiteRenderer
    {
        private int _drawLowest;
        private int _drawHighest;
        private Site site;

        public StaticSpriteLayeredDrawer StaticDrawer { get; }
        public StaticHiddenLayeredDrawer HiddenDrawer { get; }

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            this.site = site;
            _drawHighest = site.CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest - Global.ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

            StaticDrawer = new StaticSpriteLayeredDrawer(site);
            StaticDrawer.InitTerrainSprites();

            HiddenDrawer = new StaticHiddenLayeredDrawer(site);
            HiddenDrawer.InitTerrainHiddence();
        }

        private void CheckCurrentLevelChanged()
        {
            // Check if CurrentLevel changed and redraw what need to redraw
            if (_drawHighest != site.CurrentLevel)
            {
                /*if (HalfWallMode)
                {
                    RenderTasks.Enqueue(new RenderTask()
                    {
                        task = TaskLevelHalfWallUpdate(Site.PreviousLevel, Site.CurrentLevel),
                        OnComplete = new Action(() => { })
                    });
                }*/
                _drawHighest = site.CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - Global.ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

                //RecalcHiddenInstances();
            }
        }

        public void Update(GameTime gameTime)
        {
        }

        public void Draw(GameTime gameTime)
        {
            CheckCurrentLevelChanged();

            for (int z = _drawLowest; z < _drawHighest; z++)
                foreach (var key in texture2Ds)
                {
                    //HiddenDrawer.Draw(z);
                    StaticDrawer.Draw(z, new Vector2(_drawLowest, _drawHighest));
                }
            // top
            foreach (var key in texture2Ds)
            {
                HiddenDrawer.Draw(_drawHighest, new Vector2(_drawLowest, _drawHighest));
                StaticDrawer.Draw(_drawHighest, new Vector2(_drawLowest, _drawHighest), new List<byte>() { 0 });
            }
        }
    }
}