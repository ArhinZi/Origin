using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin
{
    public class Map
    {
        public static Point MAP_SIZE = new Point(100, 100);
        public static Point TILE_SIZE;
        public static Vector2 MAP_OFFSET = new Vector2(0, 0);
        private readonly Tile[,] _tiles;
        private Point _keyboardSelected = new Point(0, 0);
        private Tile _lastMouseSelected;
        private SpriteBatch spriteBatch;

        public Map()
        {
            _tiles = new Tile[MAP_SIZE.X, MAP_SIZE.Y];

            Texture2D[] textures =
            {
            MainGame.instance.Content.Load<Texture2D>("tile0"),
            MainGame.instance.Content.Load<Texture2D>("tile1"),
        };

            TILE_SIZE.X = textures[0].Width;
            TILE_SIZE.Y = textures[0].Height / 2;

            Random random = new Random();
            SimplexNoise.Noise.Seed = 209323094; // Optional
            float scale = 0.05f;

            for (int y = 0; y < MAP_SIZE.Y; y++)
            {
                for (int x = 0; x < MAP_SIZE.X; x++)
                {
                    int r = random.Next(0, textures.Length);
                    r = (int)SimplexNoise.Noise.CalcPixel2D(x, y, scale)/128;
                    _tiles[x, y] = new Tile(textures[r], MapToScreen(x, y));
                }
            }

            _tiles[_keyboardSelected.X, _keyboardSelected.Y].KeyboardSelect();

            spriteBatch = new SpriteBatch(MainGame.instance.GraphicsDevice);
        }

        private Vector2 MapToScreen(int mapX, int mapY)
        {
            var screenX = ((mapX - mapY) * TILE_SIZE.X/2);
            var screenY = ((mapY + mapX) * TILE_SIZE.Y/2);

            Vector2 res = new Vector2(screenX, screenY);
            res += MainGame.cam.Pos;
            res *= MainGame.cam.Zoom;
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
            _lastMouseSelected?.MouseDeselect();

            MouseState currentMouseState = Mouse.GetState();
            var map = ScreenToMap(currentMouseState.Position);

            if (map.X >= 0 && map.Y >= 0 && map.X < MAP_SIZE.X && map.Y < MAP_SIZE.Y)
            {
                _lastMouseSelected = _tiles[map.X, map.Y];
                _lastMouseSelected.MouseSelect();
            }

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
                transformMatrix: MainGame.cam.get_transformation(MainGame.instance.GraphicsDevice)
                );
            for (int y = 0; y < MAP_SIZE.Y; y++)
            {
                for (int x = 0; x < MAP_SIZE.X; x++)
                {
                    _tiles[x, y].Draw(spriteBatch);
                }
            }
            spriteBatch.End();
        }
    }
}
