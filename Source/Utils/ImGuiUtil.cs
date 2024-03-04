﻿using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    public class ImGuiUtil
    {
        public static void AlignForWidth(float width, float alignment = 0.5f)
        {
            var style = ImGui.GetStyle();
            float avail = ImGui.GetContentRegionAvail().X;
            float off = (avail - width) * alignment;
            if (off > 0.0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + off);
        }
    }
}