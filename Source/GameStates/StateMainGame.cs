using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended.Screens;
using Origin.Source.Events;
using System.Collections.Generic;

using System;

namespace Origin.Source.GameStates
{
    public class StateMainGame : GameScreen
    {
        public static int GameSpeed { get; private set; } = 1;
        public new OriginGame Game => (OriginGame)base.Game;

        public MainWorld World;
        public static Camera2D ActiveCamera { get; private set; }

        private InputControl _inputControl;

        public StateMainGame(Game game) : base(game)
        {
            World = new MainWorld();
            _inputControl = new InputControl();

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

            EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["DebugSelectedBlock"] = World.ActiveSite.SelectedPosition.ToString(),
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