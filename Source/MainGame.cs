using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;

using Origin.Source.GCs;
using Origin.Source.IO;
using Origin.Source.Screens;

namespace Origin.Source
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MainGame : Game
    {
        public static MainGame Instance { get; private set; }
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;

        private ScreenManager _screenManager;
        public FpsCountGC fpsCounter;
        public ControlGC control;
        public InfoDrawerGC debug;

        public static Camera2D cam;

        public static int ScreenWidth;
        public static int ScreenHeight;

        public MainGame()
        {
            Instance = this;
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";

            _screenManager = new ScreenManager();
            fpsCounter = new FpsCountGC();
            control = new ControlGC();
            debug = new InfoDrawerGC(new Point(10, 10), Color.Aqua);

            Components.Add(_screenManager);
            Components.Add(fpsCounter);
            Components.Add(control);
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
            ScreenHeight = graphics.PreferredBackBufferHeight = 800;
            ScreenWidth = graphics.PreferredBackBufferWidth = 1024;
            //graphics.IsFullScreen = true;
            graphics.ApplyChanges();
            Window.Title = "Dwarf`s Origin";

            IsMouseVisible = true;

            cam = new Camera2D
            {
                Pos = new Vector2(0, 0),
                Zoom = 1
            };

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
            debug.Add("-/+ to zoom; [/] to change level; arrows to move");
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
            _screenManager.LoadScreen(new ScreenMenuMain(this), new FadeTransition(GraphicsDevice, Color.Black, 0));
        }

        private void LoadGameScreen()
        {
            _screenManager.LoadScreen(new ScreenMainGame(this), new FadeTransition(GraphicsDevice, Color.Black, 0));
        }
    }
}