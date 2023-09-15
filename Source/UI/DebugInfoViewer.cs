using MGUI.Core.UI;

using Origin.Source.Events;
using Arch.Bus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using MGUI.Core.UI.Containers;
using System.Windows.Input;
using System.Windows.Documents;

namespace Origin.Source.UI
{
    public partial class DebugInfoViewer : MGWindow
    {
        private Dictionary<int, Dictionary<string, string>> values = new();
        private bool IsChanged = false;
        private MGStackPanel stackPanel;
        private List<MGTextBlock> lines = new List<MGTextBlock>();

        public DebugInfoViewer(MGDesktop Desktop, int Left, int Top, int Width, int Height, MGTheme Theme = null) : base(Desktop, Left, Top, Width, Height, Theme)
        {
            stackPanel = new MGStackPanel(this, Orientation.Vertical);
            SetContent(stackPanel);

            IsDraggable = false;
            IsUserResizable = false;
            WindowStyle = WindowStyle.None;
            ApplySizeToContent(SizeToContent.WidthAndHeight, 0, 0);
            Hook();
        }

        public void Update()
        {
            if (IsChanged)
            {
                Build();
                IsChanged = false;
            }
        }

        private void Build()
        {
            var list = values.Keys.ToList();
            list.Sort();
            int iter = 0;
            foreach (var order in list)
            {
                foreach (var key in values[order].Keys)
                {
                    if (lines.Count <= iter)
                    {
                        lines.Add(new MGTextBlock(this, key + ": " + values[order][key]));
                        stackPanel.TryAddChild(lines[^1]);
                    }
                    else
                        lines[iter].Text = key + ": " + values[order][key];
                    iter++;
                    /*stackPanel.TryAddChild(new MGTextBlock(this, key + ": " + values[order][key]));*/
                }
            }
        }

        [Event]
        public void OnChangeEnableFps(DebugWindowEnableChanged debugWindowEnableChanged)
        {
            IsEnabled = debugWindowEnableChanged.IsEnabled;
        }

        [Event]
        public void OnDebugValueChanged(DebugValueChanged valueChanged)
        {
            foreach (var key in valueChanged.values.Keys)
            {
                if (!values.ContainsKey(valueChanged.order))
                    values.Add(valueChanged.order, new Dictionary<string, string>());
                values[valueChanged.order][key] = valueChanged.values[key];
            }
            IsChanged = true;
        }

        [Event]
        public void OnScreenBoundsChanged(ScreenBoundsChanged bounds)
        {
            Left = 0;
            Top = 0;
        }
    }
}