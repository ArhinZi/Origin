using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using ImGuiNET;

using Microsoft.Xna.Framework;

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
        private bool _exitingToMainMenu;

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

        private readonly OptionsModule optionsModule;

        public Model.NewWorld.World World;

        private InputController _inputControl;

        public StateMainGame(Game game, bool forceNewSite = false) : base(game)
        {
            optionsModule = new OptionsModule((OriginGame)game);

            if (!forceNewSite && SaveGameEntity.Saves.Count > 0)
            {
                LoadWorld(SaveGameEntity.Saves.First().Value);
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

        private void ExitToMainMenu()
        {
            if (_exitingToMainMenu)
                return;

            _exitingToMainMenu = true;

            if (World != null)
            {
                World.Dispose();
                World = null;
            }

            Global.World = null;
            Global.ActiveCamera = null;

            if (Game is OriginGame origin)
            {
                origin.LoadMenuMainScreen();
            }
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

        public override void Update(GameTime gameTime)
        {
            if (_exitingToMainMenu || World == null)
                return;

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
            if (_exitingToMainMenu || World == null)
                return;

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
                    if (optionsModule.DrawWindow(flags, bSize, 120, hMargin))
                    {
                        OptionsMenu = false;
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
                        if (ImGui.Button("Exit to main menu", bSize))
                        {
                            ExitToMainMenu();
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
                if (!OptionsMenu && !LoadMenu)
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
            World?.Dispose();
            World = null;
        }
    }
}