namespace Origin.Source
{
    public class SiteCell
    {
        public string WallID;
        public string FloorID;
        public bool IsWallVisible { get; set; } = false;
        public bool IsFloorVisible { get; set; } = false;

        public SiteCell(string wmatid = null, string fmatid = null)
        {
            WallID = wmatid;
            FloorID = fmatid;
        }
    }
}