using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using ImGuiNET;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Screens;

using Origin.Source.ECS;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Light;
using Origin.Source.ECS.Pathfinding;
using Origin.Source.ECS.Vegetation;
using Origin.Source.Events;
using Origin.Source.Resources;
using Origin.Source.Systems;
using Origin.Source.UI;
using Origin.Source.Utils;

using System.Collections.Generic;
using System.Text.Unicode;

using Vector2 = System.Numerics.Vector2;

namespace Origin.Source.GameStates
{
    public class StateMainGame : GameScreen
    {
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

        public MainWorld World;
        public static Camera2D ActiveCamera { get; private set; }

        private InputController _inputControl;

        public StateMainGame(Game game) : base(game)
        {
            World = new MainWorld();
            _inputControl = new InputController(this);

            World.Init();

            ActiveCamera = World.ActiveSite.Camera;
        }

        public override void LoadContent()
        {
            base.LoadContent();
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
                BaseConstruction bc;

                if (World.ActiveSite.Map[pos.X, pos.Y, pos.Z] != Entity.Null && World.ActiveSite.Map[pos.X, pos.Y, pos.Z].TryGet(out bc))
                {
                    blockMat = string.Format("{0} of {1}", bc.Construction.ID, bc.Material.ID);
                }

                EventBus.Send(new DebugValueChanged(6, new Dictionary<string, string>()
                {
                    ["DebugSelectedBlock"] = World.ActiveSite.Tools.CurrentTool.Position.ToString() + blockMat,
                    ["DebugLayer"] = World.ActiveSite.CurrentLevel.ToString(),
                    //["DayTime"] = World.ActiveSite.SiteTime.ToString("#.##")
                }));
            }
        }

        public override void Draw(GameTime gameTime)
        {
            World.Draw(gameTime);
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
                    if (ImGui.Begin("OptionsMenu", (ImGuiWindowFlags)flags))
                    {
                        ImGui.SetCursorPosY(200);

                        ImGuiUtil.AlignForWidth(ImGui.CalcTextSize("Options").X);
                        ImGui.Text("Options");
                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);

                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("1", bSize))
                        {
                            EscMenu = false;
                        }

                        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                        ImGuiUtil.AlignForWidth(bSize.X);
                        if (ImGui.Button("2", bSize))
                        {
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
                    LoadSaveGUI.Draw();

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
                            //World.Save();
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
                            OriginGame.Instance.Exit();
                        }
                    }
                }

                ImGui.PushFont(GlobalResources.Fonts["Default"]);
                ImGui.End();
            }
        }
    }
}