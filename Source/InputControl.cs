using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.GameStates;
using Origin.Source.IO;
using Origin.Source.Utils;

using Point3 = Origin.Source.Utils.Point3;

namespace Origin.Source
{
    public class InputControl : IUpdate
    {
        public int base_mult = 5;
        public int shift_mult = 30;
        public float zoom_step = 0.02f;
        private KeyboardState keyboardState;

        public void Update(GameTime gameTime)
        {
            if (InputManager.JustPressed("game.exit")) OriginGame.Instance.Exit();

            keyboardState = Keyboard.GetState();
            int movemod = keyboardState.IsKeyDown(Keys.LeftShift) ? shift_mult : base_mult;

            if (InputManager.JustPressed("game.fpswitch"))
                OriginGame.Instance.debug.Visible = !OriginGame.Instance.debug.Visible;

            if (InputManager.IsPressed("Camera.left"))
                StateMainGame.ActiveCamera.Move(new Vector2(-1 * movemod, 0));
            if (InputManager.IsPressed("Camera.right"))
                StateMainGame.ActiveCamera.Move(new Vector2(1 * movemod, 0));
            if (InputManager.IsPressed("Camera.up"))
                StateMainGame.ActiveCamera.Move(new Vector2(0, -1 * movemod));
            if (InputManager.IsPressed("Camera.down"))
                StateMainGame.ActiveCamera.Move(new Vector2(0, 1 * movemod));

            if (InputManager.JustPressedAndHoldDelayed("world.level.minus"))
                MainWorld.Instance.ActiveSite.CurrentLevel -= 1;
            if (InputManager.JustPressedAndHoldDelayed("world.level.plus"))
                MainWorld.Instance.ActiveSite.CurrentLevel += 1;

            if (InputManager.IsPressed("Camera.zoom.plus"))
                StateMainGame.ActiveCamera.Zoom += zoom_step * StateMainGame.ActiveCamera.Zoom;
            if (InputManager.IsPressed("Camera.zoom.minus"))
                StateMainGame.ActiveCamera.Zoom -= zoom_step * StateMainGame.ActiveCamera.Zoom;

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                if (MainWorld.Instance.ActiveSite.SelectedBlock != Arch.Core.Entity.Null)
                {
                    if (MainWorld.Instance.ActiveSite.RemoveWall(MainWorld.Instance.ActiveSite.SelectedBlock))
                    {
                        var onSite = MainWorld.Instance.ActiveSite.SelectedBlock.Get<OnSitePosition>();
                        Point3 pos = onSite.position;
                        MainWorld.Instance.ActiveSite.BlocksToReload.Add(pos);
                    }
                }
            }
        }
    }
}