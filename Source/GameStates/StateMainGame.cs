using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended.Screens;

using Origin.Source;
using Origin.Source.Controller.IO;
using Origin.Source.Controller.UI;
using Origin.Source.Events;
using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Origin.Source.Save;
using Origin.Source.Utils;

using System.Collections.Generic;
using System.Linq;

using Vector2 = System.Numerics.Vector2;

namespace Origin.Source.GameStates
{
    public class StateMainGame : GameScreen
    {
        private static readonly float[] TimeScaleButtons = [0.5f, 1f, 2f, 4f];

        public static int GameSpeed { get; private set; } = 1;

        private bool _escMenu = false;

        public bool EscMenu
        {
            get => _escMenu;
            set
            {
                if (!value)
                {
                    OptionsMenu = false;
                    LoadMenu = false;
                }
                _escMenu = value;
            }
        }

        public bool OptionsMenu = false;

        public bool LoadMenu = false;
        private int flags;

        private GlobalSettings globalSettings;
        private bool vsyncEnabled;
        private bool fullscreenEnabled;
        private List<(int Width, int Height)> supportedResolutions = [];
        private int selectedResolutionIndex = 0;
        private int fpsLimit;

        private float masterVolume;
        private float musicVolume;
        private float sfxVolume;

        private Keys cameraUpKey;
        private Keys cameraDownKey;
        private Keys cameraLeftKey;
        private Keys cameraRightKey;
        private Keys rotateLeftKey;
        private Keys rotateRightKey;

        private static readonly Keys[] ControlKeyOptions =
        [
            Keys.W, Keys.A, Keys.S, Keys.D,
            Keys.Q, Keys.E, Keys.R, Keys.F,
            Keys.Up, Keys.Down, Keys.Left, Keys.Right,
            Keys.Space, Keys.LeftShift, Keys.LeftControl, Keys.Escape
        ];

        public Model.NewWorld.World World;

        private InputController _inputControl;

