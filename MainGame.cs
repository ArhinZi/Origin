using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;
using Origin.Draw;
using Origin.ECS;
using Origin.IO;
using Origin.Screens;
using Origin.Utils;
using System.Diagnostics;

namespace Origin
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
        private World _ecs;
        public FpsCountComp fpsCounter;
        public ControlComp control;
        public InfoDrawer debug;

        private TileSet tileset;

        public static Camera2D cam;

        public static int ScreenWidth;
        public static int ScreenHeight;

        public MainGame()
        {
            Instance = this;
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";

            _ecs = new WorldBuilder()
                .Build();

            _screenManager = new ScreenManager();
            fpsCounter = new FpsCountComp();
            control = new ControlComp();
            debug = new InfoDrawer(new Point(10, 10), Color.Black);

            Components.Add(_ecs);
            Components.Add(_screenManager);
            Components.Add(fpsCounter);
            Components.Add(control);
            Components.Add(debug);

            /*CircleSliceArray<int> a = new CircleSliceArray<int>(5);

            for (int i = 0; i <= 25; i++)
            {
                a[i] = i;
            }
            for (int i = 24; i >= 15; i--)
            {
                a[i] = i;
            }
            for (int i = 15; i <= 19; i++)
            {
                a[i] = i + 100;
            }
            for (int i = 100; i < 105; i++)
            {
                a[i] = i;
            }*/

            /*for (int i = 0; i <= 25; i++)
            {
                a.AddAfterTail(i);
            }
            for (int i = a.Start; i < a.Start + a.Count; i++)
            {
                Debug.WriteLine(a[i]);
            }
            Debug.WriteLine("-----------");
            int c = a.Start - 5;
            for (int i = a.Start; i > c; i--)
            {
                a.AddBeforeHead(i);
            }
            for (int i = a.Start; i < a.Start + a.Count; i++)
            {
                Debug.WriteLine(a[i]);
            }*/
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
            graphics.ApplyChanges();

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

            tileset = new TileSet();

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
            debug.Add(fpsCounter.msg);

            _ecs.Update(gameTime);

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
            _ecs.Draw(gameTime);

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