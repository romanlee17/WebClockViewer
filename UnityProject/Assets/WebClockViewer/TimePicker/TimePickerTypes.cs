using System;

namespace WebClockViewer
{
    internal enum TimePickerAction
    {
        Cancel,
        Confirm,
        Reset,
    }

    /// <summary>What the time picker opens with.</summary>
    internal readonly struct TimePickerRequest
    {
        public TimePickerRequest(string zoneName, TimeSpan initialTime, bool canReset)
        {
            ZoneName = zoneName;
            InitialTime = initialTime;
            CanReset = canReset;
        }

        /// <summary>Names the clock in the headline, such as "Select UTC time".</summary>
        public string ZoneName { get; }

        /// <summary>The hours and minutes shown first; seconds are ignored.</summary>
        public TimeSpan InitialTime { get; }

        /// <summary>Whether to offer going back to network time.</summary>
        public bool CanReset { get; }
    }

    /// <summary>How the time picker was closed, with the chosen time when it was confirmed.</summary>
    internal readonly struct TimePickerResult
    {
        public TimePickerResult(TimePickerAction action, TimeSpan time)
        {
            Action = action;
            Time = time;
        }

        public TimePickerAction Action { get; }
        public TimeSpan Time { get; }
    }
}
