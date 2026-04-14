using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Origin.Source.Resources
{
    public sealed class ResourceLoadReport
    {
        public List<string> MergeWarnings { get; } = [];
        public List<string> ValidationErrors { get; } = [];
        public List<string> ParseErrors { get; } = [];

        public bool HasMessages => MergeWarnings.Count > 0 || ValidationErrors.Count > 0 || ParseErrors.Count > 0;
    }

    public static class ResourceLoader
    {
        private static readonly HashSet<string> RequiredArraySections =
        [
            "Sprites",
            "RotationSprites",
            "Materials",
            "Items",
            "Constructions",
            "Vegetations",
            "Textures",
            "HalfWallDefs"
        ];

        private static readonly HashSet<string> RequiredObjectSections =
        [
            "Settings"
        ];

        public static ResourceLoadReport LastReport { get; private set; } = new();

        public static void LoadResources()
        {
            LastReport = new ResourceLoadReport();
            GlobalResources.Init();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            JObject mergedJson = [];

            foreach (var modeFolder in Directory.GetDirectories(Path.Combine(path, "Data\\")))
            {
                foreach (var file in GetAllFiles(modeFolder))
                {
                    if (file.Split(".").Last().Equals("png", StringComparison.OrdinalIgnoreCase))
                    {
                        GlobalResources.LoadTexture(file);
                    }
                }

                var jsonCandidates = GetAllFiles(modeFolder)
                    .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .Where(f => !f.Contains("\\Schemas\\", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                List<(string File, JObject Json)> parsed = [];
                foreach (var file in jsonCandidates)
                {
                    try
                    {
                        using StreamReader reader = new(file);
                        var json = reader.ReadToEnd();
                        JObject source = JObject.Parse(json);
                        parsed.Add((file, source));
                    }
                    catch (Exception ex)
                    {
                        LastReport.ParseErrors.Add($"{Path.GetRelativePath(path, file)}: {ex.Message}");
                    }
                }

                foreach (var entry in parsed
                    .OrderBy(e => GetContentOrder(e.Json))
                    .ThenBy(e => e.File, StringComparer.OrdinalIgnoreCase))
                {
                    SafeMerge(mergedJson, entry.Json, Path.GetRelativePath(path, entry.File));
                }
            }

            ValidateMergedData(mergedJson, LastReport.ValidationErrors);
            EnsureMinimumRuntimeDefaults(mergedJson, LastReport.ValidationErrors);

            try
            {
                GlobalResources.ReadFromJson(mergedJson);
            }
            catch (Exception ex)
            {
                LastReport.ValidationErrors.Add($"Resource materialization failed: {ex.Message}");
            }
        }

        private static int GetContentOrder(JObject source)
        {
            int order = 100;
            foreach (var prop in source.Properties())
            {
                order = Math.Min(order, prop.Name switch
                {
                    "Textures" => 0,
                    "Sprites" => 1,
                    "RotationSprites" => 1,
                    "Materials" => 2,
                    "Items" => 3,
                    "Constructions" => 4,
                    "Vegetations" => 5,
                    "HalfWallDefs" => 6,
                    "Settings" => 7,
                    _ => 50
                });
            }
            return order;
        }

        private static void SafeMerge(JObject target, JObject source, string sourceName)
        {
            foreach (var prop in source.Properties())
            {
                if (prop.Name.StartsWith("$", StringComparison.Ordinal))
                    continue;

                if (!target.TryGetValue(prop.Name, out JToken existing))
                {
                    target[prop.Name] = prop.Value.DeepClone();
                    continue;
                }

                if (existing.Type == JTokenType.Array && prop.Value.Type == JTokenType.Array)
                {
                    var targetArray = (JArray)existing;
                    foreach (var item in (JArray)prop.Value)
                        targetArray.Add(item.DeepClone());
                    continue;
                }

                if (existing.Type == JTokenType.Object && prop.Value.Type == JTokenType.Object)
                {
                    MergeObjectsSafe((JObject)existing, (JObject)prop.Value, prop.Name, sourceName);
                    continue;
                }

                if (!JToken.DeepEquals(existing, prop.Value))
                {
                    LastReport.MergeWarnings.Add($"Root overwrite from '{sourceName}': '{prop.Name}'.");
                    target[prop.Name] = prop.Value.DeepClone();
                }
            }
        }

        private static void MergeObjectsSafe(JObject target, JObject source, string path, string sourceName)
        {
            foreach (var prop in source.Properties())
            {
                if (!target.TryGetValue(prop.Name, out JToken existing))
                {
                    target[prop.Name] = prop.Value.DeepClone();
                    continue;
                }

                if (existing.Type == JTokenType.Array && prop.Value.Type == JTokenType.Array)
                {
                    var targetArray = (JArray)existing;
                    foreach (var item in (JArray)prop.Value)
                        targetArray.Add(item.DeepClone());
                    continue;
                }

                if (existing.Type == JTokenType.Object && prop.Value.Type == JTokenType.Object)
                {
                    MergeObjectsSafe((JObject)existing, (JObject)prop.Value, $"{path}.{prop.Name}", sourceName);
                    continue;
                }

                if (!JToken.DeepEquals(existing, prop.Value))
                {
                    LastReport.MergeWarnings.Add($"Overwrite from '{sourceName}': '{path}.{prop.Name}'.");
                    target[prop.Name] = prop.Value.DeepClone();
                }
            }
        }

        private static void EnsureMinimumRuntimeDefaults(JObject data, List<string> errors)
        {
            foreach (var required in RequiredArraySections)
            {
                if (data[required] is not JArray)
                {
                    data[required] = new JArray();
                    errors.Add($"Section '{required}' was missing and has been created as empty array.");
                }
            }

            if (data["Settings"] is not JObject settings)
            {
                settings = new JObject();
                data["Settings"] = settings;
                errors.Add("Section 'Settings' was missing and has been created with fallback values.");
            }

            settings["DefaultItemSprite"] ??= "SelectionWall";
            settings["HiddenWallSprite"] ??= "SolidSelectionWall";
            settings["HiddenFloorSprite"] ??= "SolidSelectionFloor";
            settings["HiddenColor"] ??= "70 70 70 255";
            settings["TileSize"] ??= "64 32";
            settings["SpriteSize"] ??= "64 64";
            settings["FloorYoffset"] ??= 7;
        }

        private static void ValidateMergedData(JObject data, List<string> errors)
        {
            foreach (var required in RequiredArraySections)
            {
                if (data[required] is not JArray)
                    errors.Add($"Missing required array section '{required}'.");
            }

            foreach (var required in RequiredObjectSections)
            {
                if (data[required] is not JObject)
                    errors.Add($"Missing required object section '{required}'.");
            }

            ValidateUniqueIds(data, "Sprites", errors);
            ValidateUniqueIds(data, "RotationSprites", errors);
            ValidateUniqueIds(data, "Materials", errors);
            ValidateUniqueIds(data, "Items", errors);
            ValidateUniqueIds(data, "Constructions", errors);
            ValidateUniqueIds(data, "Vegetations", errors);
            ValidateUniqueIds(data, "Textures", errors);

            HashSet<string> textureIds = GetIds(data, "Textures");
            foreach (var loaded in GlobalResources.Textures)
            {
                if (!string.IsNullOrWhiteSpace(loaded?.Name))
                    textureIds.Add(loaded.Name);
            }

            HashSet<string> spriteIds = GetIds(data, "Sprites");
            foreach (var id in GetIds(data, "RotationSprites"))
                spriteIds.Add(id);
            HashSet<string> constructionIds = GetIds(data, "Constructions");
            HashSet<string> itemIds = GetIds(data, "Items");

            if (data["Sprites"] is JArray sprites)
            {
                foreach (var token in sprites.OfType<JObject>())
                {
                    string spriteId = token["ID"]?.ToString() ?? "<unknown>";
                    string textureName = token["TextureName"]?.ToString();
                    if (string.IsNullOrWhiteSpace(textureName))
                        errors.Add($"Sprite '{spriteId}' missing TextureName.");
                    else if (!textureIds.Contains(textureName))
                        errors.Add($"Sprite '{spriteId}' references missing texture '{textureName}'.");
                }
            }

            if (data["RotationSprites"] is JArray rotationSprites)
            {
                foreach (var token in rotationSprites.OfType<JObject>())
                {
                    string rotationId = token["ID"]?.ToString() ?? "<unknown>";
                    var rots = token["Rotations"] as JObject;
                    if (rots == null)
                        continue;

                    foreach (var dir in rots.Properties())
                    {
                        if (dir.Value is not JArray refs)
                            continue;
                        foreach (var refToken in refs)
                        {
                            string refId = refToken?.ToString();
                            if (!string.IsNullOrWhiteSpace(refId) && !spriteIds.Contains(refId))
                                errors.Add($"RotationSprite '{rotationId}' references missing sprite '{refId}'.");
                        }
                    }
                }
            }

            if (data["Constructions"] is JArray constructions)
            {
                foreach (var token in constructions.OfType<JObject>())
                {
                    string id = token["ID"]?.ToString() ?? "<unknown>";
                    string removedId = token["WallRemovedConstruction"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(removedId) && !constructionIds.Contains(removedId))
                        errors.Add($"Construction '{id}' has unknown WallRemovedConstruction '{removedId}'.");

                    ValidateSpriteReferences(token["Sprites"], spriteIds, $"Construction '{id}'", errors);

                    if (token["Shapes"] is JObject shapes)
                    {
                        foreach (var shape in shapes.Properties())
                            ValidateSpriteReferences(shape.Value?["Sprites"], spriteIds, $"Construction '{id}' shape '{shape.Name}'", errors);
                    }

                    if (token["Components"] is JArray components)
                    {
                        foreach (var c in components.OfType<JObject>())
                        {
                            string itemId = c["ItemID"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(itemId) && !itemIds.Contains(itemId))
                                errors.Add($"Construction '{id}' component references missing item '{itemId}'.");
                        }
                    }
                }
            }

            if (data["Vegetations"] is JArray vegetations)
            {
                foreach (var token in vegetations.OfType<JObject>())
                {
                    string id = token["ID"]?.ToString() ?? "<unknown>";
                    if (token["Drawing"] is not JArray drawings)
                    {
                        errors.Add($"Vegetation '{id}' missing Drawing array.");
                        continue;
                    }

                    foreach (var draw in drawings.OfType<JObject>())
                    {
                        if (draw["OnConstructions"] is JArray constrRefs)
                        {
                            foreach (var c in constrRefs)
                            {
                                string constr = c?.ToString();
                                if (!string.IsNullOrWhiteSpace(constr) && !constructionIds.Contains(constr))
                                    errors.Add($"Vegetation '{id}' references unknown construction '{constr}'.");
                            }
                        }

                        if (draw["Sprites"] is JArray spritesRef)
                        {
                            foreach (var s in spritesRef)
                            {
                                string sprite = s?.ToString();
                                if (!string.IsNullOrWhiteSpace(sprite) && !spriteIds.Contains(sprite))
                                    errors.Add($"Vegetation '{id}' references missing sprite '{sprite}'.");
                            }
                        }

                        if (draw["Shapes"] is JObject shapes)
                        {
                            foreach (var shape in shapes.Properties())
                            {
                                if (shape.Value?["Sprites"] is JArray shapeSprites)
                                {
                                    foreach (var s in shapeSprites)
                                    {
                                        string sprite = s?.ToString();
                                        if (!string.IsNullOrWhiteSpace(sprite) && !spriteIds.Contains(sprite))
                                            errors.Add($"Vegetation '{id}' shape '{shape.Name}' references missing sprite '{sprite}'.");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (data["Settings"] is JObject settings)
            {
                string hiddenWall = settings["HiddenWallSprite"]?.ToString();
                string hiddenFloor = settings["HiddenFloorSprite"]?.ToString();
                if (!string.IsNullOrWhiteSpace(hiddenWall) && !spriteIds.Contains(hiddenWall))
                    errors.Add($"Settings.HiddenWallSprite references missing sprite '{hiddenWall}'.");
                if (!string.IsNullOrWhiteSpace(hiddenFloor) && !spriteIds.Contains(hiddenFloor))
                    errors.Add($"Settings.HiddenFloorSprite references missing sprite '{hiddenFloor}'.");
            }
        }

        private static void ValidateSpriteReferences(JToken token, HashSet<string> spriteIds, string context, List<string> errors)
        {
            if (token is not JObject obj)
                return;

            foreach (var prop in obj.Properties())
            {
                if (prop.Value is not JArray refs)
                    continue;

                foreach (var r in refs)
                {
                    string id = r?.ToString();
                    if (!string.IsNullOrWhiteSpace(id) && !spriteIds.Contains(id))
                        errors.Add($"{context} references missing sprite '{id}'.");
                }
            }
        }

        private static void ValidateUniqueIds(JObject data, string section, List<string> errors)
        {
            if (data[section] is not JArray arr)
                return;

            HashSet<string> ids = [];
            foreach (var token in arr.OfType<JObject>())
            {
                string id = token["ID"]?.ToString();
                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add($"Section '{section}' has item without ID.");
                    continue;
                }

                if (!ids.Add(id))
                    errors.Add($"Section '{section}' contains duplicate ID '{id}'.");
            }
        }

        private static HashSet<string> GetIds(JObject data, string section)
        {
            HashSet<string> result = [];
            if (data[section] is not JArray arr)
                return result;

            foreach (var token in arr.OfType<JObject>())
            {
                string id = token["ID"]?.ToString();
                if (!string.IsNullOrWhiteSpace(id))
                    result.Add(id);
            }
            return result;
        }

        private static IEnumerable<string> GetAllFiles(string rootDirectory)
        {
            Queue<string> pending = new();
            pending.Enqueue(rootDirectory);

            while (pending.Count > 0)
            {
                string currentDirectory = pending.Dequeue();
                foreach (string file in Directory.GetFiles(currentDirectory))
                {
                    yield return file;
                }
                foreach (string subdir in Directory.GetDirectories(currentDirectory))
                {
                    pending.Enqueue(subdir);
                }
            }
        }
    }
}