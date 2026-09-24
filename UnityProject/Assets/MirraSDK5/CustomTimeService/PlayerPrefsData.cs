using MirraGames.SDK.Common;
using System;
using UnityEngine;

namespace CustomTimeService
{

    /// <summary>
    /// Keeps MirraSDK Data in PlayerPrefs and flushes it on every write. The fallback provider
    /// keeps data in memory only, and on WebGL PlayerPrefs reach the browser's storage only when
    /// saved, because a closing tab never runs the quit-time save.
    /// </summary>
    [Provider(typeof(IData))]
    public class PlayerPrefsData : CommonData
    {

        private const string JsonKey = "WebClockViewer.Data";

        public PlayerPrefsData(IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            ReadJson(ParseContainers);
        }

        protected override void ReadJson(Action<string> jsonRequest)
        {
            jsonRequest?.Invoke(PlayerPrefs.GetString(JsonKey, Naming.EmptyJson));
        }

        protected override void WriteJson(string json)
        {
            PlayerPrefs.SetString(JsonKey, json);
            PlayerPrefs.Save();
        }

    }
}
