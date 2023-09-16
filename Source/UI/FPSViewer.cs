using MGUI.Core.UI;
using Arch.Bus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Origin.Source.Events;

namespace Origin.Source.UI
{
    public partial class FPSViewer : MGWindow
    {
        private MGTextBlock textBlock;

        public FPSViewer(MGDesktop Desktop, int Left, int Top, int Width, int Height, MGTheme Theme = null) : base(Desktop, Left, Top, Width, Height, Theme)
        {
            textBlock = new MGTextBlock(this, "");
            SetContent(textBlock);
            IsDraggable = false;
            IsUserResizable = false;
            WindowStyle = WindowStyle.None;
            ApplySizeToContent(SizeToContent.WidthAndHeight, 0, 0);
            Hook();
        }

        [Event]
        public void OnChangeEnableFps(FpsEnableChanged fpsEnableChanged)
        {
            IsEnabled = fpsEnableChanged.IsEnabled;
        }

        [Event]
        public void OnUpdateFps(UpdateFps updateFps)
        {
            textBlock.Text = ((int)updateFps.fps).ToString();
        }

        [Event]
        public void OnScreenBoundsChanged(ScreenBoundsChanged bounds)
        {
            Left = bounds.screenBounds.Width - this.ActualWidth / 2;
            Top = 0;
        }
    }
}