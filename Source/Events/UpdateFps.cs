namespace Origin.Source.Events
{
    public class UpdateFps
    {
        public float fps;
        public float min;
        public float max;

        public UpdateFps(float fps, float min, float max)
        {
            this.fps = fps;
            this.min = min;
            this.max = max;
        }
    }
}