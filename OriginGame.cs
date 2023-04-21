using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;

using Origin.Source;
using Origin.Source.GameStates;
using Origin.Source.GCs;
using Origin.Source.IO;
using Origin.Source.Utils;

namespace Origin
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class OriginGame : Game
    {
        public static OriginGame Instance { get; private set; }
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;

        private ScreenManager _screenManager;

        public FpsCountGC fpsCounter;
        public InfoDrawerGC debug;

        public static int ScreenWidth { get; private set; }
        public static int ScreenHeight { get; private set; }

        public OriginGame()
        {
            Instance = this;
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";

            _screenManager = new ScreenManager();
            fpsCounter = new FpsCountGC();
            debug = new InfoDrawerGC(new Point(10, 10), Color.Aqua);

            Components.Add(_screenManager);
            Components.Add(fpsCounter);
            Components.Add(debug);
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
            graphics.ApplyChanges();
            Window.Title = "Dwarf`s Origin";

            IsMouseVisible = true;

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

            ScreenHeight = graphics.PreferredBackBufferHeight;
            ScreenWidth = graphics.PreferredBackBufferWidth;

            debug.Clear();
            debug.Add("-/+ to zoom; [/] to change level; arrows to move; esc to exit");
            debug.Add(fpsCounter.msg);

            base.Update(gameTime);
            InputManager.FinalUpdate();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
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