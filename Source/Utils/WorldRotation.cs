namespace Origin.Source.Utils
{
    /// <summary>
    /// Screen-facing world rotation states for the isometric map.
    /// The two letters describe which map corner appears at the top of the screen.
    /// </summary>
    public enum WorldRotation
    {
        /// <summary>
        /// Top-right corner is at the top of the screen. Default orientation.
        /// </summary>
        TR,

        /// <summary>
        /// Top-left corner is at the top of the screen.
        /// </summary>
        TL,

        /// <summary>
        /// Bottom-left corner is at the top of the screen.
        /// </summary>
        BL,

        /// <summary>
        /// Bottom-right corner is at the top of the screen.
        /// </summary>
        BR
    }
}