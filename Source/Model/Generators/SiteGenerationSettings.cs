namespace Origin.Source.Model.Generators
{
    public class SiteGenerationSettings
    {
        public float HeightScale { get; set; } = 10f;
        public float NoiseFrequency { get; set; } = 0.003f;
        public int NoiseOctaves { get; set; } = 8;
        public float NoiseGain { get; set; } = 0.3f;

        public float BaseHeightRatio { get; set; } = 0.7f;
        public int SoilDepth { get; set; } = 5;
        public int SmoothIterations { get; set; } = 1;

        public bool EnableRiver { get; set; } = true;
        public int RiverStrength { get; set; } = 5;
        public float RiverRadiusMultiplier { get; set; } = 5f;
        public float RiverErosionPower { get; set; } = 0.9f;
        public float RiverMinHeight { get; set; } = -5f;
    }
}
