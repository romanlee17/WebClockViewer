using System;

namespace WebClockViewer
{
    /// <summary>Which clock a time of day is read on or entered for.</summary>
    internal enum ClockZone
    {
        Utc,
        Local,
    }

    /// <summary>The time the clocks show: network time, or the user's own time when they set one.</summary>
    internal interface IClockTime
    {
        /// <summary>The current local time to display.</summary>
        DateTime Now { get; }

        /// <summary>True while the user's own time replaces network time.</summary>
        bool IsCustom { get; }

        /// <summary>Raised when the displayed time jumps because the user set or reset it.</summary>
        event Action Changed;
    }

    /// <summary>Lets the user replace network time with their own, and go back to it.</summary>
    internal interface IClockTimeEditor : IClockTime
    {
        /// <summary>
        /// Makes the clocks show <paramref name="timeOfDay"/> right now on the
        /// <paramref name="zone"/> clock and keep ticking from there, across reloads.
        /// </summary>
        void SetTimeOfDay(TimeSpan timeOfDay, ClockZone zone);

        /// <summary>Drops the user's time so the clocks follow network time again.</summary>
        void ResetToNetworkTime();
    }
}
