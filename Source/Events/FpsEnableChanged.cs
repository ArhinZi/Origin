namespace Origin.Source.Events
{
    public class FpsEnableChanged
    {
        public bool IsEnabled;

        public FpsEnableChanged(bool value)
        {
            IsEnabled = value;
        }
    }
}