using ImGuiNET;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using Origin.Source.Resources;
using Vector2 = System.Numerics.Vector2;

namespace Origin.Source.GameStates
{
    internal class StateMenuMain : GameScreen
    {
        private new OriginGame Game => (OriginGame)base.Game;
        private bool optionsMenu;
        private readonly OptionsModule optionsModule;

        public StateMenuMain(Game game) : base(game)
        {
            optionsModule = new OptionsModule(Game);
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            bool useWorkArea = true;
            int flags = (int)(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(useWorkArea ? viewport.WorkPos : viewport.Pos);
            ImGui.SetNextWindowSize(useWorkArea ? viewport.WorkSize : viewport.Size);

            var bSize = new Vector2(320, 64);
            const int hMargin = 12;

            ImGui.PushFont(GlobalResources.Fonts["BoldTitle"]);
            if (optionsMenu)
            {
                if (optionsModule.DrawWindow(flags, bSize, 120, hMargin))
                {
                    optionsMenu = false;
                }
            }
            else
            {
                if (ImGui.Begin("MainMenuScreen", (ImGuiWindowFlags)flags))
                {
                    ImGui.SetCursorPosY(220);
                    float titleWidth = ImGui.CalcTextSize("Main Menu").X;
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - titleWidth) * 0.5f);
                    ImGui.Text("Main Menu");
                    ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);

                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - bSize.X) * 0.5f);
                    if (ImGui.Button("New Site", bSize))
                    {
                        Game.StartNewSite();
                    }

                    ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - bSize.X) * 0.5f);
                    if (ImGui.Button("Options", bSize))
                    {
                        optionsMenu = true;
                    }

                    ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - bSize.X) * 0.5f);
                    if (ImGui.Button("Exit", bSize))
                    {
                        Global.Game.Exit();
                    }
                }
                ImGui.End();
            }

            ImGui.PushFont(GlobalResources.Fonts["Default"]);
        }

        public override void Update(GameTime gameTime)
        {
        }
    }
}