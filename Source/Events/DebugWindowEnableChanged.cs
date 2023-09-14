namespace Origin.Source.Events
{
    public class DebugWindowEnableChanged
    {
        public bool IsEnabled;

        public DebugWindowEnableChanged(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }
}