using Microsoft.Xna.Framework;

using MonoGame.Extended.Screens;

namespace Origin.Source.Screens
{
    internal class ScreenMenuMain : GameScreen
    {
        private new MainGame Game => (MainGame)base.Game;

        public ScreenMenuMain(Game game) : base(game)
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            //throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {
            //throw new NotImplementedException();
        }
    }
}