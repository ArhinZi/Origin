﻿namespace Origin.Source.ECS
{
    public struct TileVisibility
    {
        public bool WallVisible = true;
        public bool WallDiscovered = true;

        public bool FloorVisible = true;
        public bool FloorDiscovered = true;

        public TileVisibility()
        { }
    }
}