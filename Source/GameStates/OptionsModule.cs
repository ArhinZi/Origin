using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Origin.Source.Controller.IO;
using Origin.Source.Save;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;

namespace Origin.Source.GameStates
{
    internal class OptionsModule
    {
        private readonly OriginGame _game;

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

        public OptionsModule(OriginGame game)
        {
            _game = game;

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

            supportedResolutions = _game.GetSupportedResolutions().ToList();
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
        }

        private static void CenterByWidth(float width)
        {
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - width) * 0.5f);
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

        public bool DrawWindow(int flags, Vector2 buttonSize, int topOffset, int hMargin)
        {
            bool back = false;
            if (ImGui.Begin("Options", (ImGuiWindowFlags)flags))
            {
                ImGui.SetCursorPosY(topOffset);

                float titleWidth = ImGui.CalcTextSize("Options").X;
                CenterByWidth(titleWidth);
                ImGui.Text("Options");
                ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);

                if (ImGui.BeginTabBar("OptionsTabs"))
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
                CenterByWidth(buttonSize.X);
                if (ImGui.Button("Apply", buttonSize))
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

                    _game.ApplyGraphicsSettings(res.Width, res.Height, fullscreenEnabled, vsyncEnabled, fpsLimit);
                    _game.ApplyAudioSettings(masterVolume, musicVolume, sfxVolume);
                }

                ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                CenterByWidth(buttonSize.X);
                if (ImGui.Button("Back", buttonSize))
                {
                    back = true;
                }
            }
            ImGui.End();
            return back;
        }
    }
}
