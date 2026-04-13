namespace Origin.Source.Model.NewWorld
{
    internal interface ITickKeeper
    {
        public void BeforeTick();

        public void AfterTick();

        public void TickTricky(int mult);
    }
}