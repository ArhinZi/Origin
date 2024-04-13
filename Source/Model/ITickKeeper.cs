namespace Origin.Source.Model
{
    internal interface ITickKeeper
    {
        public void BeforeTick();

        public void AfterTick();

        public void TickTricky(int mult);
    }
}