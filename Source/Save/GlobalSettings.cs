using Microsoft.Xna.Framework.Input;
using Origin.Source.Resources;
using Origin.Source.Utils;
using System;
using System.Globalization;
using System.IO;

namespace Origin.Source.Save
{
    public class GlobalSettings
    {
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1200;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = false;
        public int FpsLimit { get; set; } = 0;

        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 0.8f;

        public Keys CameraUpKey { get; set; } = Keys.W;
        public Keys CameraDownKey { get; set; } = Keys.S;
        public Keys CameraLeftKey { get; set; } = Keys.A;
        public Keys CameraRightKey { get; set; } = Keys.D;
        public Keys RotateLeftKey { get; set; } = Keys.Q;
        public Keys RotateRightKey { get; set; } = Keys.E;

        public int NewSiteSizeX { get; set; } = 64;
        public int NewSiteSizeY { get; set; } = 64;
        public int NewSiteSizeZ { get; set; } = 128;
        public int NewSiteSeed { get; set; } = 1234;

        public float GenHeightScale { get; set; } = 10f;
        public float GenNoiseFrequency { get; set; } = 0.003f;
        public int GenNoiseOctaves { get; set; } = 8;
        public float GenNoiseGain { get; set; } = 0.3f;
        public float GenBaseHeightRatio { get; set; } = 0.7f;
        public int GenSoilDepth { get; set; } = 5;
        public int GenSmoothIterations { get; set; } = 1;
        public bool GenEnableRiver { get; set; } = true;
        public int GenRiverStrength { get; set; } = 5;
        public float GenRiverRadiusMultiplier { get; set; } = 5f;
        public float GenRiverErosionPower { get; set; } = 0.9f;
        public float GenRiverMinHeight { get; set; } = -5f;

        private static string SettingsPath => Path.Combine(Global.AppData, "Settings.ini");

        public static GlobalSettings Load()
        {
            Directory.CreateDirectory(Global.AppData);

            var settings = new GlobalSettings();
            if (!File.Exists(SettingsPath))
            {
                settings.Save();
                return settings;
            }

            IniFile ini = new(SettingsPath);

            if (int.TryParse(ini.Read("ResolutionWidth", "Graphics"), out int width) && width > 0)
                settings.ResolutionWidth = width;
            if (int.TryParse(ini.Read("ResolutionHeight", "Graphics"), out int height) && height > 0)
                settings.ResolutionHeight = height;
            if (bool.TryParse(ini.Read("Fullscreen", "Graphics"), out bool fullscreen))
                settings.Fullscreen = fullscreen;
            if (bool.TryParse(ini.Read("VSync", "Graphics"), out bool vsync))
                settings.VSync = vsync;
            if (int.TryParse(ini.Read("FpsLimit", "Graphics"), out int fps) && fps >= 0)
                settings.FpsLimit = fps;

            if (float.TryParse(ini.Read("MasterVolume", "Sound"), out float master))
                settings.MasterVolume = master;
            if (float.TryParse(ini.Read("MusicVolume", "Sound"), out float music))
                settings.MusicVolume = music;
            if (float.TryParse(ini.Read("SfxVolume", "Sound"), out float sfx))
                settings.SfxVolume = sfx;

            if (Enum.TryParse(ini.Read("CameraUp", "Controls"), true, out Keys cameraUp))
                settings.CameraUpKey = cameraUp;
            if (Enum.TryParse(ini.Read("CameraDown", "Controls"), true, out Keys cameraDown))
                settings.CameraDownKey = cameraDown;
            if (Enum.TryParse(ini.Read("CameraLeft", "Controls"), true, out Keys cameraLeft))
                settings.CameraLeftKey = cameraLeft;
            if (Enum.TryParse(ini.Read("CameraRight", "Controls"), true, out Keys cameraRight))
                settings.CameraRightKey = cameraRight;
            if (Enum.TryParse(ini.Read("RotateLeft", "Controls"), true, out Keys rotateLeft))
                settings.RotateLeftKey = rotateLeft;
            if (Enum.TryParse(ini.Read("RotateRight", "Controls"), true, out Keys rotateRight))
                settings.RotateRightKey = rotateRight;

            if (int.TryParse(ini.Read("NewSiteSizeX", "NewSite"), out int nx) && nx > 0)
                settings.NewSiteSizeX = nx;
            if (int.TryParse(ini.Read("NewSiteSizeY", "NewSite"), out int ny) && ny > 0)
                settings.NewSiteSizeY = ny;
            if (int.TryParse(ini.Read("NewSiteSizeZ", "NewSite"), out int nz) && nz > 0)
                settings.NewSiteSizeZ = nz;
            if (int.TryParse(ini.Read("NewSiteSeed", "NewSite"), out int ns))
                settings.NewSiteSeed = ns;

            if (float.TryParse(ini.Read("HeightScale", "Generation"), NumberStyles.Float, CultureInfo.InvariantCulture, out float gHeightScale))
                settings.GenHeightScale = gHeightScale;
            if (float.TryParse(ini.Read("NoiseFrequency", "Generation"), NumberStyles.Float, CultureInfo.InvariantCulture, out float gNoiseFreq))
                settings.GenNoiseFrequency = gNoiseFreq;
            if (int.TryParse(ini.Read("NoiseOctaves", "Generation"), out int gNoiseOctaves) && gNoiseOctaves > 0)
                settings.GenNoiseOctaves = gNoiseOctaves;
            if (float.TryParse(ini.Read("NoiseGain", "Generation"), NumberStyles.Float, CultureInfo.InvariantCulture, out float gNoiseGain))
                settings.GenNoiseGain = gNoiseGain;
            if (float.TryParse(ini.Read("BaseHeightRatio", "Generation"), NumberStyles.Float, CultureInfo.InvariantCulture, out float gBaseRatio))
                settings.GenBaseHeightRatio = gBaseRatio;
            if (int.TryParse(ini.Read("SoilDepth", "Generation"), out int gSoilDepth) && gSoilDepth > 0)
                settings.GenSoilDepth = gSoilDepth;
            if (int.TryParse(ini.Read("SmoothIterations", "Generation"), out int gSmoothIterations) && gSmoothIterations >= 0)
                settings.GenSmoothIterations = gSmoothIterations;
            if (bool.TryParse(ini.Read("EnableRiver", "Generation"), out bool gEnableRiver))
                settings.GenEnableRiver = gEnableRiver;
            if (int.TryParse(ini.Read("RiverStrength", "Generation"), out int gRiverStrength) && gRiverStrength > 0)
                settings.GenRiverStrength = gRiverStrength;
            if (float.TryParse(ini.Read("RiverRadiusMultiplier", "Generation"), NumberStyles.Float, CultureInfo.InvariantCulture, out float gRiverRadius))
                settings.GenRiverRadiusMultiplier = gRiverRadius;
            if (float.TryParse(ini.Read("RiverErosionPower", "Generation"), NumberStyles.Float, CultureInfo.InvariantCulture, out float gRiverErosionPower))
                settings.GenRiverErosionPower = gRiverErosionPower;
            if (float.TryParse(ini.Read("RiverMinHeight", "Generation"), NumberStyles.Float, CultureInfo.InvariantCulture, out float gRiverMinHeight))
                settings.GenRiverMinHeight = gRiverMinHeight;

            return settings;
        }

