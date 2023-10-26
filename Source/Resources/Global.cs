using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}