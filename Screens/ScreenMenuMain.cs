using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using System;

namespace Origin.Screens
{
    internal class ScreenMenuMain : GameScreen
    {
        private new MainGame Game => (MainGame)base.Game;

        public ScreenMenuMain(MainGame game) : base(game)
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