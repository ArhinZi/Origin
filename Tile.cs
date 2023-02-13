using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin
{
    public class Tile
    {
        private readonly Texture2D _texture;
        private readonly Vector2 _position;
        private bool _keyboardSelected;
        private bool _mouseSelected;

        public Tile(Texture2D texture, Vector2 position)
        {
            _texture = texture;
            _position = position;
        }

        public void KeyboardSelect()
        {
            _keyboardSelected = true;
        }

        public void KeyboardDeselect()
        {
            _keyboardSelected = false;
        }

        public void MouseSelect()
        {
            _mouseSelected = true;
        }

        public void MouseDeselect()
        {
            _mouseSelected = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var color = Color.White;
            if (_keyboardSelected) color = Color.Red;
            if (_mouseSelected) color = Color.Green;
            spriteBatch.Draw(_texture, _position, color);
        }
    }
}