        public StateMainGame(Game game) : base(game)
        {
            globalSettings = GlobalSettings.Load();
            vsyncEnabled = globalSettings.VSync;
            fullscreenEnabled = globalSettings.Fullscreen;
            fpsLimit = globalSettings.FpsLimit;

            masterVolume = globalSettings.MasterVolume;
            musicVolume = globalSettings.MusicVolume;
            sfxVolume = globalSettings.SfxVolume;

            cameraUpKey = globalSettings.CameraUpKey;
            cameraDownKey = globalSettings.CameraDownKey;
            cameraLeftKey = globalSettings.CameraLeftKey;
            cameraRightKey = globalSettings.CameraRightKey;
            rotateLeftKey = globalSettings.RotateLeftKey;
            rotateRightKey = globalSettings.RotateRightKey;

            if (Global.Game is OriginGame origin)
            {
                supportedResolutions = origin.GetSupportedResolutions().ToList();
            }
            if (supportedResolutions.Count == 0)
            {
                supportedResolutions.Add((globalSettings.ResolutionWidth, globalSettings.ResolutionHeight));
            }

            selectedResolutionIndex = supportedResolutions.FindIndex(r => r.Width == globalSettings.ResolutionWidth && r.Height == globalSettings.ResolutionHeight);
            if (selectedResolutionIndex < 0)
            {
                supportedResolutions.Add((globalSettings.ResolutionWidth, globalSettings.ResolutionHeight));
                selectedResolutionIndex = supportedResolutions.Count - 1;
            }

            ApplyControlsToInputManager();

            //World = new();

            //World.Initialize();

            if (SaveGameEntity.Saves.Count > 0)
            {
                LoadWorld(SaveGameEntity.Saves.First().Value);
                //Global.World = World;
                Global.ActiveCamera = World.ActiveSite.Camera;
            }
            else
            {
                World = new Model.NewWorld.World();
                World.NewInitialize();
                Global.World = World;
                Global.ActiveCamera = World.ActiveSite.Camera;

                World.PostInitialize();
            }

            _inputControl = new InputController(this);
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public void LoadWorld(SaveGameEntity sge)
        {
            World = sge.Load();
            Global.ActiveCamera = World.ActiveSite.Camera;
        }

        private void ApplyControlsToInputManager()
        {
            InputManager.RebindKeyboardKey("camera.up", cameraUpKey);
            InputManager.RebindKeyboardKey("camera.down", cameraDownKey);
            InputManager.RebindKeyboardKey("camera.left", cameraLeftKey);
            InputManager.RebindKeyboardKey("camera.right", cameraRightKey);
            InputManager.RebindKeyboardKey("world.rotate.left", rotateLeftKey);
            InputManager.RebindKeyboardKey("world.rotate.right", rotateRightKey);
        }

        private void DrawControlKeyCombo(string label, ref Keys selected)
        {
            if (ImGui.BeginCombo(label, selected.ToString()))
            {
                for (int i = 0; i < ControlKeyOptions.Length; i++)
                {
                    var key = ControlKeyOptions[i];
                    bool isSelected = key == selected;
                    if (ImGui.Selectable(key.ToString(), isSelected))
                    {
                        selected = key;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }
        }

        public override void Update(GameTime gameTime)
        {
            _inputControl.Update(gameTime);
            if (!EscMenu)
                World.Update(gameTime);

            if (World.ActiveSite.Tools.CurrentTool != null)
            {
                Point3 pos = World.ActiveSite.Tools.CurrentTool.Position;
                //string chunk = WorldUtils.GetChunkByCell(pos, new Point3(World.ActiveSite.DrawControl.StaticDrawer.ChunkSize, 1)).ToString();

                string blockMat = "NONE";

                if (World.ActiveSite.Map.TryGet(pos, out var tile) && tile.Exists && tile.HasConstruction)
                {
                    var bc = tile.Construction;
                    blockMat = string.Format("{0} of {1}", bc.Construction.ID, bc.Material.ID);
                }

                EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
                {
                    ["DebugSelectedBlock"] = World.ActiveSite.Tools.CurrentTool.Position.ToString() + blockMat,
                    ["DebugLayer"] = World.ActiveSite.CurrentLevel.ToString(),
                }));
            }

            EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
            {
                ["DayTime"] = World.TimeManager.DayTime.ToString(),
                ["SunIntensity"] = World.TimeManager.GetSunLightIntensity().ToString("#.##"),
            }));
        }

