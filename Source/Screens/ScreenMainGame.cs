using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended.Screens;

namespace Origin.Source.Screens
{
    internal class ScreenMainGame : GameScreen
    {
        private new MainGame Game => (MainGame)base.Game;

        private MainWorld _world;

        public ScreenMainGame(Game game) : base(game)
        {
            _world = new MainWorld();
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            MouseState currentMouseState = Mouse.GetState();
            Game.debug.Add("Mouse POS: " + currentMouseState.Position.ToString());

            Game.debug.Add("Cam ZOOM: " + MainGame.Camera.Zoom.ToString());
            Game.debug.Add("Cam POS: " + MainGame.Camera.Position.ToString());
            //Game.debug.Add("Cam Projection: " + MainGame.Camera.Projection.ToString());
            //Game.debug.Add("Cam Transformation: " + MainGame.Camera.Transformation.ToString());

            Game.debug.Add("Curr LEVEL: " + _world.ActiveSite.CurrentLevel.ToString());

            _world.Update(gameTime);
        }

        public override void Dispose()
        {
            // TODO: Check Disposing of World and sites inside properly
            _world.Dispose();
            base.Dispose();
        }

        public override void Draw(GameTime gameTime)
        {
            _world.Draw(gameTime);
            long drawcalls = Game.GraphicsDevice.Metrics.DrawCount;
            Game.debug.Add("DrawCALLS: " + drawcalls.ToString());
        }
    }
}