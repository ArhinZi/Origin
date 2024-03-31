using Microsoft.Xna.Framework;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.Resources;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model
{
    public class WorldTickManager : IUpdate
    {
        public static readonly ulong MINUTE = 10;
        public static readonly ulong HOUR = 600;
        public static readonly ulong DAY = 14400;
        public static readonly ulong WEEK = 100800;
        public static readonly ulong MONTH = 403200;
        public static readonly ulong SEASON = 1209600;
        public static readonly ulong YEAR = 4838400;

        public TimeOnly SunRiseTime { get; private set; } = new TimeOnly(6, 0);
        public TimeOnly SunSetTime { get; private set; } = new TimeOnly(17, 0);

        public ulong Ticks { get; set; } = DAY / 4;

        public float TimeMod { get; private set; } = 1f;
        public float PrePauseTimeMod { get; private set; } = 1f;

        public ulong DayTick => Ticks % DAY;

        public TimeOnly DayTime => new(
            hour: (int)(DayTick / HOUR),
            minute: (int)((DayTick % HOUR) / MINUTE),
            second: (int)(DayTick % MINUTE)
            );

        public ulong Day => (Ticks / DAY) % 28;
        public ulong Month => (Ticks / MONTH) % 12;
        public ulong Season => (Ticks / SEASON) % 4;
        public ulong Year => Ticks / YEAR;

        private float _htick = 0;

        public SystemGroupsManager SystemsManager;
        private World World;

        public bool Pause { get; private set; }

        public WorldTickManager(World world)
        {
            SystemsManager = new SystemGroupsManager(this);
            World = world;
        }

        public void Update(GameTime gameTime)
        {
            _htick += TimeMod;
            var counter = 0;
            while (_htick > 1)
            {
                BeforeTickSimple();
                SystemsManager.Tick(gameTime);
                AfterTickSimple();
                _htick--;
                Ticks++;
                counter++;
                //do smth
            }
            TickTricky(counter);
        }

        private void BeforeTickSimple()
        {
            foreach (var item in World.Sites)
            {
                item.BeforeTick();
            }
        }

        private void AfterTickSimple()
        {
            foreach (var item in World.Sites)
            {
                item.AfterTick();
            }
        }

        private void TickTricky(int mult)
        {
            foreach (var item in World.Sites)
            {
                item.TickTricky(mult);
            }
        }

        public float GetSunLightIntensity()
        {
            const int transitionDurationMinutes = 60; // Duration of sunrise/sunset transition in minutes

            // Convert times to total minutes from midnight
            int totalMinutes = DayTime.Hour * 60 + DayTime.Minute;
            int sunriseMinutes = SunRiseTime.Hour * 60 + SunRiseTime.Minute;
            int sunsetMinutes = SunSetTime.Hour * 60 + SunSetTime.Minute;

            // Calculate the difference in minutes between the current time and sunrise/sunset
            int sunriseDiff = totalMinutes - sunriseMinutes;
            int sunsetDiff = sunsetMinutes - totalMinutes;

            // Check if it's before sunrise or after sunset
            if (sunriseDiff < 0 || sunsetDiff < 0)
            {
                return 0f; // It's night, so SunLightIntensity is 0
            }

            // Check if it's during the sunrise transition
            if (sunriseDiff <= transitionDurationMinutes)
            {
                return sunriseDiff / (float)transitionDurationMinutes; // SunLightIntensity increases
            }

            // Check if it's during the sunset transition
            if (sunsetDiff <= transitionDurationMinutes)
            {
                return sunsetDiff / (float)transitionDurationMinutes; // SunLightIntensity decreases
            }

            return 1f; // It's daytime, so SunLightIntensity is 1
        }

        public bool TogglePause()
        {
            Pause = !Pause;
            if (Pause)
            {
                PrePauseTimeMod = TimeMod;
                TimeMod = 0;
            }
            else
            {
                TimeMod = PrePauseTimeMod;
            }
            return Pause;
        }
    }
}