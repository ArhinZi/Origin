using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Origin.WorldComps;
using System;

namespace Origin.IO
{
    internal class KeyboardController : IUpdateable
    {
        public bool Enabled
        {
            get; set;
        } = true;

        public int UpdateOrder
        {
            get; set;
        } = 0;

        public event EventHandler<EventArgs> EnabledChanged;

        public event EventHandler<EventArgs> UpdateOrderChanged;

        private MainWorld _world;

        public KeyboardController(MainWorld w, bool enabled = true, int order = 0)
        {
            _world = w;
            Enabled = enabled;
            UpdateOrder = order;
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                MainGame.Instance.Exit();

            int movemod = keyboardState.IsKeyDown(Keys.LeftShift) ? 30 : 5;

            if (keyboardState.IsKeyDown(Keys.Left))
                MainGame.cam.Move(new Vector2(-1 * movemod, 0));
            if (keyboardState.IsKeyDown(Keys.Right))
                MainGame.cam.Move(new Vector2(1 * movemod, 0));
            if (keyboardState.IsKeyDown(Keys.Up))
                MainGame.cam.Move(new Vector2(0, -1 * movemod));
            if (keyboardState.IsKeyDown(Keys.Down))
                MainGame.cam.Move(new Vector2(0, 1 * movemod));

            if (keyboardState.IsKeyDown(Keys.OemOpenBrackets))
                _world.ActiveSite.CurrentLevel -= 1;
            if (keyboardState.IsKeyDown(Keys.OemCloseBrackets))
                _world.ActiveSite.CurrentLevel += 1;

            if (keyboardState.IsKeyDown(Keys.OemPlus))
                MainGame.cam.Zoom += 0.02f;
            if (keyboardState.IsKeyDown(Keys.OemMinus))
                MainGame.cam.Zoom -= 0.02f;
        }

        public void Draw(GameTime gameTime)
        { }
    }
}