using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.GameComponentsServices;
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

        public delegate void LevelChangeActivateHandler(short value);

        public delegate void ZoomChangeActivateHandler(short step);

        public delegate void CameraMoveActivateHandler(Vector2 value);

        public event LevelChangeActivateHandler LevelChangeActivated;

        public event ZoomChangeActivateHandler ZoomChangeActivated;

        public event CameraMoveActivateHandler CameraMoveActivated;

        public void Update(GameTime gameTime)
        {
            if (InputManager.JustPressed("game.exit")) OriginGame.Instance.Exit();

            keyboardState = Keyboard.GetState();
            int movemod = keyboardState.IsKeyDown(Keys.LeftShift) ? shift_mult : base_mult;

            if (InputManager.JustPressed("game.fpswitch"))
                OriginGame.Instance.Services.GetService<IGameInfoMonitor>().Switch();

            /* if (InputManager.IsPressed("Camera.left"))
                 CameraMoveActivated(new Vector2(-1 * movemod, 0));
             if (InputManager.IsPressed("Camera.right"))
                 CameraMoveActivated(new Vector2(1 * movemod, 0));
             if (InputManager.IsPressed("Camera.up"))
                 CameraMoveActivated(new Vector2(0, -1 * movemod));
             if (InputManager.IsPressed("Camera.down"))
                 CameraMoveActivated(new Vector2(0, 1 * movemod));

             if (InputManager.JustPressedAndHoldDelayed("world.level.minus"))
                 LevelChangeActivated(+1);
             if (InputManager.JustPressedAndHoldDelayed("world.level.plus"))
                 LevelChangeActivated(-1);

             if (InputManager.IsPressed("Camera.zoom.plus"))
                 ZoomChangeActivated(+1);
             if (InputManager.IsPressed("Camera.zoom.minus"))
                 ZoomChangeActivated(-1);*/

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                /*if (GlobalWorld.Instance.ActiveSite.SelectedBlock != new Point3(-1, -1, -1))
                {
                    SiteCell sc = GlobalWorld.Instance.ActiveSite.CellGetOrCreate(GlobalWorld.Instance.ActiveSite.SelectedBlock);
                    if (sc.RemoveWall())
                        GlobalWorld.Instance.ActiveSite.BlocksToReload.Add(sc.Position);
                }*/
            }
        }
    }
}