using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;

using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Origin.Source.Model.Map.Light;
using Origin.Source.Model.NewWorld;
using Origin.Source.Model.NewWorld.Systems.Vegetation;
using Origin.Source.Resources;
using System;
using System.Collections.Generic;

namespace Origin.Source.Model.Map.Tools
{
    public class ToolInfo : Tool
    {
        private Point3 selected = Point3.Null;

        private SpritePositionColor template = new()
        {
            sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "SelectionFloor"),
            offset = new Point(0, -GlobalResources.Settings.FloorYoffset),
            color = Color.Red
        };

        public ToolInfo(SiteToolsComponent controller) :
            base(controller)
        {
            Name = "ToolInfo";
            sprites = [];
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
            Point m = Mouse.GetState().Position;
            Position = MouseScreenToMapSurface(Camera, m, Controller.Site.CurrentLevel, Controller.Site, true);
            if (!Active)
            {
                if (Position != Point3.Null && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    Active = true;
                    DrawDirty = true;
                    selected = Position;
                }
            }
            else
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
        }

        private int selectedOther = 0;

        private static void DrawBool(string name, bool value)
        {
            ImGui.TextWrapped($"{name}: {(value ? "true" : "false")}");
        }

        private static void DrawText(string name, object value)
        {
            ImGui.TextWrapped($"{name}: {value}");
        }

        private static void DrawConstruction(string title, TileConstruction construction)
        {
            DrawText(title, construction.ConstructionID);
            DrawText("Material", construction.MaterialID);
            DrawText("ConstructionMetaID", construction.ConstructionMetaID);
            DrawText("MaterialMetaID", construction.MaterialMetaID);
            DrawText("ConstructionType", construction.Construction.Type);
            DrawText("ConstructionCategory", construction.Construction.Category);
        }

        private static void DrawConstructionOver(string title, TileConstructionOver construction)
        {
            DrawText(title, construction.Construction.ID);
            DrawText("Material", construction.Material.ID);
            DrawText("ConstructionMetaID", construction.ConstructionMetaID);
            DrawText("MaterialMetaID", construction.MaterialMetaID);
        }

        private static void DrawFluid(TileFluid fluid)
        {
            DrawText("Type", fluid.Type);
            DrawText("Volume", fluid.Volume);
            DrawText("Direction", fluid.Direction);
        }

        private static void DrawVegetation(Site site, Tile tile)
        {
            // Читаємо стан рослинності напряму з ECS-ентіті.
            if (!VegUtilities.TryGetVegetationState(site, tile, out var vegetation, out var growthLevel, out var neighbours))
            {
                ImGui.TextWrapped("Vegetation entity is missing or invalid.");
                return;
            }

            DrawText("Vegetation", vegetation.ID);
            DrawText("VegetationMetaID", GlobalResources.Vegetations.IndexOf(vegetation.ID));
            DrawText("VegetationNeighbours", neighbours);
            DrawText("GrowthLevel", growthLevel);
            DrawBool("IsGrown", growthLevel >= 8);
            DrawText("Entity", tile.VegetationEntity);
        }

        private static void DrawTile(Site site, Tile tile)
        {
            DrawBool("Exists", tile.Exists);
            DrawBool("HasConstruction", tile.HasConstruction);
            if (tile.HasConstruction)
            {
                ImGui.SeparatorText("Construction");
                DrawConstruction("Construction", tile.Construction);
            }

            DrawBool("HasConstructionOver", tile.HasConstructionOver);
            if (tile.HasConstructionOver)
            {
                ImGui.SeparatorText("ConstructionOver");
                DrawConstructionOver("Over", tile.ConstructionOver);
            }

            DrawBool("IsRamp", tile.IsRamp);
            DrawBool("HasConstructionShape", tile.HasConstructionShape);
            if (tile.HasConstructionShape)
                DrawText("ConstructionShape", tile.ConstructionShape.Name);

            DrawBool("HasConstructionRotation", tile.HasConstructionRotation);
            if (tile.HasConstructionRotation)
                DrawText("ConstructionRotation", tile.ConstructionRotation.Direction);

            DrawBool("HasFluid", tile.HasFluid);
            if (tile.HasFluid)
            {
                ImGui.SeparatorText("Fluid");
                DrawFluid(tile.Fluid);
                DrawBool("IsFluidStatic", tile.IsFluidStatic);
                DrawBool("IsFluidBlocker", tile.IsFluidBlocker);
            }
            else
            {
                DrawBool("IsFluidStatic", tile.IsFluidStatic);
                DrawBool("IsFluidBlocker", tile.IsFluidBlocker);
            }

            DrawBool("HasVegetation", tile.HasVegetation);
            if (tile.HasVegetation)
            {
                ImGui.SeparatorText("Vegetation");
                DrawVegetation(site, tile);
            }

            DrawBool("IsWalkable", tile.IsWalkable);
            DrawText("WalkableConstructionBelowMetaID", tile.WalkableConstructionBelowMetaID);
            DrawBool("IsAir", tile.IsAir);
        }

        private static void DrawPackedLight(PackedLight light)
        {
            DrawText("SunLighted", light.SunLighted);
            DrawText("LightLevel", light.LightLevel);
            DrawBool("HasMultipleLightSources", light.HasMultipleLightSources);
            DrawBool("IsLightBlocker", light.IsLightBlocker);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Active) return;

            if (Controller.Site.Map.TryGet(selected, out Tile tile) && tile.Exists)
            {
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 440), ImGuiCond.FirstUseEver);
                if (ImGui.Begin($"Tile explorer {selected}###TileExplorer"))
                {
                    ImGui.BeginChild("left pane", new System.Numerics.Vector2(150, 0),
                        ImGuiChildFlags.Border | ImGuiChildFlags.ResizeX);
                    if (ImGui.Selectable("Tile", selectedOther == 0))
                        selectedOther = 0;
                    if (ImGui.Selectable("Light", selectedOther == 1))
                        selectedOther = 1;
                    if (ImGui.Selectable("Vegetation", selectedOther == 2))
                        selectedOther = 2;
                    ImGui.EndChild();

                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    ImGui.BeginChild("item view", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()));
                    ImGui.Text(selectedOther == 0 ? "Tile" : selectedOther == 1 ? "Light" : "Vegetation");
                    ImGui.Separator();
                    if (ImGui.BeginTabBar("##Tabs", ImGuiTabBarFlags.None))
                    {
                        if (ImGui.BeginTabItem("Info"))
                        {
                            if (selectedOther == 0)
                            {
                                DrawTile(Controller.Site, tile);
                            }
                            else if (selectedOther == 1)
                            {
                                if (Controller.Site.LightControl.TryGetTile(selected, out PackedLight pl))
                                    DrawPackedLight(pl);
                            }
                            else
                            {
                                if (tile.HasVegetation)
                                    DrawVegetation(Controller.Site, tile);
                                else
                                    ImGui.TextWrapped("No vegetation on this tile.");
                            }
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }

                    if (ImGui.Button("Up"))
                    {
                        selected += new Utils.Point3(0, 0, 1);
                        sprites.Clear();
                        DrawDirty = true;
                    }
                    if (ImGui.Button("Down"))
                    {
                        selected -= new Utils.Point3(0, 0, 1);
                        sprites.Clear();
                        DrawDirty = true;
                    }
                    ImGui.EndChild();
                    ImGui.EndGroup();
                }
                ImGui.End();
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
                sprites[^1].color = Active ? Color.Blue : Color.Red;
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
    }
}