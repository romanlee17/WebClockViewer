using MirraGames.SDK.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Logger = MirraGames.SDK.Common.Logger;

namespace CustomTimeService
{

    /// <summary>
    /// Serves the date from a time server instead of the device clock. The server time is anchored
    /// to <see cref="Time.realtimeSinceStartupAsDouble"/> on every sync, so between syncs the date
    /// advances with the unscaled startup clock and never with the device clock, which the user can
    /// change.
    /// </summary>
    [Provider(typeof(IDateTime))]
    public class WebDateTime : CommonDateTime
    {

        private const string SyncUrl = "https://yandex.com/time/sync.json";
        private const int RequestTimeoutSeconds = 10;

        private static readonly List<Action> onSynchronizedActions = new();

        /// <summary>True once the first request to the time server has succeeded.</summary>
        public static bool IsSynchronized { get; private set; }

        /// <summary>
        /// Invokes <paramref name="onSynchronized"/> once the first sync has succeeded, right away
        /// when it already has. <see cref="IDateTime"/> is not awaitable in MirraSDK, so
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
        private readonly WaitForSecondsRealtime retryInterval = new(2.0f);

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
                yield return RequestServerTime();
                // Until the first success the app is waiting on us, so retry sooner than the
                // regular resync.
                yield return IsSynchronized ? syncInterval : retryInterval;
            }
        }

        private IEnumerator RequestServerTime()
        {
            using UnityWebRequest request = UnityWebRequest.Get(SyncUrl);
            request.timeout = RequestTimeoutSeconds;

            double requestRealtime = Time.realtimeSinceStartupAsDouble;
            yield return request.SendWebRequest();
            double responseRealtime = Time.realtimeSinceStartupAsDouble;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Logger.CreateError(this, nameof(RequestServerTime), request.error);
                yield break;
            }

            SyncResponse response;
            try
            {
                response = JsonUtility.FromJson<SyncResponse>(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Logger.CreateError(this, nameof(RequestServerTime), exception);
                yield break;
            }

            if (response == null || response.time <= 0)
            {
                Logger.CreateError(this, nameof(RequestServerTime), request.downloadHandler.text);
                yield break;
            }

            // The server stamped its time somewhere during the round trip; the midpoint is the best
            // estimate, so the stamp is half a round trip old by the time the response arrives.
            double halfRoundTripSeconds = (responseRealtime - requestRealtime) / 2.0;
            syncedUtcDate = DateTimeOffset.FromUnixTimeMilliseconds(response.time).UtcDateTime
                .AddSeconds(halfRoundTripSeconds);
            syncedRealtime = responseRealtime;

            if (!IsSynchronized)
            {
                IsSynchronized = true;
                Logger.CreateText(this, nameof(RequestServerTime), "synchronized");
                InvokeSynchronizedActions();
            }
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
            if (!IsSynchronized)
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

        [Serializable]
        private class SyncResponse
        {
            public long time = default;
        }

    }
}
