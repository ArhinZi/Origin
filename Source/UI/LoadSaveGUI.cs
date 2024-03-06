using ImGuiNET;

using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.UI
{
    public static class LoadSaveGUI
    {
        public static void Draw()
        {
            var saves = SaveGameEntity.Saves;

            ImGui.PushFont(GlobalResources.Fonts["BoldTitle"]);
            var flags = (int)(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);
            var bSize = new Vector2(300, 100);
            int hMargin = 10;

            if (ImGui.Begin("Load", (ImGuiWindowFlags)flags))
            {
                foreach (var save in saves)
                {
                    ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * hMargin);
                    ImGuiUtil.AlignForWidth(bSize.X);
                    var t_id = OriginGame.GuiRenderer.BindTexture(save.Value.Texture);
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.1f, 0.5f));
                    if (ImGui.Button($"{save.Value.Name}\n", bSize))
                    {
                        //save.Value.Load(MainWorld.Instance);
                    }
                    ImGui.SameLine();
                    var pos = ImGui.GetCursorPos();
                    pos.X -= 100;
                    pos.Y += (bSize.Y - 64) / 2;
                    ImGui.SetCursorPos(pos);
                    ImGui.Image(t_id, Vector2.One * 64);
                }
            }

            ImGui.PushFont(GlobalResources.Fonts["Default"]);
            ImGui.End();
        }
    }
}