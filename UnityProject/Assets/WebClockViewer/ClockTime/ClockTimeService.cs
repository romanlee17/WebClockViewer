using MirraGames.SDK;
using System;

namespace WebClockViewer
{
    /// <summary>
    /// Keeps the user's time as an offset from network time, stored with MirraSDK Data so it
    /// survives reloads. The clocks keep ticking with network time, shifted by the offset.
    /// </summary>
    internal sealed class ClockTimeService : IClockTimeEditor
    {
        private const string OffsetKey = "WebClockViewer.ClockTime.OffsetMilliseconds";

        private static readonly TimeSpan HalfDay = TimeSpan.FromHours(12);
        private static readonly TimeSpan Day = TimeSpan.FromDays(1);

        public event Action Changed;

        public DateTime Now => IsCustom ? NetworkNow + Offset : NetworkNow;

        public bool IsCustom => MirraSDK.Data.HasKey(OffsetKey);

        private static DateTime NetworkNow => MirraSDK.Time.CurrentDate;

        private static TimeSpan Offset => TimeSpan.FromMilliseconds(MirraSDK.Data.GetInt(OffsetKey));

        public void SetTimeOfDay(TimeSpan timeOfDay, ClockZone zone)
        {
            DateTime networkNow = NetworkNow;
            DateTime zonedNow = zone == ClockZone.Utc ? networkNow.ToUniversalTime() : networkNow;

            // Only the time of day is picked, so the date follows the instant with that time that
            // lies nearest to network time. That keeps the offset within half a day either way,
            // which also keeps it well inside an int of milliseconds.
            TimeSpan offset = zonedNow.Date + timeOfDay - zonedNow;
            if (offset > HalfDay)
            {
                offset -= Day;
            }
            else if (offset <= -HalfDay)
            {
                offset += Day;
            }

            MirraSDK.Data.SetInt(OffsetKey, (int)Math.Round(offset.TotalMilliseconds));
            Changed?.Invoke();
        }

        public void ResetToNetworkTime()
        {
            if (!IsCustom)
            {
                return;
            }

            MirraSDK.Data.DeleteKey(OffsetKey);
            Changed?.Invoke();
        }
    }
}
