global using ArchWorld = Arch.Core.World;
global using Point3 = Origin.Source.Utils.Point3;

using Arch.Bus;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;
using MonoGame.ImGuiNet;

using Origin.Source.Controller.IO;
using Origin.Source.Events;
using Origin.Source.GameStates;
using Origin.Source.Model.Generators;
using Origin.Source.Resources;
using Origin.Source.Save;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;

namespace Origin.Source
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class OriginGame : Game
    {
        //public static OriginGame Instance { get; private set; }

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;

        private ScreenManager _screenManager;

        private Rectangle _prevBounds;

        public DebugMonitor dmonitor;

        public static ImGuiRenderer GuiRenderer;

        private bool _showResourceLoadReport;

        public OriginGame()
        {
            Global.Game = this;
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";

            _screenManager = new ScreenManager();
            dmonitor = new DebugMonitor(this);
            // Sleep time when Window not in focus
            InactiveSleepTime = TimeSpan.FromMilliseconds(100);

            Components.Add(_screenManager);
            Components.Add(dmonitor);
            //Components.Add(debug);
        }

        public bool InFocus()
        {
            if (IsActive) return true;
            return false;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            var settings = GlobalSettings.Load();

            graphics.PreferredBackBufferHeight = settings.ResolutionHeight;
            graphics.PreferredBackBufferWidth = settings.ResolutionWidth;
            graphics.IsFullScreen = settings.Fullscreen;
            graphics.SynchronizeWithVerticalRetrace = settings.VSync; //Vsync
            ApplyFpsLimit(settings.FpsLimit);
            graphics.ApplyChanges();

            ApplyAudioSettings(settings.MasterVolume, settings.MusicVolume, settings.SfxVolume);

            Global.GraphicsDevice = GraphicsDevice;

            Window.Title = "Dwarf`s Origin";

            IsMouseVisible = true;

            GuiRenderer = new ImGuiRenderer(this);

            InputManager.Initialise(this);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Content.Load<SpriteFont>("basefont");

            // Load Resources
            ResourceLoader.LoadResources();
            _showResourceLoadReport = ResourceLoader.LastReport.HasMessages;
            //SaveGameEntity.ReadAllSaves();

            GuiRenderer.RebuildFontAtlas();
            LoadMenuMainScreen();
        }

        public void LoadMenuMainScreen()
        {
            _screenManager.LoadScreen(new StateMenuMain(this), new FadeTransition(GraphicsDevice, Color.Black, 0));
        }

        public void StartNewSite(Point3? siteSize = null, int? generationSeed = null, SiteGenerationSettings generationSettings = null)
        {
            _screenManager.LoadScreen(new StateMainGame(this, true, siteSize, generationSeed, generationSettings), new FadeTransition(GraphicsDevice, Color.Black, 0));
        }

        public void LoadGameScreen()
        {
            _screenManager.LoadScreen(new StateMainGame(this), new FadeTransition(GraphicsDevice, Color.Black, 0));
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (IsActive) InputManager.Update(gameTime);

            if (_prevBounds != Window.ClientBounds)
            {
                EventBus.Send(new ScreenBoundsChanged(new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height)));
                _prevBounds = Window.ClientBounds;
                GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height);
                graphics.ApplyChanges();
            }

            base.Update(gameTime);
            if (IsActive) InputManager.FinalUpdate();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            //GraphicsDevice.Clear(Color.CornflowerBlue);
            GuiRenderer.BeginLayout(gameTime);
            ImGui.PushFont(GlobalResources.Fonts["Default"]);

            base.Draw(gameTime);

            DrawResourceLoadReportWindow();

            GuiRenderer.EndLayout();
        }

        private void DrawResourceLoadReportWindow()
        {
            if (!_showResourceLoadReport || !ResourceLoader.LastReport.HasMessages)
                return;

            var report = ResourceLoader.LastReport;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(900, 540), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Resource Load Warnings", ref _showResourceLoadReport))
            {
                ImGui.TextWrapped("Resource loading completed with warnings/errors. Merge was applied. Please review details below.");

                if (ImGui.Button("Copy report"))
                {
                    var sb = new StringBuilder();

                    if (report.ParseErrors.Count > 0)
                    {
                        sb.AppendLine($"JSON Parse Errors ({report.ParseErrors.Count})");
                        foreach (var msg in report.ParseErrors)
                            sb.AppendLine($"- {msg}");
                        sb.AppendLine();
                    }

                    if (report.MergeWarnings.Count > 0)
                    {
                        sb.AppendLine($"Merge Conflicts/Overwrites ({report.MergeWarnings.Count})");
                        foreach (var msg in report.MergeWarnings)
                            sb.AppendLine($"- {msg}");
                        sb.AppendLine();
                    }

                    if (report.ValidationErrors.Count > 0)
                    {
                        sb.AppendLine($"Validation Errors ({report.ValidationErrors.Count})");
                        foreach (var msg in report.ValidationErrors)
                            sb.AppendLine($"- {msg}");
                    }

                    ImGui.SetClipboardText(sb.ToString());
                }

                ImGui.Separator();

                if (report.ParseErrors.Count > 0 && ImGui.CollapsingHeader($"JSON Parse Errors ({report.ParseErrors.Count})", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach (var msg in report.ParseErrors)
                        ImGui.BulletText(msg);
                }

                if (report.MergeWarnings.Count > 0 && ImGui.CollapsingHeader($"Merge Conflicts/Overwrites ({report.MergeWarnings.Count})", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach (var msg in report.MergeWarnings)
                        ImGui.BulletText(msg);
                }

                if (report.ValidationErrors.Count > 0 && ImGui.CollapsingHeader($"Validation Errors ({report.ValidationErrors.Count})", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach (var msg in report.ValidationErrors)
                        ImGui.BulletText(msg);
                }
            }
            ImGui.End();
        }

        public void ApplyGraphicsSettings(int width, int height, bool fullscreen, bool vsync, int fpsLimit)
        {
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            graphics.IsFullScreen = fullscreen;
            graphics.SynchronizeWithVerticalRetrace = vsync;
            ApplyFpsLimit(fpsLimit);
            graphics.ApplyChanges();
        }

        public void ApplyAudioSettings(float masterVolume, float musicVolume, float sfxVolume)
        {
            masterVolume = MathHelper.Clamp(masterVolume, 0f, 1f);
            musicVolume = MathHelper.Clamp(musicVolume, 0f, 1f);
            sfxVolume = MathHelper.Clamp(sfxVolume, 0f, 1f);

            SoundEffect.MasterVolume = masterVolume * sfxVolume;
            MediaPlayer.Volume = masterVolume * musicVolume;
        }

        private void ApplyFpsLimit(int fpsLimit)
        {
            if (fpsLimit <= 0)
            {
                IsFixedTimeStep = false;
                return;
            }

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / fpsLimit);
        }

        public IReadOnlyList<(int Width, int Height)> GetSupportedResolutions()
        {
            return GraphicsAdapter.DefaultAdapter.SupportedDisplayModes
                .Select(m => (m.Width, m.Height))
                .Distinct()
                .OrderBy(r => r.Width)
                .ThenBy(r => r.Height)
                .ToList();
        }

        public new void Dispose()
        {
            base.Dispose();
            _screenManager.Dispose();
        }
    }
}