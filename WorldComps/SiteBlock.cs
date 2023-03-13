namespace Origin.WorldComps
{
    public enum SiteBlockParams : byte
    {
        IsVisible,
        IsSelected,
        IsDiscovered
    }

    public struct SiteBlock
    {
        public ushort wallId;
        public ushort floorId;
        public bool isSelected;
        public bool isVisible;
        public bool isDiscovered;

        public SiteBlock(ushort wallid = 0, ushort floorid = 0)
        {
            this.wallId = wallid;
            this.floorId = floorid;
            this.isSelected = false;
            this.isVisible = false;
            this.isDiscovered = false;
        }
    }
}