using ImGuiNET;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using Origin.Source.Controller.IO;
using Origin.Source.Model.Generators;
using Origin.Source.Resources;
using Origin.Source.Save;
using Vector2 = System.Numerics.Vector2;

namespace Origin.Source.GameStates
{
    internal class StateMenuMain : GameScreen
    {
        private new OriginGame Game => (OriginGame)base.Game;
        private bool optionsMenu;
        private bool newSiteMenu;
        private readonly OptionsModule optionsModule;
        private readonly GlobalSettings globalSettings;

        private int newSiteSizeX;
        private int newSiteSizeY;
        private int newSiteSizeZ;
        private int newSiteSeed;

        private float genHeightScale;
        private float genNoiseFrequency;
        private int genNoiseOctaves;
        private float genNoiseGain;
        private float genBaseHeightRatio;
        private int genSoilDepth;
        private int genSmoothIterations;

        private bool genEnableRiver;
        private int genRiverStrength;
        private float genRiverRadiusMultiplier;
        private float genRiverErosionPower;
        private float genRiverMinHeight;

        public StateMenuMain(Game game) : base(game)
        {
            optionsModule = new OptionsModule(Game);
            globalSettings = GlobalSettings.Load();

            newSiteSizeX = globalSettings.NewSiteSizeX;
            newSiteSizeY = globalSettings.NewSiteSizeY;
            newSiteSizeZ = globalSettings.NewSiteSizeZ;
            newSiteSeed = globalSettings.NewSiteSeed;

            genHeightScale = globalSettings.GenHeightScale;
            genNoiseFrequency = globalSettings.GenNoiseFrequency;
            genNoiseOctaves = globalSettings.GenNoiseOctaves;
            genNoiseGain = globalSettings.GenNoiseGain;
            genBaseHeightRatio = globalSettings.GenBaseHeightRatio;
            genSoilDepth = globalSettings.GenSoilDepth;
            genSmoothIterations = globalSettings.GenSmoothIterations;

            genEnableRiver = globalSettings.GenEnableRiver;
            genRiverStrength = globalSettings.GenRiverStrength;
            genRiverRadiusMultiplier = globalSettings.GenRiverRadiusMultiplier;
            genRiverErosionPower = globalSettings.GenRiverErosionPower;
            genRiverMinHeight = globalSettings.GenRiverMinHeight;
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        private void DrawNewSiteWindow(int flags, Vector2 bSize, int hMargin)
        {
            if (ImGui.Begin("New Site", (ImGuiWindowFlags)flags))
            {
                Vector2 panelSize = new(760, 560);
                Vector2 panelPos = new((ImGui.GetWindowWidth() - panelSize.X) * 0.5f, (ImGui.GetWindowHeight() - panelSize.Y) * 0.5f);
                ImGui.SetCursorPos(panelPos);

                if (ImGui.BeginChild("NewSiteSettingsPanel", panelSize, ImGuiChildFlags.Border))
                {
                    ImGui.PushFont(GlobalResources.Fonts["Default"]);

                    float titleWidth = ImGui.CalcTextSize("New Site").X;
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - titleWidth) * 0.5f);
                    ImGui.Text("New Site");
                    ImGui.Spacing();

                    void RowInt(string label, string id, ref int value)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextUnformatted(label);
                        ImGui.TableSetColumnIndex(1);
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputInt(id, ref value);
                    }

                    void RowFloat(string label, string id, ref float value)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextUnformatted(label);
                        ImGui.TableSetColumnIndex(1);
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat(id, ref value, 0.1f, 1f, "%.3f");
                    }

                    void RowIntSlider(string label, string id, ref int value, int min, int max)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextUnformatted(label);
                        ImGui.TableSetColumnIndex(1);
                        ImGui.SetNextItemWidth(-1);
                        ImGui.SliderInt(id, ref value, min, max);
                    }

