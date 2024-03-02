using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Screens;

using Origin.Source.ECS;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Light;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.ECS.Vegetation;
using Origin.Source.Events;
using Origin.Source.Systems;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.GameStates
{
    public class StateMainGame : GameScreen
    {
        public static int GameSpeed { get; private set; } = 1;

        public MainWorld World;
        public static Camera2D ActiveCamera { get; private set; }

        private InputController _inputControl;

        public StateMainGame(Game game) : base(game)
        {
            World = new MainWorld();
            _inputControl = new InputController();

            World.Init();

            ActiveCamera = World.ActiveSite.Camera;
        }

        public override void LoadContent()
        {
            base.LoadContent();

        }

        public override void Update(GameTime gameTime)
        {
            _inputControl.Update(gameTime);
            World.Update(gameTime);

            if (World.ActiveSite.Tools.CurrentTool != null)
            {
                Point3 pos = World.ActiveSite.Tools.CurrentTool.Position;
                //string chunk = WorldUtils.GetChunkByCell(pos, new Point3(World.ActiveSite.DrawControl.StaticDrawer.ChunkSize, 1)).ToString();

                string blockMat = "NONE";
                BaseConstruction bc;

                if (World.ActiveSite.Map[pos.X, pos.Y, pos.Z] != Entity.Null && World.ActiveSite.Map[pos.X, pos.Y, pos.Z].TryGet(out bc))
                {
                    blockMat = string.Format("{0} of {1}", bc.Construction.ID, bc.Material.ID);
                }

                EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
                {
                    ["DebugSelectedBlock"] = World.ActiveSite.Tools.CurrentTool.Position.ToString() + blockMat,
                    ["DebugLayer"] = World.ActiveSite.CurrentLevel.ToString(),
                    //["DayTime"] = World.ActiveSite.SiteTime.ToString("#.##")
                }));
            }
        }

        public override void Draw(GameTime gameTime)
        {
            World.Draw(gameTime);
        }
    }
}