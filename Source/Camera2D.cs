﻿using Arch.Bus;

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
        private float _minZoom = 0.05f;
        private float _maxZoom = 4f;
        private Matrix _projection;
        private Matrix _transformation;

        #region Set Get

        public float Zoom
        {
            get { return _zoom; }
            set
            {
                EventBus.Send(new DebugValueChanged(2, new Dictionary<string, string>()
                {
                    ["CameraZoom"] = value.ToString("##.##")
                }));
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
                        ["CameraPosition"] = value.ToString()
                    }));
                _position = value;
            }
        }

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