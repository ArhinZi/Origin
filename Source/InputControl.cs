using Arch.Bus;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.GameStates;
using Origin.Source.IO;
using Origin.Source.Utils;

using Point3 = Origin.Source.Utils.Point3;

namespace Origin.Source
{
    public class InputControl : IUpdate
    {
        public int base_mult = 1000;
        public int shift_mult = 5000;
        public float zoom_step = 1f;
        private KeyboardState keyboardState;

        public void Update(GameTime gameTime)
        {
            if (InputManager.JustPressed("game.exit")) OriginGame.Instance.Exit();

            keyboardState = Keyboard.GetState();
            float movemod = (keyboardState.IsKeyDown(Keys.LeftShift) ? shift_mult : base_mult) * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (InputManager.JustPressed("game.fpswitch"))
            {
                EventBus.Send(new HalfWallModeChanged());
            }

            if (InputManager.IsPressed("Camera.left"))
                StateMainGame.ActiveCamera.Move(new Vector2(-1 * movemod, 0));
            if (InputManager.IsPressed("Camera.right"))
                StateMainGame.ActiveCamera.Move(new Vector2(1 * movemod, 0));
            if (InputManager.IsPressed("Camera.up"))
                StateMainGame.ActiveCamera.Move(new Vector2(0, -1 * movemod));
            if (InputManager.IsPressed("Camera.down"))
                StateMainGame.ActiveCamera.Move(new Vector2(0, 1 * movemod));

            if (InputManager.JustPressedAndHoldDelayed("world.level.minus") || InputManager.IsPressed("ctrl") && InputManager.MouseScrollNotchesY < 0)
                MainWorld.Instance.ActiveSite.CurrentLevel -= 1;
            if (InputManager.JustPressedAndHoldDelayed("world.level.plus") || InputManager.IsPressed("ctrl") && InputManager.MouseScrollNotchesY > 0)
                MainWorld.Instance.ActiveSite.CurrentLevel += 1;

            if (InputManager.IsPressed("Camera.zoom.plus"))
                StateMainGame.ActiveCamera.Zoom += zoom_step * StateMainGame.ActiveCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!InputManager.IsPressed("ctrl") && InputManager.MouseScrollNotchesY > 0)
                StateMainGame.ActiveCamera.Zoom += 10f * StateMainGame.ActiveCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (InputManager.IsPressed("Camera.zoom.minus"))
                StateMainGame.ActiveCamera.Zoom -= zoom_step * StateMainGame.ActiveCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!InputManager.IsPressed("ctrl") && InputManager.MouseScrollNotchesY < 0)
                StateMainGame.ActiveCamera.Zoom -= 10f * StateMainGame.ActiveCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (InputManager.JustPressed("mouse.right") &&
                    MainWorld.Instance.ActiveSite.SelectedBlock != Arch.Core.Entity.Null)
            {
                if (MainWorld.Instance.ActiveSite.SelectedBlock.Has<TileHasPathNode>())
                {
                    MainWorld.Instance.ActiveSite.startPathNode = MainWorld.Instance.ActiveSite.SelectedBlock.Get<TileHasPathNode>().node;
                }
            }
            if (InputManager.JustPressed("mouse.left") &&
                MainWorld.Instance.ActiveSite.SelectedBlock != Arch.Core.Entity.Null)
            {
                if (MainWorld.Instance.ActiveSite.SelectedBlock.Has<TileHasPathNode>())
                {
                    MainWorld.Instance.ActiveSite.endPathNode = MainWorld.Instance.ActiveSite.SelectedBlock.Get<TileHasPathNode>().node;
                    if (MainWorld.Instance.ActiveSite.startPathNode != null)
                        MainWorld.Instance.ActiveSite.FindPath();
                }
            }

            /*if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                if (MainWorld.Instance.ActiveSite.SelectedBlock != Arch.Core.Entity.Null)
                {
                    *//*if (MainWorld.Instance.ActiveSite.RemoveWall(MainWorld.Instance.ActiveSite.SelectedBlock))
                    {
                        var onSite = MainWorld.Instance.ActiveSite.SelectedBlock.Get<OnSitePosition>();
                        Point3 pos = onSite.position;
                        MainWorld.Instance.ActiveSite.BlocksToReload.Add(pos);
                    }*//*
                }
            }*/
        }
    }
}