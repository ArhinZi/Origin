﻿using Arch.Bus;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.GameStates;
using Origin.Source.IO;

namespace Origin.Source
{
    public class InputController : IUpdate
    {
        private bool LShift;
        private bool LCtrl;

        public void Update(GameTime gameTime)
        {
            if (InputManager.JustPressed("game.exit")) OriginGame.Instance.Exit();

            KeyboardState keystate = Keyboard.GetState();
            LShift = keystate.IsKeyDown(Keys.LeftShift);
            LCtrl = keystate.IsKeyDown(Keys.LeftControl);

            if (InputManager.JustPressed("game.halfwallswitch"))
            {
                EventBus.Send(new HalfWallModeChanged());
            }

            Camera2D activeCamera = StateMainGame.ActiveCamera;
            float camMoveMode = (float)((LShift ? Global.CAM_SHIFT_SPEED_MULT : 1) * Global.CAM_SPEED * gameTime.ElapsedGameTime.TotalSeconds);
            if (InputManager.IsPressed("Camera.left"))
                activeCamera.Position += new Vector2(-1, 0) * camMoveMode / activeCamera.Zoom;
            if (InputManager.IsPressed("Camera.right"))
                activeCamera.Position += new Vector2(1, 0) * camMoveMode / activeCamera.Zoom;
            if (InputManager.IsPressed("Camera.up"))
                activeCamera.Position += new Vector2(0, -1) * camMoveMode / activeCamera.Zoom;
            if (InputManager.IsPressed("Camera.down"))
                activeCamera.Position += new Vector2(0, 1) * camMoveMode / activeCamera.Zoom;

            if (InputManager.IsPressed("Camera.zoom.plus"))
                activeCamera.Zoom += Global.CAM_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!LCtrl && InputManager.MouseScrollNotchesY > 0)
                activeCamera.Zoom += Global.CAM_MOUSE_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (InputManager.IsPressed("Camera.zoom.minus"))
                activeCamera.Zoom -= Global.CAM_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!LCtrl && InputManager.MouseScrollNotchesY < 0)
                activeCamera.Zoom -= Global.CAM_MOUSE_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (InputManager.JustPressedAndHoldDelayed("world.level.minus") || LCtrl && InputManager.MouseScrollNotchesY < 0)
                MainWorld.Instance.ActiveSite.CurrentLevel -= 1;
            if (InputManager.JustPressedAndHoldDelayed("world.level.plus") || LCtrl && InputManager.MouseScrollNotchesY > 0)
                MainWorld.Instance.ActiveSite.CurrentLevel += 1;

            /*if (InputManager.JustPressed("mouse.right") &&
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
            }*/

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