using Arch.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Model.Site;

using System;
using System.IO;

namespace Origin.Source.Resources
{
    public static class Global
    {
        public enum DrawBufferLayer : byte
        {
            HiddenBack = 0,
            Back = 1,
            Water = 2,
            BackNoLight = 4,
            BackInteractives = 5,
            HiddenFront = 10,
            Front = 11,
            FrontOver = 12,
            FrontNoLight = 14,
            FrontInteractives = 15,
        }

        public static readonly byte LightFrontStart = 11;

        public static byte[] NoLightLayers = [
            (byte)DrawBufferLayer.HiddenBack,
            (byte)DrawBufferLayer.BackInteractives,
            (byte)DrawBufferLayer.BackNoLight,
            (byte)DrawBufferLayer.HiddenFront,
            (byte)DrawBufferLayer.FrontInteractives,
            (byte)DrawBufferLayer.FrontNoLight,
        ];

        public enum FluidType : byte // 4b - 16 vals
        {
            WATER = 0,
            LAVA = 1
        }

        public enum Direction
        {
            NONE,
            NORTH,
            EAST,
            WEST,
            SOUTH
        }

        #region Camera

        public static readonly float SITE_CAM_MIN_ZOOM = 0.05f;
        public static readonly float SITE_CAM_MAX_ZOOM = 4f;
        public static readonly int CAM_SPEED = 1000;
        public static readonly int CAM_SHIFT_SPEED_MULT = 4;
        public static readonly int CAM_ZOOM_SPEED = 1;
        public static readonly int CAM_MOUSE_ZOOM_SPEED = 10;

        #endregion Camera

        #region Render

        public static GraphicsDevice GraphicsDevice;
        public static Game Game;
        public static Model.World World;
        public static Camera2D ActiveCamera;

        /// <summary>
        /// Z offset.
        /// Blocks in the far diagonal line are appearing behind the ones in the near line.
        /// </summary>
        public static readonly float Z_DIAGONAL_OFFSET = 0.01f;

        /// <summary>
        /// Z offset.
        /// Blocks will have different Z coordinate depending on level
        /// </summary>
        public static readonly float Z_LEVEL_OFFSET = 0.01f;

        public static readonly Point BASE_CHUNK_SIZE = new(512, 512);
        public static readonly int ONE_MOMENT_DRAW_LEVELS = 32;

        //public static int GPU_LAYER_PACK_COUNT = 65536;
        public const int GPU_LAYER_PACK_COUNT = 1024;

        #endregion Render

        public static readonly float FontSize = 20;

        public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Arhin Studio", "Origin");
    }
}