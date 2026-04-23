using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Origin.Source.Utils;

using static Origin.Source.Render.SpriteChunk;
using static Origin.Source.Resources.Global;

namespace Origin.Source.Render
{
    public class SiteRenderer
    {
        public static VertexBuffer GeometryBuffer;
        public static Effect InstanceMainEffect;

        private int _drawLowest;
        private int _drawHighest;
        private Site site;
        private GraphicsDevice _device = Global.GraphicsDevice;

        public StaticSpriteLayeredDrawer StaticDrawer { get; }
        public StaticHiddenLayeredDrawer HiddenDrawer { get; }

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            this.site = site;
            _drawHighest = site.CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);
            InstanceMainEffect = Global.Game.Content.Load<Effect>("FX/InstancedTileDraw");
            GenerateInstanceGeometry();

            StaticDrawer = new StaticSpriteLayeredDrawer(site);

            HiddenDrawer = new StaticHiddenLayeredDrawer(site);
        }

        private void GenerateInstanceGeometry()
        {
            int size = site.Size.X * site.Size.Y * 4;
            GeometryData[] _vertices = new GeometryData[6 * size];

            #region filling vertices

            for (int i = 0; i < size; i++)
            {
                _vertices[i * 6 + 0].World = new Color((byte)0, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 1].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 2].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 3].World = new Color((byte)255, (byte)0, (byte)0, (byte)0);
                _vertices[i * 6 + 4].World = new Color((byte)255, (byte)255, (byte)0, (byte)0);
                _vertices[i * 6 + 5].World = new Color((byte)0, (byte)255, (byte)0, (byte)0);
            }

            #endregion filling vertices

            GeometryBuffer = new VertexBuffer(_device, typeof(GeometryData), _vertices.Length, BufferUsage.WriteOnly);
            GeometryBuffer.SetData(_vertices);
        }

        public void Update(GameTime gameTime)
        {
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
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

                //RecalcHiddenInstances();
            }
        }

        public void Draw(GameTime gameTime)
        {
            CheckCurrentLevelChanged();

            Matrix WVP = Matrix.Multiply(Matrix.Multiply(site.Camera.WorldMatrix, site.Camera.Transformation),
                                site.Camera.Projection);
            InstanceMainEffect.Parameters["WorldViewProjection"].SetValue(WVP);
            InstanceMainEffect.Parameters["LowHighLevel"].SetValue(new Vector2(_drawLowest, _drawHighest));
            InstanceMainEffect.Parameters["WorldSize"].SetValue(new Vector2(site.Size.X, site.Size.Y));
            InstanceMainEffect.Parameters["WorldRotation"].SetValue((int)site.Rotation);
            InstanceMainEffect.Parameters["HiddenColor"].SetValue(GlobalResources.HIDDEN_COLOR.ToVector4());

            for (int z = _drawLowest; z < _drawHighest; z++)
            {
                InstanceMainEffect.Parameters["CurrentLevel"].SetValue(z);

                HiddenDrawer.DrawSides(z);
                StaticDrawer.Draw(z);
            }
            InstanceMainEffect.Parameters["CurrentLevel"].SetValue(_drawHighest);
            // top
            HiddenDrawer.DrawLayer(_drawHighest, site.DrawComponent.HalfWallMode);
            StaticDrawer.Draw(_drawHighest, [(byte)DrawBufferLayer.Back, (byte)DrawBufferLayer.BackNoLight, (byte)DrawBufferLayer.BackInteractives, (byte)DrawBufferLayer.FrontInteractives, (byte)DrawBufferLayer.Water], site.DrawComponent.HalfWallMode);
        }
    }
}