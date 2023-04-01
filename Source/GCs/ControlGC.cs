using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.IO;

namespace Origin.Source.GCs
{
    public class ControlGC : SimpleGameComponent
    {
        public int base_mult = 5;
        public int shift_mult = 30;
        public float zoom_step = 0.02f;
        private KeyboardState keyboardState;

        public override void Update(GameTime gameTime)
        {
            if (InputManager.JustPressed("game.exit")) MainGame.Instance.Exit();

            keyboardState = Keyboard.GetState();
            int movemod = keyboardState.IsKeyDown(Keys.LeftShift) ? shift_mult : base_mult;

            if (InputManager.JustPressed("game.fpswitch"))
                MainGame.Instance.debug.Visible = !MainGame.Instance.debug.Visible;

            if (InputManager.IsPressed("camera.left"))
                MainGame.cam.Move(new Vector2(-1 * movemod, 0));
            if (InputManager.IsPressed("camera.right"))
                MainGame.cam.Move(new Vector2(1 * movemod, 0));
            if (InputManager.IsPressed("camera.up"))
                MainGame.cam.Move(new Vector2(0, -1 * movemod));
            if (InputManager.IsPressed("camera.down"))
                MainGame.cam.Move(new Vector2(0, 1 * movemod));

            if (InputManager.JustPressed("world.level.minus"))
                MainWorld.Instance.ActiveSite.CurrentLevel -= 1;
            if (InputManager.JustPressed("world.level.plus"))
                MainWorld.Instance.ActiveSite.CurrentLevel += 1;

            if (InputManager.IsPressed("camera.zoom.plus"))
                MainGame.cam.Zoom += zoom_step * MainGame.cam.Zoom;
            if (InputManager.IsPressed("camera.zoom.minus"))
                MainGame.cam.Zoom -= zoom_step * MainGame.cam.Zoom;
        }
    }
}