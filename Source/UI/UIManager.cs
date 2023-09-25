using Arch.Bus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Myra.Assets;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Utility;

using Origin.Source.Events;
using Origin.Source.IO;

using System.Collections.Generic;
using System.IO;

namespace Origin.Source.UI
{
    public partial class UIManager
    {
        private Desktop _desktop;
        private Game _game;

        private List<Widget> _overingWidgets;

        private Widget _debugInfo;
        private Widget _fps;
        private Widget _compass;

        private Texture2D compassTexture;
        private Rectangle currentBounds;

        public UIManager(Game game)
        {
            _desktop = new Desktop();
            _game = game;

            FileAssetResolver assetResolver = new FileAssetResolver(Path.Combine(PathUtils.ExecutingAssemblyDirectory, "Content\\Assets"));
            AssetManager assetManager = new AssetManager(assetResolver);

            // Debug Info
            string data = File.ReadAllText("Mods\\Core\\DebugInfo.xmmp");
            _debugInfo = Project.LoadFromXml(data, assetManager).Root;

            // FPS
            _fps = new Label();
            _fps.Width = 100;

            // Compass
            _compass = new Image();
            _compass.Width = 100;
            string path = "Mods\\Core\\Compass.png";
            using (FileStream stream = new FileStream(path, FileMode.Open))
                compassTexture = Texture2D.FromStream(OriginGame.Instance.GraphicsDevice, stream);
            _compass.Background = new TextureRegion(compassTexture, new Rectangle(0, 0, 32, 32));
            _compass.Enabled = true;
            _compass.Width = _compass.Height = 64;

            _desktop.Widgets.Add(_debugInfo);
            _desktop.Widgets.Add(_fps);
            _desktop.Widgets.Add(_compass);

            (_desktop.GetWidgetByID("DebugInfo") as Label).Text = "/c[red]-/+ /cd to zoom; \n/c[red][/] /cd to change level; " +
                "\n/c[red]ARROWS /cd to move; \n/c[red]ESC /cd to exit; \n/c[red]L /cd to HalfWall Mode";

            Hook();
        }

        public void Update()
        {
            Point mousePos = new Point(InputManager.MouseX, InputManager.MouseY);

            EventBus.Send(new DebugValueChanged(1, new Dictionary<string, string>()
            {
                ["DebugMousePosition"] = mousePos.ToString()
            }));
        }

        public void Draw()
        {
            _desktop.Render();
        }

        [Event]
        public void OnChangeEnableFps(DebugWindowEnableChanged debugWindowEnableChanged)
        {
            _fps.Enabled = debugWindowEnableChanged.IsEnabled;
        }

        [Event]
        public void OnDebugValueChanged(DebugValueChanged valueChanged)
        {
            if (_debugInfo.Enabled)
                foreach (var key in valueChanged.values.Keys)
                {
                    Label lab;
                    if ((lab = _desktop.GetWidgetByID(key) as Label) != null)
                    {
                        lab.Text = valueChanged.values[key];
                    }
                }
        }

        [Event]
        public void OnScreenBoundsChanged(ScreenBoundsChanged bounds)
        {
            _fps.Left = (int)(bounds.screenBounds.Width - _fps.Width / 2);
            _fps.Top = 10;

            _compass.Left = (int)(bounds.screenBounds.Width - _compass.Width * 2);

            currentBounds = bounds.screenBounds;
        }

        [Event]
        public void OnUpdateFps(UpdateFps updateFps)
        {
            (_fps as Label).Text = ((int)updateFps.fps).ToString();
        }
    }
}