                    if (ImGui.BeginTable("NewSiteTable", 2, ImGuiTableFlags.SizingStretchProp))
                    {
                        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 260);
                        ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.SeparatorText("Map Size");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Separator();
                        RowInt("Size X", "##NewSiteSizeX", ref newSiteSizeX);
                        RowInt("Size Y", "##NewSiteSizeY", ref newSiteSizeY);
                        RowInt("Size Z", "##NewSiteSizeZ", ref newSiteSizeZ);
                        RowInt("Generation Seed", "##NewSiteSeed", ref newSiteSeed);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.SeparatorText("Terrain");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Separator();
                        RowFloat("Height Scale", "##GenHeightScale", ref genHeightScale);
                        RowFloat("Base Height Ratio", "##GenBaseHeightRatio", ref genBaseHeightRatio);
                        RowInt("Soil Depth", "##GenSoilDepth", ref genSoilDepth);
                        RowIntSlider("Smooth Iterations", "##GenSmoothIterations", ref genSmoothIterations, 0, 10);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.SeparatorText("Noise");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Separator();
                        RowFloat("Noise Frequency", "##GenNoiseFrequency", ref genNoiseFrequency);
                        RowInt("Noise Octaves", "##GenNoiseOctaves", ref genNoiseOctaves);
                        RowFloat("Noise Gain", "##GenNoiseGain", ref genNoiseGain);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.SeparatorText("River");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Separator();

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextUnformatted("Enable River");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.Checkbox("##GenEnableRiver", ref genEnableRiver);

                        if (genEnableRiver)
                        {
                            RowInt("River Strength", "##GenRiverStrength", ref genRiverStrength);
                            RowFloat("River Radius Multiplier", "##GenRiverRadiusMultiplier", ref genRiverRadiusMultiplier);
                            RowFloat("River Erosion Power", "##GenRiverErosionPower", ref genRiverErosionPower);
                            RowFloat("River Min Height", "##GenRiverMinHeight", ref genRiverMinHeight);
                        }

