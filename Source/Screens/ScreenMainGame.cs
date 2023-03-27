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
            Game.debug.Add("Cam ZOOM: " + MainGame.cam.Zoom.ToString());
            Game.debug.Add("Cam POS: " + MainGame.cam.Pos.ToString());

            Game.debug.Add("Curr LEVEL: " + _world.ActiveSite.CurrentLevel.ToString());

            _world.Update();
        }

        public override void Draw(GameTime gameTime)
        {
            _world.Draw();
        }
    }
}