using Microsoft.Xna.Framework;

namespace Origin.Source.Resources
{
    public static class Global
    {
        #region Camera

        public static float SITE_CAM_MIN_ZOOM = 0.05f;
        public static float SITE_CAM_MAX_ZOOM = 4f;
        public static int CAM_SPEED = 1000;
        public static int CAM_SHIFT_SPEED_MULT = 4;
        public static int CAM_ZOOM_SPEED = 1;
        public static int CAM_MOUSE_ZOOM_SPEED = 10;

        #endregion Camera

        #region Render

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

        public static readonly Point BASE_CHUNK_SIZE = new Point(128, 128);
        public static readonly int ONE_MOMENT_DRAW_LEVELS = 32;

        //public static int GPU_LAYER_PACK_COUNT = 65536;
        public static int GPU_LAYER_PACK_COUNT = BASE_CHUNK_SIZE.X * BASE_CHUNK_SIZE.Y / 4;

        #endregion Render
    }
}