using Arch.Bus;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;

using Origin.Source.Events;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System.Collections.Generic;

using Vector2 = System.Numerics.Vector2;

namespace Origin.Source
{
    public partial class DebugMonitor : SimpleDrawableGameComponent
    {
        public double frames = 0;
        public double updates = 0;
        public double elapsed = 0;
        public double avgTickTime = 0;
        public double last = 0;
        public double last2 = 0;
        public double now = 0;
        public double msgFrequency = 10.0f;
        public string msg = "";

        private int fps = 0;
        private int min = 0;
        private int max = 0;
        private FixedSizedQueue<float> fpss = new FixedSizedQueue<float>(600);

        private long drawCalls;

        private bool isVisible = true;
        private int location = 0;

        private Dictionary<string, string> values = new();

        public bool IsVisible
        {
            get
            {
                return isVisible;
            }
        }

        private GraphicsDevice GraphicsDevice => Global.GraphicsDevice;
        private OriginGame game;

        public DebugMonitor(OriginGame game)
        {
            DrawOrder = 999;
            this.game = game;

            Hook();
        }

        /// <summary>
        /// The msgFrequency here is the reporting time to update the message.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            now = gameTime.TotalGameTime.TotalSeconds;
            elapsed = now - last;
            avgTickTime = (avgTickTime + now - last2) / 2;

            min = min < fps ? min : fps;
            max = max > fps ? max : fps;

            if (elapsed > msgFrequency)
            {
                elapsed = 0;
                frames = 0;
                updates = 0;
                last = now;
                min = max = fps;
            }
            last2 = now;
            updates++;
        }

        public override void Draw(GameTime gameTime)
        {
            fps = (int)(1f / gameTime.ElapsedGameTime.TotalSeconds);
            fpss.Enqueue(fps);
            frames++;
            drawCalls = GraphicsDevice.Metrics.DrawCount;

            ImGui.ShowDemoWindow();
            DrawOverlayWindow(gameTime);

            //OriginGame.GuiRenderer.BeginLayout(gameTime);

            //OriginGame.GuiRenderer.EndLayout();
        }

        public void DrawOverlayWindow(GameTime gameTime)
        {
            var io = ImGui.GetIO();
            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
            if (location >= 0)
            {
                float PAD = 10.0f;
                ImGuiViewportPtr viewport = ImGui.GetMainViewport();
                var work_pos = viewport.WorkPos; // Use work area to avoid menu-bar/task-bar, if any!
                var work_size = viewport.WorkSize;
                Vector2 window_pos, window_pos_pivot;
                window_pos.X = (location == 1) ? (work_pos.X + work_size.X - PAD) : (work_pos.X + PAD);
                window_pos.Y = (location == 2) ? (work_pos.Y + work_size.Y - PAD) : (work_pos.Y + PAD);
                window_pos_pivot.X = (location == 1) ? 1.0f : 0.0f;
                window_pos_pivot.Y = (location == 2) ? 1.0f : 0.0f;
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                window_flags |= ImGuiWindowFlags.NoMove;
            }
            else if (location == -2)
            {
                // Center window
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                window_flags |= ImGuiWindowFlags.NoMove;
            }
            ImGui.SetNextWindowBgAlpha(0.35f); // Transparent background
            if (ImGui.Begin("Example: Simple overlay", ref isVisible, window_flags))
            {
                if (ImGui.IsWindowHovered())
                    ImGui.SetNextFrameWantCaptureMouse(false);

                //ImGui.Text("(right-click to change position)");
                //ImGui.Separator();
                if (ImGui.IsMousePosValid())
                    ImGui.Text($"Mouse Position: ({io.MousePos.X},{io.MousePos.Y})");
                else
                    ImGui.Text("Mouse Position: <invalid>");
                ImGui.Text($"FPS : {fps} ({min}, {max}) - {avgTickTime.ToString("0.####")}");
                float[] samples = fpss.ToArray();
                ImGui.PlotLines("##FPS", ref samples[0], samples.Length, 0, null, 0, 100, default(Vector2), 4);
                ImGui.Text($"DrawCalls : {drawCalls}");
                ImGui.Text($"Mouse over UI: {io.WantCaptureMouse}");

                ImGui.Separator();

                foreach (var item in values)
                {
                    ImGui.Text($"{item.Key}: {item.Value}");
                }
                /*if (ImGui.BeginPopupContextWindow())
                {
                    if (ImGui.MenuItem("Custom", null, location == -1)) location = -1;
                    if (ImGui.MenuItem("Center", null, location == -2)) location = -2;
                    if (ImGui.MenuItem("Top-left", null, location == 0)) location = 0;
                    if (ImGui.MenuItem("Top-right", null, location == 1)) location = 1;
                    if (ImGui.MenuItem("Bottom-left", null, location == 2)) location = 2;
                    if (ImGui.MenuItem("Bottom-right", null, location == 3)) location = 3;
                    if (isVisible && ImGui.MenuItem("Close")) isVisible = false;
                    ImGui.EndPopup();
                }*/
            }

            ImGui.End();
        }

        [Event]
        public void OnDebugValueChanged(DebugValueChanged valueChanged)
        {
            if (true)
                foreach (var key in valueChanged.values.Keys)
                {
                    values[key] = valueChanged.values[key];
                }
        }
    }
}