using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Origin.Source.Model.NewWorld.Systems;
using System;

namespace Origin.Source.Model.NewWorld
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

        private const int SunTransitionDurationMinutes = 240;
        // Базовий TPS для режиму x1 (сумісність зі старим TimeScale).
        private const float BaseTicksPerSecond = 60f;
        // Інтервал оновлення метрики фактичного TPS.
        private const float TpsSampleWindowSeconds = 1f;

        public TimeOnly SunRiseTime { get; private set; } = new TimeOnly(6, 0);
        public TimeOnly SunSetTime { get; private set; } = new TimeOnly(19, 0);

        public ulong Ticks { get; set; } = DAY / 4;

        // Цільовий TPS (коли не на паузі).
        public float TicksPerSecond { get; private set; } = BaseTicksPerSecond;
        // Збережений TPS для відновлення після паузи.
        public float PrePauseTicksPerSecond { get; private set; } = BaseTicksPerSecond;
        // Активний TPS для UI/дебагу (на паузі показує збережене значення).
        public float ActiveTPS => Pause ? PrePauseTicksPerSecond : TicksPerSecond;
        // Фактичний TPS, виміряний за останнє вікно часу.
        public float CurrentTPS { get; private set; }

        // Зворотна сумісність зі старим API швидкості.
        public float TimeMod => ActiveTimeScale;
        public float PrePauseTimeMod => PrePauseTicksPerSecond / BaseTicksPerSecond;
        public float ActiveTimeScale => ActiveTPS / BaseTicksPerSecond;

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

        // Акумулятор дробових тіків (для стабільного TPS поверх elapsed time).
        private float _htick = 0;
        // Акумулятори для розрахунку фактичного TPS.
        private float _tpsElapsed;
        private int _tpsTicks;

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
            // На паузі не виконуємо системні тіки, але лишаємо TickTricky(0).
            if (Pause)
            {
                CurrentTPS = 0f;
                TickTricky(0);
                return;
            }

            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _htick += TicksPerSecond * elapsedSeconds;

            int counter = 0;
            while (_htick >= 1f)
            {
                BeforeTickSimple();
                SystemsManager.Tick(gameTime);
                AfterTickSimple();
                _htick -= 1f;
                Ticks++;
                counter++;
            }

            // Оновлюємо фактичний TPS у ковзному часовому вікні.
            _tpsElapsed += elapsedSeconds;
            _tpsTicks += counter;
            if (_tpsElapsed >= TpsSampleWindowSeconds)
            {
                CurrentTPS = _tpsTicks / _tpsElapsed;
                _tpsElapsed = 0f;
                _tpsTicks = 0;
            }

            TickTricky(counter);
        }

        public void Draw(GameTime gameTime)
        {
            SystemsManager.Draw(gameTime);
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
            int totalMinutes = DayTime.Hour * 60 + DayTime.Minute;
            int sunriseMinutes = SunRiseTime.Hour * 60 + SunRiseTime.Minute;
            int sunsetMinutes = SunSetTime.Hour * 60 + SunSetTime.Minute;

            int sunriseDiff = totalMinutes - sunriseMinutes;
            int sunsetDiff = sunsetMinutes - totalMinutes;

            if (sunriseDiff < 0 || sunsetDiff < 0)
            {
                return 0f;
            }

            if (sunriseDiff <= SunTransitionDurationMinutes)
            {
                return sunriseDiff / (float)SunTransitionDurationMinutes;
            }

            if (sunsetDiff <= SunTransitionDurationMinutes)
            {
                return sunsetDiff / (float)SunTransitionDurationMinutes;
            }

            return 1f;
        }

        // Новий керуючий API: задає швидкість гри у тіках за секунду.
        public void SetTicksPerSecond(float tps)
        {
            if (tps <= 0)
                tps = BaseTicksPerSecond;

            if (Pause)
            {
                PrePauseTicksPerSecond = tps;
            }
            else
            {
                TicksPerSecond = tps;
                PrePauseTicksPerSecond = tps;
            }
        }

        // Зворотна сумісність: scale x1 відповідає BaseTicksPerSecond.
        public void SetTimeScale(float scale)
        {
            if (scale <= 0)
                scale = 1f;

            SetTicksPerSecond(BaseTicksPerSecond * scale);
        }

        public bool TogglePause()
        {
            Pause = !Pause;
            if (Pause)
            {
                PrePauseTicksPerSecond = TicksPerSecond;
                TicksPerSecond = 0f;
            }
            else
            {
                TicksPerSecond = PrePauseTicksPerSecond;
            }
            return Pause;
        }
    }
}