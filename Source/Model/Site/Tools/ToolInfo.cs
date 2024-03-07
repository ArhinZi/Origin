using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Newtonsoft.Json.Linq;

using Origin.Source.Controller.IO;
using Origin.Source.Resources;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using Utf8Json;

namespace Origin.Source.Model.Site.Tools
{
    public class ToolInfo : Tool
    {
        private Point3 selected = Point3.Null;

        private SpritePositionColor template = new SpritePositionColor()
        {
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SelectionFloor"),
            offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
            color = Color.Red
        };

        public ToolInfo(SiteToolsComponent controller) :
            base(controller)
        {
            Name = "ToolInfo";
            sprites = new List<SpritePositionColor> { };
        }

        public override void Reset()
        {
            Active = false;
            DrawDirty = true;
            sprites.Clear();
            selected = Point3.Null;
        }

        public override void Update(GameTime gameTime)
        {
            var io = ImGui.GetIO();
            Point m = Mouse.GetState().Position;
            Position = MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site, true);
            if (!Active)
            {
                if (Position != Point3.Null)
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        Active = true;
                        DrawDirty = true;
                        selected = Position;
                    }
                }
            }
            else if (Active)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    sprites.Clear();
                    DrawDirty = true;
                    selected = Position;
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    Reset();
                }
            }

            if (selected != Point3.Null && (DrawDirty || !Active))
            {
                if (!DrawDirty)
                {
                    sprites.Clear();
                    DrawDirty = true;
                }
                sprites.Add(template);
                sprites[^1].position = selected;
                if (Active) sprites[^1].color = Color.Blue;
                else sprites[^1].color = Color.Red;
                for (int i = Math.Min(selected.Z + 1, Controller.Site.CurrentLevel); i <= Controller.Site.CurrentLevel; i++)
                {
                    sprites.Add(new SpritePositionColor()
                    {
                        sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SelectionWall"),
                        color = new Color(25, 25, 25, 200),
                        position = new Point3(selected.X, selected.Y, i)
                    });
                }
            }
            else if (!Active)
            {
                sprites.Clear();
                DrawDirty = true;
            }
        }

        private ComponentType selectedType;

        public override void Draw(GameTime gameTime)
        {
            if (!Active) return;

            if (Controller.Site.Map.TryGet(selected, out Entity ent))
            {
                var a = ent.GetArchetype();
                var types = a.Types;

                ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 440), ImGuiCond.FirstUseEver);
                if (ImGui.Begin($"Tile explorer {selected}###TileExplorer"))
                {
                    // Left
                    {
                        ImGui.BeginChild("left pane", new System.Numerics.Vector2(150, 0),
                            ImGuiChildFlags.Border | ImGuiChildFlags.ResizeX);
                        foreach (ref var type in types.AsSpan())
                        {
                            if (ImGui.Selectable(type.Type.Name, selectedType == type))
                                selectedType = type;
                        }
                        /*for (int i = 0; i < 100; i++)
                        {
                            // FIXME: Good candidate to use ImGuiSelectableFlags_SelectOnNav
                            char label[128];
                            sprintf(label, "MyObject %d", i);
                            if (ImGui::Selectable(label, selected == i))
                                selected = i;
                        }*/
                        ImGui.EndChild();
                    }
                    ImGui.SameLine();

                    // Right
                    {
                        ImGui.BeginGroup();
                        ImGui.BeginChild("item view", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing())); // Leave room for 1 line below us
                        ImGui.Text(selectedType.Type != null ? selectedType.Type.Name : "NONE");
                        ImGui.Separator();
                        if (ImGui.BeginTabBar("##Tabs", ImGuiTabBarFlags.None))
                        {
                            if (ImGui.BeginTabItem("Info"))
                            {
                                if (selectedType.Type != null && ent.TryGet(selectedType, out object obj))
                                {
                                    foreach (var f in selectedType.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                                    {
                                        ImGui.TextWrapped($"{f.Name}: {JsonSerializer.NonGeneric.ToJsonString(f.GetValue(obj))}");
                                    }
                                }
                            }
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                }
                ImGui.End();
            }
        }
    }
}