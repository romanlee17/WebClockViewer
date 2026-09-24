using Cysharp.Threading.Tasks;
using MirraGames.SDK;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace WebClockViewer
{
    internal class ClockViewer : MonoBehaviour
    {
        [SerializeField] private Text _utcTimeText;
        [SerializeField] private Text _localTimeText;

        private async void Start()
        {
            await ClockUpdateTask();
        }

        private async UniTask ClockUpdateTask()
        {
            while (true)
            {
                DateTime currentDate = MirraSDK.Time.CurrentDate;
                DateTime utcDate = currentDate.ToUniversalTime();

                _utcTimeText.text = $"UTC+0: {utcDate:HH:mm:ss}";
                _localTimeText.text = $"Local time: {currentDate:HH:mm:ss}";

                await UniTask.Delay(1000);
            }
        }
    }
}