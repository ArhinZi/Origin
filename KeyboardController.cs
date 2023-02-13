using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin
{
    class KeyboardController : IUpdateable
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

        public KeyboardController(bool enabled=true, int order=0)
        {
            Enabled = enabled;
            UpdateOrder = order;
        }



        public void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                MainGame.instance.Exit();

            int movemod = keyboardState.IsKeyDown(Keys.LeftShift) ? 50 : 1;

            if (keyboardState.IsKeyDown(Keys.Left))
                MainGame.cam.Move(new Vector2(-1* movemod, 0));
            if (keyboardState.IsKeyDown(Keys.Right))
                MainGame.cam.Move(new Vector2(1 * movemod, 0));
            if (keyboardState.IsKeyDown(Keys.Up))
                MainGame.cam.Move(new Vector2(0, -1 * movemod));
            if (keyboardState.IsKeyDown(Keys.Down))
                MainGame.cam.Move(new Vector2(0, 1 * movemod));

            /* if (keyboardState.IsKeyDown(Keys.OemOpenBrackets))
                 MainGame.cam.Rotation += 0.1f;
             if (keyboardState.IsKeyDown(Keys.OemCloseBrackets))
                 MainGame.cam.Rotation -= 0.1f;*/

            if (keyboardState.IsKeyDown(Keys.OemPlus))
                MainGame.cam.Zoom += 0.1f;
            if (keyboardState.IsKeyDown(Keys.OemMinus))
                    MainGame.cam.Zoom -= 0.1f;
        }

        public void Draw(GameTime gameTime) { }
    }
}
