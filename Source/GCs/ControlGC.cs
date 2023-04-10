using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.IO;
using Origin.Source.Utils;

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

            if (InputManager.IsPressed("Camera.left"))
                MainGame.Camera.Move(new Vector2(-1 * movemod, 0));
            if (InputManager.IsPressed("Camera.right"))
                MainGame.Camera.Move(new Vector2(1 * movemod, 0));
            if (InputManager.IsPressed("Camera.up"))
                MainGame.Camera.Move(new Vector2(0, -1 * movemod));
            if (InputManager.IsPressed("Camera.down"))
                MainGame.Camera.Move(new Vector2(0, 1 * movemod));

            if (InputManager.JustPressedAndHoldDelayed("world.level.minus"))
                MainWorld.Instance.ActiveSite.CurrentLevel -= 1;
            if (InputManager.JustPressedAndHoldDelayed("world.level.plus"))
                MainWorld.Instance.ActiveSite.CurrentLevel += 1;

            if (InputManager.IsPressed("Camera.zoom.plus"))
                MainGame.Camera.Zoom += zoom_step * MainGame.Camera.Zoom;
            if (InputManager.IsPressed("Camera.zoom.minus"))
                MainGame.Camera.Zoom -= zoom_step * MainGame.Camera.Zoom;

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                if (MainWorld.Instance.ActiveSite.SelectedBlock != null)
                {
                    MainWorld.Instance.ActiveSite.SelectedBlock.RemoveWall();
                    MainWorld.Instance.ActiveSite.BlocksToReload.Add(MainWorld.Instance.ActiveSite.SelectedBlock.Position);
                }
            }
        }
    }
}