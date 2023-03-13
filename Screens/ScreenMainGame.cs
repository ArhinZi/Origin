using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using Origin.WorldComps;

namespace Origin.Screens
{
    internal class ScreenMainGame : GameScreen
    {
        private new MainGame Game => (MainGame)base.Game;

        private MainWorld _world;

        public ScreenMainGame(MainGame game) : base(game)
        {
            _world = new MainWorld();
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            _world.Update();
        }

        public override void Draw(GameTime gameTime)
        {
            _world.Draw();
        }
    }
}