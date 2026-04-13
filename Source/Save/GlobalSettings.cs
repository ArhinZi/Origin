using Microsoft.Xna.Framework.Input;
using Origin.Source.Resources;
using Origin.Source.Utils;
using System;
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

            ini.Write("MasterVolume", MasterVolume.ToString(System.Globalization.CultureInfo.InvariantCulture), "Sound");
            ini.Write("MusicVolume", MusicVolume.ToString(System.Globalization.CultureInfo.InvariantCulture), "Sound");
            ini.Write("SfxVolume", SfxVolume.ToString(System.Globalization.CultureInfo.InvariantCulture), "Sound");

            ini.Write("CameraUp", CameraUpKey.ToString(), "Controls");
            ini.Write("CameraDown", CameraDownKey.ToString(), "Controls");
            ini.Write("CameraLeft", CameraLeftKey.ToString(), "Controls");
            ini.Write("CameraRight", CameraRightKey.ToString(), "Controls");
            ini.Write("RotateLeft", RotateLeftKey.ToString(), "Controls");
            ini.Write("RotateRight", RotateRightKey.ToString(), "Controls");
        }
    }
}
