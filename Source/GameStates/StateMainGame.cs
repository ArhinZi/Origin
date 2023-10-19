using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Screens;

using Origin.Source.ECS;
using Origin.Source.Events;
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

            Point3 pos = World.ActiveSite.Tools.CurrentTool.Position;
            string chank = WorldUtils.GetChunkByCell(pos, new Point3(World.Renderer.ChunkSize, 1)).ToString();

            string blockMat = "NONE";
            TileStructure ts;

            if (World.ActiveSite.Blocks[pos.X, pos.Y, pos.Z] != Entity.Null && World.ActiveSite.Blocks[pos.X, pos.Y, pos.Z].TryGet(out ts))
            {
                blockMat = ts.FloorMaterial.ID;
            }

            EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["DebugSelectedBlock"] = World.ActiveSite.Tools.CurrentTool.Position.ToString() + chank + blockMat,
                ["DebugLayer"] = World.ActiveSite.CurrentLevel.ToString(),
                ["DayTime"] = World.ActiveSite.SiteTime.ToString("#.##")
            }));
        }

        public override void Draw(GameTime gameTime)
        {
            World.Draw(gameTime);
        }
    }
}