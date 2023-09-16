using Arch.Bus;

using MGUI.Core.UI;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Origin.Source.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Mouse = Microsoft.Xna.Framework.Input.Mouse;

namespace Origin.Source.UI
{
    public class UIManager
    {
        private MGDesktop desktop;

        private Rectangle prevBounds;

        private FPSViewer fpsViewer;
        private DebugInfoViewer debugInfoViewer;
        private CompassControl compassControl;

        public UIManager()
        {
            desktop = new MGDesktop(OriginGame.Instance.MGUIRenderer);
            prevBounds = new Rectangle();

            fpsViewer = new FPSViewer(desktop, 0, 0, 0, 0);
            desktop.Windows.Add(fpsViewer);

            debugInfoViewer = new DebugInfoViewer(desktop, 0, 0, 0, 0);
            desktop.Windows.Add(debugInfoViewer);

            compassControl = new CompassControl(desktop, 0, 0, 64, 64);
            desktop.Windows.Add(compassControl);
        }

        public void Update()
        {
            debugInfoViewer.Update();
            fpsViewer.Update(new ElementUpdateArgs());
            compassControl.Update(new ElementUpdateArgs());
            desktop.Update();

            if (prevBounds != desktop.ValidScreenBounds)
                EventBus.Send(new ScreenBoundsChanged(desktop.ValidScreenBounds));
            prevBounds = desktop.ValidScreenBounds;

            MouseState currentMouseState = Mouse.GetState();

            EventBus.Send(new DebugValueChanged(1, new Dictionary<string, string>()
            {
                ["MousePosition"] = currentMouseState.Position.ToString()
            }));
        }

        public void Draw()
        {
            desktop.Draw();
        }
    }
}