global using ArchWorld = Arch.Core.World;
global using Point3 = Origin.Source.Utils.Point3;

using Arch.Bus;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;
using MonoGame.ImGuiNet;

using Origin.Source.Controller.IO;
using Origin.Source.ECS.BaseComponents;
using Origin.Source.Events;
using Origin.Source.GameStates;
using Origin.Source.Resources;

using System;

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
            graphics.PreferredBackBufferHeight = 1024;
            graphics.PreferredBackBufferWidth = 1024;
            graphics.IsFullScreen = false;
            graphics.SynchronizeWithVerticalRetrace = false; //Vsync
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0f / 60);
            graphics.ApplyChanges();

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

            GuiRenderer.RebuildFontAtlas();
            //LoadMenuMainScreen();
            LoadGameScreen();

            ArchWorld world = ArchWorld.Create();
            world.Create<IsTile>();
            world.Create<IsTile>();
            world.Create<IsTile>();

            SaveGameEntity.ReadAllSaves();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
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

            GuiRenderer.EndLayout();
        }

        private void LoadMenuMainScreen()
        {
            _screenManager.LoadScreen(new StateMenuMain(this), new FadeTransition(GraphicsDevice, Color.Black, 0));
        }

        private void LoadGameScreen()
        {
            _screenManager.LoadScreen(new StateMainGame(this), new FadeTransition(GraphicsDevice, Color.Black, 0));
        }
    }
}