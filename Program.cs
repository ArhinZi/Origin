﻿using System;

using Origin.Source;

namespace Origin
{
#if WINDOWS || LINUX

    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            using (var game = new OriginGame())
                game.Run();
        }
    }

#endif
}