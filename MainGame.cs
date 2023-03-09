using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Origin.Draw;
using Origin.IO;
using Origin.World;
using SimplexNoise;

namespace Origin
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MainGame : Game
    {
        public static MainGame instance;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        KeyboardController kbController;
        MainWorld _world;
        TileSet tileset;

        public static Camera2D cam;
        public static InfoDrawer debug;

        private SimpleFps _frameCounter = new SimpleFps();

        public static int ScreenWidth;
        public static int ScreenHeight;


        public MainGame()
        {
            instance = this;
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            cam = new Camera2D();
            cam.Pos = new Vector2(0,0);
            cam.Zoom = 1;

            _world = new MainWorld();
            

            kbController = new KeyboardController(_world);
            

            

            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.ApplyChanges();


            IsMouseVisible = true;


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
            debug = new InfoDrawer(spriteFont, new Point(10, 10), Color.Black);

            tileset = new TileSet();

            // TODO: use this.Content to load your game content here
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
            ScreenHeight = graphics.PreferredBackBufferHeight;
            ScreenWidth = graphics.PreferredBackBufferWidth;
            _frameCounter.Update(gameTime);
            debug.Set(_frameCounter.msg);

            kbController.Update(gameTime);

            _world.Update();


            MouseState currentMouseState = Mouse.GetState();
            debug.Add(currentMouseState.Position.ToString());

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _world.Draw();

            _frameCounter.Draw();

            
            spriteBatch.Begin();
            debug.Draw(spriteBatch);

            // TODO: Add your drawing code here

            spriteBatch.End();

            

            base.Draw(gameTime);
        }
    }
}
