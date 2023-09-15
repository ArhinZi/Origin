using Microsoft.Xna.Framework;

using MonoGame.Extended.Screens;

namespace Origin.Source.GameStates
{
    internal class StateMenuMain : GameScreen
    {
        private new OriginGame Game => (OriginGame)base.Game;

        public StateMenuMain(Game game) : base(game)
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