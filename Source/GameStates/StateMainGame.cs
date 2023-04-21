using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended.Screens;

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
            MouseState currentMouseState = Mouse.GetState();
            Game.debug.Add("Mouse POS: " + currentMouseState.Position.ToString());
            //Game.debug.Add("Cam Projection: " + MainGame.Camera.Projection.ToString());
            //Game.debug.Add("Cam Transformation: " + MainGame.Camera.Transformation.ToString());

            Game.debug.Add("Curr LEVEL: " + World.ActiveSite.CurrentLevel.ToString());

            _inputControl.Update(gameTime);
            World.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            World.Draw(gameTime);
            long drawcalls = Game.GraphicsDevice.Metrics.DrawCount;
            Game.debug.Add("DrawCALLS: " + drawcalls.ToString());
        }
    }
}