                        ImGui.EndTable();
                    }

                    ImGui.Spacing();
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - bSize.X) * 0.5f);
                    if (ImGui.Button("Create", bSize))
                    {
                        newSiteSizeX = System.Math.Clamp(newSiteSizeX, 16, 1024);
                        newSiteSizeY = System.Math.Clamp(newSiteSizeY, 16, 1024);
                        newSiteSizeZ = System.Math.Clamp(newSiteSizeZ, 16, 512);

                        genHeightScale = System.Math.Clamp(genHeightScale, 1f, 40f);
                        genNoiseFrequency = System.Math.Clamp(genNoiseFrequency, 0.0001f, 0.05f);
                        genNoiseOctaves = System.Math.Clamp(genNoiseOctaves, 1, 16);
                        genNoiseGain = System.Math.Clamp(genNoiseGain, 0.01f, 1f);
                        genBaseHeightRatio = System.Math.Clamp(genBaseHeightRatio, 0.05f, 0.95f);
                        genSoilDepth = System.Math.Clamp(genSoilDepth, 1, 64);
                        genSmoothIterations = System.Math.Clamp(genSmoothIterations, 0, 16);
                        genRiverStrength = System.Math.Clamp(genRiverStrength, 1, 64);
                        genRiverRadiusMultiplier = System.Math.Clamp(genRiverRadiusMultiplier, 0.5f, 20f);
                        genRiverErosionPower = System.Math.Clamp(genRiverErosionPower, 0.1f, 3f);
                        genRiverMinHeight = System.Math.Clamp(genRiverMinHeight, -64f, 10f);

                        globalSettings.NewSiteSizeX = newSiteSizeX;
                        globalSettings.NewSiteSizeY = newSiteSizeY;
                        globalSettings.NewSiteSizeZ = newSiteSizeZ;
                        globalSettings.NewSiteSeed = newSiteSeed;

                        globalSettings.GenHeightScale = genHeightScale;
                        globalSettings.GenNoiseFrequency = genNoiseFrequency;
                        globalSettings.GenNoiseOctaves = genNoiseOctaves;
                        globalSettings.GenNoiseGain = genNoiseGain;
                        globalSettings.GenBaseHeightRatio = genBaseHeightRatio;
                        globalSettings.GenSoilDepth = genSoilDepth;
                        globalSettings.GenSmoothIterations = genSmoothIterations;
                        globalSettings.GenEnableRiver = genEnableRiver;
                        globalSettings.GenRiverStrength = genRiverStrength;
                        globalSettings.GenRiverRadiusMultiplier = genRiverRadiusMultiplier;
                        globalSettings.GenRiverErosionPower = genRiverErosionPower;
                        globalSettings.GenRiverMinHeight = genRiverMinHeight;
                        globalSettings.Save();

                        SiteGenerationSettings generationSettings = new SiteGenerationSettings
                        {
                            HeightScale = genHeightScale,
                            NoiseFrequency = genNoiseFrequency,
                            NoiseOctaves = genNoiseOctaves,
                            NoiseGain = genNoiseGain,
                            BaseHeightRatio = genBaseHeightRatio,
                            SoilDepth = genSoilDepth,
                            SmoothIterations = genSmoothIterations,
                            EnableRiver = genEnableRiver,
                            RiverStrength = genRiverStrength,
                            RiverRadiusMultiplier = genRiverRadiusMultiplier,
                            RiverErosionPower = genRiverErosionPower,
                            RiverMinHeight = genRiverMinHeight
                        };

                        Game.StartNewSite(new Point3(newSiteSizeX, newSiteSizeY, newSiteSizeZ), newSiteSeed, generationSettings);
                    }

                    ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - bSize.X) * 0.5f);
                    if (ImGui.Button("Reset Generation Defaults", bSize))
                    {
                        var defaults = new SiteGenerationSettings();
                        genHeightScale = defaults.HeightScale;
                        genNoiseFrequency = defaults.NoiseFrequency;
                        genNoiseOctaves = defaults.NoiseOctaves;
                        genNoiseGain = defaults.NoiseGain;
                        genBaseHeightRatio = defaults.BaseHeightRatio;
                        genSoilDepth = defaults.SoilDepth;
                        genSmoothIterations = defaults.SmoothIterations;
                        genEnableRiver = defaults.EnableRiver;
                        genRiverStrength = defaults.RiverStrength;
                        genRiverRadiusMultiplier = defaults.RiverRadiusMultiplier;
                        genRiverErosionPower = defaults.RiverErosionPower;
                        genRiverMinHeight = defaults.RiverMinHeight;

                        globalSettings.GenHeightScale = genHeightScale;
                        globalSettings.GenNoiseFrequency = genNoiseFrequency;
                        globalSettings.GenNoiseOctaves = genNoiseOctaves;
                        globalSettings.GenNoiseGain = genNoiseGain;
                        globalSettings.GenBaseHeightRatio = genBaseHeightRatio;
                        globalSettings.GenSoilDepth = genSoilDepth;
                        globalSettings.GenSmoothIterations = genSmoothIterations;
                        globalSettings.GenEnableRiver = genEnableRiver;
                        globalSettings.GenRiverStrength = genRiverStrength;
                        globalSettings.GenRiverRadiusMultiplier = genRiverRadiusMultiplier;
                        globalSettings.GenRiverErosionPower = genRiverErosionPower;
                        globalSettings.GenRiverMinHeight = genRiverMinHeight;
                        globalSettings.Save();
                    }

                    ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - bSize.X) * 0.5f);
                    if (ImGui.Button("Back", bSize))
                    {
                        newSiteMenu = false;
                    }

                    ImGui.PopFont();
                }
                ImGui.EndChild();
            }
            ImGui.End();
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
            else if (newSiteMenu)
            {
                DrawNewSiteWindow(flags, bSize, hMargin);
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
                        newSiteMenu = true;
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
            if (InputManager.JustPressed("game.exit"))
            {
                if (optionsMenu)
                    optionsMenu = false;
                else if (newSiteMenu)
                    newSiteMenu = false;
            }
        }
    }
}