using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using Origin.IO;
using Microsoft.Xna.Framework.Input;
using Origin.WorldComps;

namespace Origin.ECS
{
    public class ControlComp : SimpleGameComponent
    {
        public int base_mult = 5;
        public int shift_mult = 30;
        public float zoom_step = 0.02f;
        private KeyboardState keyboardState;

        public override void Update(GameTime gameTime)
        {
            if (InputManager.PressedStart("game.exit")) MainGame.Instance.Exit();

            keyboardState = Keyboard.GetState();
            int movemod = keyboardState.IsKeyDown(Keys.LeftShift) ? shift_mult : base_mult;

            if (InputManager.IsPressed("camera.left"))
                MainGame.cam.Move(new Vector2(-1 * movemod, 0));
            if (InputManager.IsPressed("camera.right"))
                MainGame.cam.Move(new Vector2(1 * movemod, 0));
            if (InputManager.IsPressed("camera.up"))
                MainGame.cam.Move(new Vector2(0, -1 * movemod));
            if (InputManager.IsPressed("camera.down"))
                MainGame.cam.Move(new Vector2(0, 1 * movemod));

            if (keyboardState.IsKeyDown(Keys.OemOpenBrackets))
                MainWorld.Instance.ActiveSite.CurrentLevel -= 1;
            if (keyboardState.IsKeyDown(Keys.OemCloseBrackets))
                MainWorld.Instance.ActiveSite.CurrentLevel += 1;

            if (keyboardState.IsKeyDown(Keys.OemPlus))
                MainGame.cam.Zoom += zoom_step;
            if (keyboardState.IsKeyDown(Keys.OemMinus))
                MainGame.cam.Zoom -= zoom_step;
        }
    }
}