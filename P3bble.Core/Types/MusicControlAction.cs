namespace P3bble.Core.Types
{
    /// <summary>
    /// The music control action
    /// </summary>
    public enum MusicControlAction : byte
    {
        /// <summary>
        /// Unknown state
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Unkown state
        /// </summary>
        Unkown2 = 8,

        /// <summary>
        /// Play or Pause
        /// </summary>
        PlayPause = 1,

        /// <summary>
        /// Skip next
        /// </summary>
        Next = 4,

        /// <summary>
        /// Skip previous
        /// </summary>
        Previous = 5 ,

        /// <summary>
        /// Volume Up
        /// </summary>
        VolUp = 6,

        /// <summary>
        /// Volume Down
        /// </summary>
        VolDown = 7
    }
}
