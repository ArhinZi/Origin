using Microsoft.Xna.Framework;

namespace Origin.Source.Draw
{
    public class Camera2D
    {
        private float _zoom;
        private Matrix _projection;
        private Matrix _transformation;

        #region Set Get

        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                if (_zoom < 0.05f) _zoom = 0.05f;
                if (_zoom > 4f) _zoom = 4f;
            } // Negative zoom will flip image
        }

        public Vector2 Pos { get; set; }

        protected float Rotation { get; private set; }

        public Matrix WorldMatrix { get; private set; }

        public Matrix Projection
        {
            get
            {
                _projection = Matrix.CreateOrthographicOffCenter(0, MainGame.ScreenWidth, MainGame.ScreenHeight, 0, -1, 100);
                return _projection;
            }
            private set
            {
                _projection = value;
            }
        }

        public Matrix Transformation
        {
            get
            {
                _transformation =
                    Matrix.CreateTranslation(-Pos.X, -Pos.Y, 1) *
                    Matrix.CreateScale(Zoom, Zoom, 1) *
                    Matrix.CreateTranslation(new Vector3(MainGame.ScreenWidth * 0.5f, MainGame.ScreenHeight * 0.5f, 0));
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
            Rotation = 0.0f;
            Pos = Vector2.Zero;

            WorldMatrix = Matrix.CreateWorld(new Vector3(0, 0, 0), new Vector3(0, 0, -1), Vector3.Up);
        }

        // Auxiliary function to move the camera
        public void Move(Vector2 amount)
        {
            Pos += amount;
        }

        public Vector2 ScreenToWorld(Vector2 screenPos, int currentLevel)
        {
            return Vector2.Zero;
        }
    }
}