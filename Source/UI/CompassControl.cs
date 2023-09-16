using MGUI.Core.UI;
using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Events;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.UI
{
    public partial class CompassControl : MGWindow
    {
        private Texture2D compassTexture;
        private MGImage image;

        public CompassControl(MGDesktop Desktop, int Left, int Top, int Width, int Height, MGTheme Theme = null) : base(Desktop, Left, Top, Width, Height, Theme)
        {
            string path = "Mods\\Core\\Compass.png";
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                compassTexture = Texture2D.FromStream(OriginGame.Instance.GraphicsDevice, stream);
            }

            IsDraggable = false;
            IsUserResizable = false;
            WindowStyle = WindowStyle.None;
            Width = 64;
            Height = 64;

            image = new MGImage(this, compassTexture, new Rectangle(0, 0, 32, 32), Stretch: Stretch.Uniform);
            SetContent(image);
            Hook();
        }

        [Event]
        public void OnScreenBoundsChanged(ScreenBoundsChanged bounds)
        {
            Left = bounds.screenBounds.Width - ActualWidth * 2;
            Top = 0;
        }
    }
}