using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.StaticBatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin
{
    public class Map
    {
        public static Vector3 MAP_SIZE = new Vector3(100, 100, 1);
        public static Point TILE_SIZE = new Point(32,16);
        public static Vector2 MAP_OFFSET = new Vector2(0, 0);

        private Texture2D mainTexture;

        private Tile[,,] blocks;

        private SpriteBatch spriteBatch;
        private StaticBatch staticGrid;
        private List<StaticBatch.StaticSprite> _sprites;
        Rectangle viewport;

        public Map()
        {
            mainTexture = MainGame.instance.Content.Load<Texture2D>("default");
            blocks = new Tile[(int)MAP_SIZE.X, (int)MAP_SIZE.Y, (int)MAP_SIZE.Z];
            for (int i = 0; i < MAP_SIZE.Z/2; i++)
            {
                for (int x = 0; x < MAP_SIZE.X; x++)
                {
                    for (int y = 0; y < MAP_SIZE.Y; y++)
                    {
                        blocks[x,y,i] = new Tile(1, 1);
                    }
                }
            }


            spriteBatch = new SpriteBatch(MainGame.instance.GraphicsDevice);
            _sprites = new List<StaticBatch.StaticSprite>();
            staticGrid = new StaticBatch(MainGame.instance.GraphicsDevice, new Point(512,512));
            Rectangle blockPos = new Rectangle(0, 72, 32, 32);
            Rectangle floorPos = new Rectangle(0, 52, 32, 20);


            for (int i = 0; i < MAP_SIZE.Z; i++)
            {
                for (int y = 0; y < MAP_SIZE.Y; y++)
                {
                    for (int x = 0; x < MAP_SIZE.X; x++)
                    {
                        Point scr = MapToScreen(x, y).ToPoint();
                        //draw block
                        _sprites.Add(new StaticBatch.StaticSprite(mainTexture, 
                            new Rectangle(scr, TILE_SIZE),
                            blockPos, zindex: i*2+1f));


                        //draw floor
                        /*_sprites.Add(new StaticBatch.StaticSprite(mainTexture,
                                        new Rectangle(TILE_SIZE * new Point(x, y), TILE_SIZE),
                                        floorPos, zindex: i*2));*/
                    }
                }
            }
            staticGrid.AddSprites(_sprites);
            staticGrid.Build(spriteBatch);

            
            
        }

        private Vector2 MapToScreen(int mapX, int mapY)
        {
            var screenX = ((mapX - mapY) * TILE_SIZE.X/2);
            var screenY = ((mapY + mapX) * TILE_SIZE.Y/2);

            Vector2 res = new Vector2(screenX, screenY);
            //res += MainGame.cam.Pos;
            //res *= MainGame.cam.Zoom;
            //Matrix inverted = Matrix.Invert(MainGame.cam.get_transformation(MainGame.instance.GraphicsDevice));
            return res;
        }

        private Point ScreenToMap(Point mousePos)
        {
            Vector2 cursor = new Vector2(mousePos.X, mousePos.Y);
            
            cursor = cursor / MainGame.cam.Zoom;
            cursor = cursor + MainGame.cam.Pos;
            
            var x = cursor.X + (2 * cursor.Y) - (TILE_SIZE.X / 2);
            int mapX = (x < 0) ? -1 : (int)(x / TILE_SIZE.X);
            var y = -cursor.X + (2 * cursor.Y) + (TILE_SIZE.X / 2);
            int mapY = (y < 0) ? -1 : (int)(y / TILE_SIZE.X);

            Vector2 res = new Vector2(mapX, mapY);
            return new Point(mapX,mapY);
        }

        
        public void Update()
        {
            MainGame.debug.Add("Pos: "+MainGame.cam.Pos.ToString());

            viewport = MainGame.instance.GraphicsDevice.Viewport.Bounds;
            /*if ( InputManager.Direction != Point.Zero)
            {
                _tiles[_keyboardSelected.X, _keyboardSelected.Y].KeyboardDeselect();
                _keyboardSelected.X = Math.Clamp(_keyboardSelected.X + InputManager.Direction.X, 0, MAP_SIZE.X - 1);
                _keyboardSelected.Y = Math.Clamp(_keyboardSelected.Y + InputManager.Direction.Y, 0, MAP_SIZE.Y - 1);
                _tiles[_keyboardSelected.X, _keyboardSelected.Y].KeyboardSelect();
            }*/
        }

        public void Draw()
        {
            spriteBatch.Begin(
                SpriteSortMode.Texture,
                transformMatrix: MainGame.cam.get_transformation(MainGame.instance.GraphicsDevice)
                );
            Rectangle blockPos = new Rectangle(0, 72, 32, 32);
            Rectangle floorPos = new Rectangle(0, 52, 32, 20);
            for (int i = 0; i < MAP_SIZE.Z; i++)
            {
                for (int y = 0; y < MAP_SIZE.Y; y++)
                {
                    for (int x = 0; x < MAP_SIZE.X; x++)
                    {
                        //draw block
                        spriteBatch.Draw(mainTexture,
                            MapToScreen(x, y),
                            blockPos,
                            Color.White
                            );

                        //draw floor
                        spriteBatch.Draw(mainTexture,
                            MapToScreen(x, y) - new Vector2(0, 4),
                            floorPos,
                            Color.White
                            );
                    }
                }
            }


            spriteBatch.End();
            /*staticGrid.Draw(spriteBatch, viewport,
                offset: MainGame.cam.Pos.ToPoint());*/

        }
    }
}
