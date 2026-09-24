using MirraGames.SDK.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using Logger = MirraGames.SDK.Common.Logger;

namespace CustomTimeService
{

    /// <summary>
    /// Serves the date from a time server instead of the device clock. The server time is anchored
    /// to <see cref="Time.realtimeSinceStartupAsDouble"/> on every sync, so between syncs the date
    /// advances with the unscaled startup clock and never with the device clock, which the user can
    /// change. Sources are tried in order; when none answers, the last server time keeps being
    /// extrapolated, and before any server has answered the device clock is the safety net.
    /// </summary>
    [Provider(typeof(IDateTime))]
    public class WebDateTime : CommonDateTime
    {

        private delegate bool TimeParser(string text, out DateTime utcDate);

        /// <summary>
        /// Both endpoints allow cross-origin requests, which WebGL builds need, and answer with
        /// no-cache headers, so the browser never serves a stale time.
        /// </summary>
        private static readonly (string url, TimeParser parser)[] timeSources =
        {
            ("https://time.akamai.com/?ms", TryParseAkamai),
            ("https://www.cloudflare.com/cdn-cgi/trace", TryParseCloudflare),
        };

        private const int RequestTimeoutSeconds = 5;

        private static readonly List<Action> onSynchronizedActions = new();

        /// <summary>
        /// True once the first sync attempt has finished, whether it reached a time server or fell
        /// back to the device clock.
        /// </summary>
        public static bool IsSynchronized { get; private set; }

        /// <summary>
        /// Invokes <paramref name="onSynchronized"/> once the first sync attempt has finished, right
        /// away when it already has. <see cref="IDateTime"/> is not awaitable in MirraSDK, so
        /// <c>WaitForProviders</c> does not wait for the sync and callers wait here instead.
        /// </summary>
        public static void WaitForSynchronization(Action onSynchronized)
        {
            if (IsSynchronized)
            {
                onSynchronized?.Invoke();
                return;
            }

            onSynchronizedActions.Add(onSynchronized);
        }

        private readonly WaitForSecondsRealtime syncInterval = new(15.0f);

        private bool hasServerTime;
        private DateTime syncedUtcDate;
        private double syncedRealtime;

        public WebDateTime(IEventDispatcher eventDispatcher)
        {
            eventDispatcher.StartCoroutine(SyncRoutine());
        }

        private IEnumerator SyncRoutine()
        {
            while (true)
            {
                yield return SyncFromSources();

                if (!IsSynchronized)
                {
                    IsSynchronized = true;
                    InvokeSynchronizedActions();
                }

                yield return syncInterval;
            }
        }

        private IEnumerator SyncFromSources()
        {
            foreach ((string url, TimeParser parser) in timeSources)
            {
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = RequestTimeoutSeconds;

                double requestRealtime = Time.realtimeSinceStartupAsDouble;
                yield return request.SendWebRequest();
                double responseRealtime = Time.realtimeSinceStartupAsDouble;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Logger.CreateWarning(this, nameof(SyncFromSources), url, request.error);
                    continue;
                }

                string text = request.downloadHandler.text;
                if (!parser(text, out DateTime serverUtcDate))
                {
                    Logger.CreateWarning(this, nameof(SyncFromSources), url, "unexpected response", text);
                    continue;
                }

                // The server stamped its time somewhere during the round trip; the midpoint is the
                // best estimate, so the stamp is half a round trip old by the time the response arrives.
                double halfRoundTripSeconds = (responseRealtime - requestRealtime) / 2.0;
                syncedUtcDate = serverUtcDate.AddSeconds(halfRoundTripSeconds);
                syncedRealtime = responseRealtime;

                if (!hasServerTime)
                {
                    hasServerTime = true;
                    Logger.CreateText(this, nameof(SyncFromSources), "synchronized with", url);
                }
                yield break;
            }

            Logger.CreateError(this, nameof(SyncFromSources), hasServerTime
                ? "no time source answered, extrapolating the last server time"
                : "no time source answered, using the device clock");
        }

        /// <summary>Parses Unix seconds with milliseconds, such as <c>1790255757.966</c>.</summary>
        private static bool TryParseAkamai(string text, out DateTime utcDate)
        {
            return TryParseUnixSeconds(text.Trim(), out utcDate);
        }

        /// <summary>Parses the <c>ts=</c> line out of the trace's <c>key=value</c> lines.</summary>
        private static bool TryParseCloudflare(string text, out DateTime utcDate)
        {
            const string timestampPrefix = "ts=";
            foreach (string line in text.Split('\n'))
            {
                if (line.StartsWith(timestampPrefix, StringComparison.Ordinal)
                    && TryParseUnixSeconds(line.Substring(timestampPrefix.Length).Trim(), out utcDate))
                {
                    // The trace truncates to whole seconds, so the true time lies anywhere in the
                    // following second; its middle halves the worst-case error.
                    utcDate = utcDate.AddSeconds(0.5);
                    return true;
                }
            }

            utcDate = default;
            return false;
        }

        private static bool TryParseUnixSeconds(string text, out DateTime utcDate)
        {
            if (decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal seconds)
                && seconds > 0)
            {
                utcDate = DateTimeOffset.FromUnixTimeMilliseconds((long)(seconds * 1000)).UtcDateTime;
                return true;
            }

            utcDate = default;
            return false;
        }

        private static void InvokeSynchronizedActions()
        {
            Action[] actions = onSynchronizedActions.ToArray();
            onSynchronizedActions.Clear();
            foreach (Action action in actions)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception exception)
                {
                    Logger.CreateError(nameof(WebDateTime), nameof(InvokeSynchronizedActions), exception);
                }
            }
        }

        protected override DateTime GetCurrentDateImpl()
        {
            if (!hasServerTime)
            {
                return DateTime.Now;
            }

            double elapsedSeconds = Time.realtimeSinceStartupAsDouble - syncedRealtime;
            return syncedUtcDate.AddSeconds(elapsedSeconds).ToLocalTime();
        }

        protected override HolidayType GetCurrentHolidayImpl()
        {
            DateTime currentDate = GetCurrentDateImpl();
            int day = currentDate.Day;
            return currentDate.Month switch
            {
                1 when day == 1 => HolidayType.NewYear,
                10 when day == 31 => HolidayType.Halloween,
                4 when day == 4 => HolidayType.Easter,
                _ => HolidayType.None,
            };
        }

    }
}
