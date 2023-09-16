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

        public GlobalWorld World;

        public static InputControl InputControl;

        public StateMainGame(Game game) : base(game)
        {
            World = new GlobalWorld();
            InputControl = new InputControl();
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

            InputControl.Update(gameTime);
            World.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            World.Draw(gameTime);
        }
    }
}