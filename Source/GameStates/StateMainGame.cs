using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended.Screens;

using Origin.Source.GameComponentsServices;

using System.Diagnostics;

namespace Origin.Source.GameStates
{
    public delegate void EventHandler();

    public class StateMainGame : GameScreen
    {
        public static int GameSpeed { get; private set; } = 1;
        public new OriginGame Game => (OriginGame)base.Game;

        public GameWorld World;
        public static Camera2D ActiveCamera { get; private set; }

        private InputControl _inputControl;

        public StateMainGame(Game game) : base(game)
        {
            World = new GameWorld();
            _inputControl = new InputControl();

            ActiveCamera = World.ActiveSite.Camera;
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            MouseState currentMouseState = Mouse.GetState();

            IGameInfoMonitor debug = Services.GetService<IGameInfoMonitor>();
            debug.Set("Mouse POS", currentMouseState.Position.ToString(), 5);
            debug.Set("Mouse POS", World.ActiveSite.CurrentLevel.ToString(), 6);

            _inputControl.Update(gameTime);
            World.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            World.Draw(gameTime);
        }
    }
}