using Arch.Bus;

using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Rendering;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;

using Origin.Source;
using Origin.Source.Events;
using Origin.Source.GameStates;
using Origin.Source.GCs;
using Origin.Source.IO;
using Origin.Source.UI;
using Origin.Source.Utils;

using SharpDX.Direct2D1;

using System;
using System.Collections.Generic;

using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;

namespace Origin
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class OriginGame : Game, IObservableUpdate
    {
        public static OriginGame Instance { get; private set; }
        public MainRenderer MGUIRenderer { get; private set; }

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;

        private ScreenManager _screenManager;

        public FpsCountGC fpsCounter;
        public InfoDrawerGC debug;

        public event EventHandler<TimeSpan> PreviewUpdate;

        public event EventHandler<EventArgs> EndUpdate;

        public static int ScreenWidth { get; private set; }
        public static int ScreenHeight { get; private set; }

        public UIManager UiManager;

        public OriginGame()
        {
            Instance = this;
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";

            _screenManager = new ScreenManager();
            fpsCounter = new FpsCountGC();
            debug = new InfoDrawerGC(new Point(10, 10), Color.Aqua);
            // Sleep time when Window not in focus
            InactiveSleepTime = TimeSpan.FromMilliseconds(100);

            Components.Add(_screenManager);
            Components.Add(fpsCounter);
            //Components.Add(debug);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            ScreenHeight = graphics.PreferredBackBufferHeight = 1024;
            ScreenWidth = graphics.PreferredBackBufferWidth = 1024;
            //graphics.IsFullScreen = true;
            IsFixedTimeStep = true;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();
            Window.Title = "Dwarf`s Origin";

            IsMouseVisible = true;

            MGUIRenderer = new(new GameRenderHost<OriginGame>(this));
            UiManager = new UIManager();

            EventBus.Send(new DebugValueChanged(0, new Dictionary<string, string>()
            {
                ["Info"] = "-/+ to zoom; [/] to change level; arrows to move; esc to exit"
            }));

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

            debug.spriteBatch = spriteBatch;
            debug.font = spriteFont;
            //LoadMenuMainScreen();
            LoadGameScreen();
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
            InputManager.Update();
            PreviewUpdate?.Invoke(this, gameTime.TotalGameTime);

            ScreenHeight = graphics.PreferredBackBufferHeight;
            ScreenWidth = graphics.PreferredBackBufferWidth;

            debug.Clear();

            UiManager.Update();
            base.Update(gameTime);
            InputManager.FinalUpdate();
            EndUpdate?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
            long drawcalls = GraphicsDevice.Metrics.DrawCount;
            EventBus.Send(new DebugValueChanged(3, new Dictionary<string, string>()
            {
                ["DrawCalls"] = drawcalls.ToString()
            }));
            UiManager.Draw();
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