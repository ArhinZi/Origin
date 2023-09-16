using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Origin.Source.GameStates;
using Origin.Source.IO;

using System;

namespace Origin.Source
{
    public class Camera2D
    {
        private float _zoom = 1f;
        private float _minZoom = 0.05f;
        private float _maxZoom = 4f;
        private Matrix _projection;
        private Matrix _transformation;

        public int base_mult = 5;
        public int shift_mult = 30;
        public float zoom_step = 0.02f;

        #region Set Get

        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                if (_zoom < _minZoom) _zoom = _minZoom;
                if (_zoom > _maxZoom) _zoom = _maxZoom;
            } // Negative zoom will flip image
        }

        public float MinZoom
        { set { _minZoom = value; } }

        public float MaxZoom
        { set { _maxZoom = value; } }

        public float AspectPatio
        {
            get
            {
                return (float)OriginGame.Instance.GraphicsDevice.Viewport.Width / OriginGame.Instance.GraphicsDevice.Viewport.Height;
            }
        }

        public Vector2 Position { get; set; }

        public Matrix WorldMatrix { get; private set; }

        public Matrix Projection
        {
            get
            {
                _projection = Matrix.CreateOrthographicOffCenter(0, OriginGame.ScreenWidth, OriginGame.ScreenHeight, 0, -100, 100) * Matrix.CreateScale(1, AspectPatio, 1);
                return _projection;
            }
        }

        public Matrix Transformation
        {
            get
            {
                _transformation =
                    Matrix.CreateTranslation(-Position.X, -Position.Y, 1) *
                    Matrix.CreateScale(Zoom, Zoom, 1) *
                    Matrix.CreateTranslation(new Vector3(OriginGame.ScreenWidth * 0.5f, OriginGame.ScreenHeight * 0.5f, 0));
                return _transformation;
            }
            private set
            {
                _transformation = value;
            }
        }

        #endregion Set Get

        public Camera2D()
        {
            Zoom = 1.0f;
            Position = Vector2.Zero;

            WorldMatrix = Matrix.CreateWorld(new Vector3(0, 0, 0), new Vector3(0, 0, -1), Vector3.Up);
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            int movemod = keyboardState.IsKeyDown(Keys.LeftShift) ? shift_mult : base_mult;
            if (InputManager.IsPressed("Camera.left"))
                Move(new Vector2(-1 * movemod, 0));
            if (InputManager.IsPressed("Camera.right"))
                Move(new Vector2(1 * movemod, 0));
            if (InputManager.IsPressed("Camera.up"))
                Move(new Vector2(0, -1 * movemod));
            if (InputManager.IsPressed("Camera.down"))
                Move(new Vector2(0, 1 * movemod));

            if (InputManager.IsPressed("Camera.zoom.plus"))
                Zoom += zoom_step * Zoom;
            if (InputManager.IsPressed("Camera.zoom.minus"))
                Zoom -= zoom_step * Zoom;
        }

        // Auxiliary function to move the camera
        public void Move(Vector2 amount)
        {
            Position += amount;
        }

        public Vector2 ScreenToWorld(Vector2 screenPos, int currentLevel)
        {
            throw new NotImplementedException();
        }
    }
}