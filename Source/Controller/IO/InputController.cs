using Arch.Bus;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.Events;
using Origin.Source.GameStates;
using Origin.Source.Model;
using Origin.Source.Model.Site;
using Origin.Source.Resources;

namespace Origin.Source.Controller.IO
{
    public class InputController : IUpdate
    {
        private bool LShift;
        private bool LCtrl;

        private StateMainGame StateMainGame;
        private World World;
        private Site ActiveSite => World.ActiveSite;

        public InputController(StateMainGame smg)
        {
            StateMainGame = smg;
            World = smg.World;
        }

        public void Update(GameTime gameTime)
        {
            //if (InputManager.JustPressed("game.exit")) OriginGame.Instance.Exit();

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                StateMainGame.EscMenu = !StateMainGame.EscMenu;
            }

            KeyboardState keystate = Keyboard.GetState();
            LShift = keystate.IsKeyDown(Keys.LeftShift);
            LCtrl = keystate.IsKeyDown(Keys.LeftControl);

            var io = ImGui.GetIO();
            if (io.WantCaptureMouse) return;

            if (InputManager.JustPressed("game.halfwallswitch"))
            {
                //EventBus.Send(new HalfWallModeChanged());
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
                activeCamera.ZoomIn();
            //activeCamera.Zoom += Global.CAM_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!LCtrl && InputManager.MouseScrollNotchesY > 0)
                activeCamera.ZoomIn();
            //activeCamera.Zoom += Global.CAM_MOUSE_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (InputManager.IsPressed("Camera.zoom.minus"))
                activeCamera.ZoomOut();
            //activeCamera.Zoom -= Global.CAM_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!LCtrl && InputManager.MouseScrollNotchesY < 0)
                activeCamera.ZoomOut();
            //activeCamera.Zoom -= Global.CAM_MOUSE_ZOOM_SPEED * activeCamera.Zoom * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (InputManager.JustPressedAndHoldDelayed("world.level.minus") || LCtrl && InputManager.MouseScrollNotchesY < 0)
                ActiveSite.CurrentLevel -= 1;
            if (InputManager.JustPressedAndHoldDelayed("world.level.plus") || LCtrl && InputManager.MouseScrollNotchesY > 0)
                ActiveSite.CurrentLevel += 1;

            if (InputManager.JustPressed("num.1"))
                ActiveSite.Tools.SetToolByName("ToolDig");
            if (InputManager.JustPressed("num.2"))
                ActiveSite.Tools.SetToolByName("ToolPlaceDirt");
            if (InputManager.JustPressed("num.3"))
                ActiveSite.Tools.SetToolByName("ToolPathfind");
            if (InputManager.JustPressed("num.4"))
                ActiveSite.Tools.SetToolByName("ToolPlaceDirt");
            if (InputManager.JustPressed("num.5"))
                ActiveSite.Tools.SetToolByName(null);
            if (InputManager.JustPressed("num.6"))
                ActiveSite.Tools.SetToolByName(null);
            if (InputManager.JustPressed("num.7"))
                ActiveSite.Tools.SetToolByName(null);
            if (InputManager.JustPressed("num.8"))
                ActiveSite.Tools.SetToolByName(null);
            if (InputManager.JustPressed("num.9"))
                ActiveSite.Tools.SetToolByName(null);
            if (InputManager.JustPressed("num.0"))
                ActiveSite.Tools.SetToolByName("ToolInfo");

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