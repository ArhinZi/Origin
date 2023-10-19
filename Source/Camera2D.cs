using Arch.Bus;

using Microsoft.Xna.Framework;

using Origin.Source.Events;

using System;
using System.Collections.Generic;

namespace Origin.Source
{
    public class Camera2D
    {
        private float _zoom = 1f;
        private Vector2 _position;
        private Matrix _projection;
        private Matrix _transformation;
        private float _localMinZoom;
        private float _localMaxZoom;

        #region Set Get

        private Rectangle ClientBounds => OriginGame.Instance.Window.ClientBounds;

        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                if (_zoom < _localMinZoom) _zoom = _localMinZoom;
                if (_zoom > _localMaxZoom) _zoom = _localMaxZoom;
                EventBus.Send(new DebugValueChanged(2, new Dictionary<string, string>()
                {
                    ["DebugCameraZoom"] = value.ToString("##.##")
                }));
            } // Negative zoom will flip image
        }

        public float AspectRatio
        {
            get
            {
                return OriginGame.Instance.GraphicsDevice.Viewport.AspectRatio;
            }
        }

        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value != _position)
                    EventBus.Send(new DebugValueChanged(2, new Dictionary<string, string>()
                    {
                        ["DebugCameraPosition"] = value.ToPoint().ToString()
                    }));
                _position = value;
            }
        }

        public Matrix WorldMatrix { get; private set; }

        public Matrix Projection
        {
            get
            {
                _projection = Matrix.CreateOrthographicOffCenter(0, ClientBounds.Width, ClientBounds.Height, 0, -100, 100) * Matrix.CreateScale(AspectRatio, AspectRatio, 1);
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
                    Matrix.CreateTranslation(new Vector3(ClientBounds.Width * 0.5f, ClientBounds.Height * 0.5f, 0));
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
            _localMinZoom = Global.SITE_CAM_MIN_ZOOM;
            _localMaxZoom = Global.SITE_CAM_MAX_ZOOM;

            Zoom = 1.0f;
            Position = Vector2.Zero;

            WorldMatrix = Matrix.CreateWorld(new Vector3(0, 0, 0), new Vector3(0, 0, -1), Vector3.Up);
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