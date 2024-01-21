﻿using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using Origin.Source.Resources.Converters;

namespace Origin.Source.Resources
{
    public class Settings
    {
        public string DefaultItemSprite { get; set; }
        public string HiddenWallSprite { get; set; }
        public string HiddenFloorSprite { get; set; }
        public string HiddenColor { get; set; }

        [JsonConverter(typeof(PointConverter))]
        public Point TileSize { get; set; }

        [JsonConverter(typeof(PointConverter))]
        public Point SpriteSize { get; set; }

        public int FloorYoffset { get; set; }
    }
}