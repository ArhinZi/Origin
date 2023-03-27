namespace Origin.Source
{
    public class SiteCell
    {
        public string WallID;
        public string FloorID;
        public bool IsVisible { get; set; }

        public SiteCell(string wmatid = null, string fmatid = null)
        {
            WallID = wmatid;
            FloorID = fmatid;
            IsVisible = false;
        }
    }
}