        public override void Draw(GameTime gameTime)
        {
            World.Draw(gameTime);

            if (!EscMenu)
            {
                DrawTimeScaleOverlay();
                DrawToolsPanel();
            }

            if (EscMenu)
            {
                bool use_work_area = true;
                flags = (int)(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);

                // We demonstrate using the full viewport area or the work area (without menu-bars, task-bars etc.)
                // Based on your use case you may want one or the other.
                ImGuiViewportPtr viewport = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(use_work_area ? viewport.WorkPos : viewport.Pos);
                ImGui.SetNextWindowSize(use_work_area ? viewport.WorkSize : viewport.Size);

                var bSize = new Vector2(300, 60);
                int hMargin = 10;

                ImGui.PushFont(GlobalResources.Fonts["BoldTitle"]);

                if (OptionsMenu)
                {
                    if (ImGui.Begin("Settings", (ImGuiWindowFlags)flags))
                    {
                        ImGui.SetCursorPosY(120);

                        ImGuiUtil.AlignForWidth(ImGui.CalcTextSize("Settings").X);
                        ImGui.Text("Settings");
                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);

                        if (ImGui.BeginTabBar("SettingsTabs"))
                        {
                            if (ImGui.BeginTabItem("Gameplay"))
                            {
                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem("Graphics"))
                            {
                                ImGui.Checkbox("Fullscreen", ref fullscreenEnabled);
                                ImGui.Checkbox("Vertical Sync", ref vsyncEnabled);

                                bool uncapped = fpsLimit <= 0;
                                if (ImGui.Checkbox("Uncapped FPS", ref uncapped))
                                {
                                    fpsLimit = uncapped ? 0 : 60;
                                }
                                if (!uncapped)
                                {
                                    ImGui.SliderInt("FPS Limit", ref fpsLimit, 30, 240);
                                }

                                var currentRes = supportedResolutions[selectedResolutionIndex];
                                string preview = $"{currentRes.Width}x{currentRes.Height}";
                                if (ImGui.BeginCombo("Resolution", preview))
                                {
                                    for (int i = 0; i < supportedResolutions.Count; i++)
                                    {
                                        bool isSelected = i == selectedResolutionIndex;
                                        var res = supportedResolutions[i];
                                        string label = $"{res.Width}x{res.Height}";
                                        if (ImGui.Selectable(label, isSelected))
                                            selectedResolutionIndex = i;

                                        if (isSelected)
                                            ImGui.SetItemDefaultFocus();
                                    }

                                    ImGui.EndCombo();
                                }

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem("Sound"))
                            {
                                ImGui.SliderFloat("Master Volume", ref masterVolume, 0.0f, 1.0f);
                                ImGui.SliderFloat("Music Volume", ref musicVolume, 0.0f, 1.0f);
                                ImGui.SliderFloat("SFX Volume", ref sfxVolume, 0.0f, 1.0f);
                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem("Controls"))
                            {
                                DrawControlKeyCombo("Camera Up", ref cameraUpKey);
                                DrawControlKeyCombo("Camera Down", ref cameraDownKey);
                                DrawControlKeyCombo("Camera Left", ref cameraLeftKey);
                                DrawControlKeyCombo("Camera Right", ref cameraRightKey);
                                DrawControlKeyCombo("Rotate Left", ref rotateLeftKey);
                                DrawControlKeyCombo("Rotate Right", ref rotateRightKey);
                                ImGui.EndTabItem();
                            }

                            ImGui.EndTabBar();
                        }

                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Apply", bSize))
                        {
                            var res = supportedResolutions[selectedResolutionIndex];
                            globalSettings.ResolutionWidth = res.Width;
                            globalSettings.ResolutionHeight = res.Height;
                            globalSettings.Fullscreen = fullscreenEnabled;
                            globalSettings.VSync = vsyncEnabled;
                            globalSettings.FpsLimit = fpsLimit;

                            globalSettings.MasterVolume = masterVolume;
                            globalSettings.MusicVolume = musicVolume;
                            globalSettings.SfxVolume = sfxVolume;

                            globalSettings.CameraUpKey = cameraUpKey;
                            globalSettings.CameraDownKey = cameraDownKey;
                            globalSettings.CameraLeftKey = cameraLeftKey;
                            globalSettings.CameraRightKey = cameraRightKey;
                            globalSettings.RotateLeftKey = rotateLeftKey;
                            globalSettings.RotateRightKey = rotateRightKey;

                            globalSettings.Save();

                            ApplyControlsToInputManager();

                            if (Global.Game is OriginGame origin && World != null)
                            {
                                origin.ApplyGraphicsSettings(res.Width, res.Height, fullscreenEnabled, vsyncEnabled, fpsLimit);
                                origin.ApplyAudioSettings(masterVolume, musicVolume, sfxVolume);
                            }
                        }

                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Back", bSize))
                        {
                            OptionsMenu = false;
                        }
                    }
                }
                else if (LoadMenu)
                {
                    LoadSaveGUI.Draw(this);

                    ImGui.PushFont(GlobalResources.Fonts["BoldTitle"]);
                    if (ImGui.Begin("Load", (ImGuiWindowFlags)flags))
                    {
                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Back", bSize))
                        {
                            LoadMenu = false;
                        }
                    }
                    ImGui.PushFont(GlobalResources.Fonts["Default"]);
                    ImGui.End();
                }
                else
                {
                    if (ImGui.Begin("EscMenu", (ImGuiWindowFlags)flags))
                    {
                        ImGui.SetCursorPosY(200);

                        ImGuiUtil.AlignForWidth(ImGui.CalcTextSize("Main menu").X);
                        ImGui.Text("Main menu");
                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);

                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Back to game", bSize))
                        {
                            EscMenu = false;
                        }

                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Save", bSize))
                        {
                            World.Save();
                        }

                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Load", bSize))
                        {
                            LoadMenu = true;
                        }

                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Options", bSize))
                        {
                            OptionsMenu = true;
                        }

                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("Exit", bSize))
                        {
                            Global.Game.Exit();
                        }
                    }
                }

                ImGui.PushFont(GlobalResources.Fonts["Default"]);
                ImGui.End();
            }
        }

        private void DrawTimeScaleOverlay()
        {
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            Vector2 size = new(340, 64);
            Vector2 pos = new(viewport.WorkPos.X + viewport.WorkSize.X - size.X - 12, viewport.WorkPos.Y + 12);

            ImGui.SetNextWindowPos(pos);
            ImGui.SetNextWindowSize(size);

            var overlayFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;
            if (ImGui.Begin("TimeScaleOverlay", overlayFlags))
            {
                float activeScale = World.TimeManager.ActiveTimeScale;
                bool paused = World.TimeManager.Pause;

                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f);

                if (paused)
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.75f, 0.2f, 0.2f, 1f));

                if (ImGui.Button("||", new Vector2(56, 44)))
                {
                    World.TimeManager.TogglePause();
                }

                if (paused)
                    ImGui.PopStyleColor();

                ImGui.SameLine();

                for (int i = 0; i < TimeScaleButtons.Length; i++)
                {
                    float scale = TimeScaleButtons[i];
                    bool selected = System.Math.Abs(activeScale - scale) < 0.001f;

                    if (selected)
                    {
                        if (paused)
                            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.18f, 0.35f, 0.75f, 1f));
                        else
                            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.2f, 0.55f, 0.25f, 1f));
                    }

                    if (ImGui.Button($"x{scale:0.#}", new Vector2(56, 44)))
                    {
                        World.TimeManager.SetTimeScale(scale);
                        if (World.TimeManager.Pause)
                            World.TimeManager.TogglePause();
                    }

                    if (selected)
                        ImGui.PopStyleColor();

                    if (i < TimeScaleButtons.Length - 1)
                        ImGui.SameLine();
                }

                ImGui.PopStyleVar();
            }
            ImGui.End();
        }

        private static string GetToolBindLabel(string toolName)
        {
            return toolName switch
            {
                "ToolDig" => "1",
                "ToolPathfind" => "2",
                "ToolPlaceDirt" => "3",
                "ToolPlaceWater" => "4",
                "ToolInfo" => "5",
                _ => ""
            };
        }

        private static string GetToolDisplayName(string toolName)
        {
            return toolName.StartsWith("Tool") ? toolName[4..] : toolName;
        }

        private void DrawToolsPanel()
        {
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            Vector2 size = new(190, 320);
            Vector2 pos = new(viewport.WorkPos.X + 12, viewport.WorkPos.Y + 470);

            ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

            var panelFlags = ImGuiWindowFlags.NoSavedSettings;
            if (ImGui.Begin("Tools", panelFlags))
            {
                var tools = World.ActiveSite.Tools.Tools;
                for (int i = 0; i < tools.Count; i++)
                {
                    var tool = tools[i];
                    bool selected = World.ActiveSite.Tools.CurrentTool != null && World.ActiveSite.Tools.CurrentTool.Name == tool.Name;
                    if (selected)
                        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.2f, 0.45f, 0.78f, 1f));

                    string bind = GetToolBindLabel(tool.Name);
                    string title = GetToolDisplayName(tool.Name);
                    if (ImGui.Button($"[{bind}] {title}", new Vector2(160, 0)))
                    {
                        World.ActiveSite.Tools.SetToolByName(tool.Name);
                    }

                    if (selected)
                        ImGui.PopStyleColor();
                }
            }
            ImGui.End();
        }

        public override void Dispose()
        {
            base.Dispose();
            World.Dispose();
        }
    }
}