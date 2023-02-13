using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Origin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin
{
    class WorldMap : IUpdateable, IDrawable
    {
        public int WorldWidth { get; private set; }
        public int WorldHeight { get; private set; }

        int[,] map;
        IDrawTile[] tileset;

        SpriteBatch spriteBatch;

        public bool Enabled { get; set; } = true;

        public int UpdateOrder { get; set; } = 1;

        public int DrawOrder { get; set; } = 0;

        public bool Visible { get; set; } = true;

        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;
        public event EventHandler<EventArgs> DrawOrderChanged;
        public event EventHandler<EventArgs> VisibleChanged;


        public WorldMap(int w, int h)
        {
            WorldHeight = h;
            WorldWidth = w;

            map = new int[WorldHeight, WorldWidth];

            SimplexNoise.Noise.Seed = 209323094; // Optional
            float scale = 0.05f;
            float[,] noiseValues = SimplexNoise.Noise.Calc2D(WorldWidth, WorldHeight, scale);
            for (int i = 0; i < WorldHeight; i++)
            {
                for (int j = 0; j < WorldWidth; j++)
                {
                    map[i, j] = (int)(noiseValues[i, j] / 128);
                }
            }

            spriteBatch = new SpriteBatch(MainGame.instance.GraphicsDevice);
        }

        public void LoadContent()
        {
            /*tileset = new IDrawTile[]{
                // grass
                new AnimatedTile(MainGame.instance.Content.Load<Texture2D>("map atlas"),
                new List<Rectangle>()
                {
                    new Rectangle(0,0,10,12),
                    new Rectangle(10,0,10,12),
                    new Rectangle(5,0,10,12)
                },
                period: 1000,
                0
                ),
                // rock
                new Tile(MainGame.instance.Content.Load<Texture2D>("map atlas"), new Rectangle(0, 12, 10, 12), 0.1f)
            };*/
        }

        public void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(blendState: BlendState.AlphaBlend, sortMode: SpriteSortMode.FrontToBack,
                transformMatrix: MainGame.cam.get_transformation(MainGame.instance.GraphicsDevice)
                );
            for (int i = WorldHeight-1; i >= 0; i--)
            {
                for (int j = WorldWidth-1; j >= 0; j--)
                {
                    tileset[map[i,j]].Draw(gameTime, spriteBatch, new Vector2(i * 10, j * 10));
                }
            }
            spriteBatch.End();
        }

        public void Update(GameTime gameTime)
        {
            foreach (var item in tileset)
            {
                if(item is IUpdate )
                {
                    (item as IUpdate).Update(gameTime);
                }
            }
        }
    }
}