        public void Save()
        {
            Directory.CreateDirectory(Global.AppData);
            IniFile ini = new(SettingsPath);
            ini.Write("ResolutionWidth", ResolutionWidth.ToString(), "Graphics");
            ini.Write("ResolutionHeight", ResolutionHeight.ToString(), "Graphics");
            ini.Write("Fullscreen", Fullscreen.ToString(), "Graphics");
            ini.Write("VSync", VSync.ToString(), "Graphics");
            ini.Write("FpsLimit", FpsLimit.ToString(), "Graphics");

            ini.Write("MasterVolume", MasterVolume.ToString(CultureInfo.InvariantCulture), "Sound");
            ini.Write("MusicVolume", MusicVolume.ToString(CultureInfo.InvariantCulture), "Sound");
            ini.Write("SfxVolume", SfxVolume.ToString(CultureInfo.InvariantCulture), "Sound");

            ini.Write("CameraUp", CameraUpKey.ToString(), "Controls");
            ini.Write("CameraDown", CameraDownKey.ToString(), "Controls");
            ini.Write("CameraLeft", CameraLeftKey.ToString(), "Controls");
            ini.Write("CameraRight", CameraRightKey.ToString(), "Controls");
            ini.Write("RotateLeft", RotateLeftKey.ToString(), "Controls");
            ini.Write("RotateRight", RotateRightKey.ToString(), "Controls");

            ini.Write("NewSiteSizeX", NewSiteSizeX.ToString(), "NewSite");
            ini.Write("NewSiteSizeY", NewSiteSizeY.ToString(), "NewSite");
            ini.Write("NewSiteSizeZ", NewSiteSizeZ.ToString(), "NewSite");
            ini.Write("NewSiteSeed", NewSiteSeed.ToString(), "NewSite");

            ini.Write("HeightScale", GenHeightScale.ToString(CultureInfo.InvariantCulture), "Generation");
            ini.Write("NoiseFrequency", GenNoiseFrequency.ToString(CultureInfo.InvariantCulture), "Generation");
            ini.Write("NoiseOctaves", GenNoiseOctaves.ToString(), "Generation");
            ini.Write("NoiseGain", GenNoiseGain.ToString(CultureInfo.InvariantCulture), "Generation");
            ini.Write("BaseHeightRatio", GenBaseHeightRatio.ToString(CultureInfo.InvariantCulture), "Generation");
            ini.Write("SoilDepth", GenSoilDepth.ToString(), "Generation");
            ini.Write("SmoothIterations", GenSmoothIterations.ToString(), "Generation");
            ini.Write("EnableRiver", GenEnableRiver.ToString(), "Generation");
            ini.Write("RiverStrength", GenRiverStrength.ToString(), "Generation");
            ini.Write("RiverRadiusMultiplier", GenRiverRadiusMultiplier.ToString(CultureInfo.InvariantCulture), "Generation");
            ini.Write("RiverErosionPower", GenRiverErosionPower.ToString(CultureInfo.InvariantCulture), "Generation");
            ini.Write("RiverMinHeight", GenRiverMinHeight.ToString(CultureInfo.InvariantCulture), "Generation");
        }
    }
}
