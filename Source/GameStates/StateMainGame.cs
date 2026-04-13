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
using Origin.Source.Model.Generators;
using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Origin.Source.Save;
using Origin.Source.Utils;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Vector2 = System.Numerics.Vector2;

namespace Origin.Source.GameStates
{
    public class StateMainGame : GameScreen
    {
        private static readonly float[] TimeScaleButtons = [0.5f, 1f, 2f, 4f];
        private bool _exitingToMainMenu;

        private bool _loadingNewSite;
        private Task<Model.NewWorld.World> _newSiteBuildTask;
        private bool _postInitPrepared;
        private float _loadingProgress;
        private string _loadingOperation = "Preparing";

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

        private Point3? _requestedSiteSize;
        private int? _requestedSiteSeed;
        private SiteGenerationSettings _requestedGenerationSettings;

        public StateMainGame(Game game, bool forceNewSite = false, Point3? requestedSiteSize = null, int? requestedSiteSeed = null, SiteGenerationSettings requestedGenerationSettings = null) : base(game)
        {
            optionsModule = new OptionsModule((OriginGame)game);
            _requestedSiteSize = requestedSiteSize;
            _requestedSiteSeed = requestedSiteSeed;
            _requestedGenerationSettings = requestedGenerationSettings;

            if (forceNewSite)
            {
                _loadingNewSite = true;
                _loadingProgress = 0.02f;
                _loadingOperation = "Preparing world";
                _newSiteBuildTask = Task.Run(() =>
                {
                    var world = new Model.NewWorld.World();
                    world.NewInitialize(false, (p, op) =>
                    {
                        _loadingProgress = p * 0.7f;
                        _loadingOperation = op;
                    }, _requestedSiteSize, _requestedSiteSeed, _requestedGenerationSettings);
                    return world;
                });
            }
            else if (SaveGameEntity.Saves.Count > 0)
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

        private void UpdateLoading()
        {
            if (_newSiteBuildTask == null)
                return;

            if (_newSiteBuildTask.IsFaulted)
            {
                _loadingOperation = "Loading failed";
                _loadingProgress = 0;
                return;
            }

            if (!_newSiteBuildTask.IsCompleted)
                return;

            if (World == null)
            {
                World = _newSiteBuildTask.Result;
                World.ActiveSite.InitializeRenderAndTools();
                Global.World = World;
                Global.ActiveCamera = World.ActiveSite.Camera;
                World.PreparePostInitialize();
                _postInitPrepared = true;
                _loadingProgress = 0.75f;
            }

            if (_postInitPrepared)
            {
                _loadingOperation = string.IsNullOrEmpty(World.PendingInitSystemName)
                    ? "Finalizing"
                    : $"Initializing {World.PendingInitSystemName}";

                bool done = World.InitializeNextSystem();
                int total = World.SystemsCount <= 0 ? 1 : World.SystemsCount;
                _loadingProgress = 0.75f + 0.25f * (World.InitializedSystemsCount / (float)total);

                if (done)
                {
                    _loadingProgress = 1f;
                    _loadingOperation = "Done";
                    _loadingNewSite = false;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_exitingToMainMenu)
                return;

            if (_loadingNewSite)
            {
                UpdateLoading();
                return;
            }

            if (World == null)
                return;

            _inputControl.Update(gameTime);
            if (!EscMenu)
                World.Update(gameTime);

            if (World.ActiveSite.Tools.CurrentTool != null)
            {
                Point3 pos = World.ActiveSite.Tools.CurrentTool.Position;
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
            if (_exitingToMainMenu)
                return;

            if (_loadingNewSite)
            {
                DrawLoadingOverlay();
                return;
            }

            if (World == null)
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

        private void DrawLoadingOverlay()
        {
            bool useWorkArea = true;
            int overlayFlags = (int)(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(useWorkArea ? viewport.WorkPos : viewport.Pos);
            ImGui.SetNextWindowSize(useWorkArea ? viewport.WorkSize : viewport.Size);

            if (ImGui.Begin("LoadingOverlay", (ImGuiWindowFlags)overlayFlags))
            {
                Vector2 panelSize = new(520, 140);
                Vector2 center = new((ImGui.GetWindowWidth() - panelSize.X) * 0.5f, (ImGui.GetWindowHeight() - panelSize.Y) * 0.5f);
                ImGui.SetCursorPos(center);

                ImGui.BeginChild("LoadingPanel", panelSize, ImGuiChildFlags.Border);
                ImGui.Text("Loading site...");
                ImGui.Spacing();
                ImGui.TextWrapped(_loadingOperation);
                ImGui.Spacing();
                ImGui.ProgressBar(System.Math.Clamp(_loadingProgress, 0f, 1f), new Vector2(-1, 20));
                ImGui.EndChild();
            }
            ImGui.End();
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