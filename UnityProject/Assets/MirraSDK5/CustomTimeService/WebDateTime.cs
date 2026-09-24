using MirraGames.SDK.Common;
using System;

namespace CustomTimeService
{

    [Provider(typeof(IDateTime))]
    public class WebDateTime : CommonDateTime
    {

        protected override DateTime GetCurrentDateImpl()
        {
            return DateTime.Now;
        }

        protected override HolidayType GetCurrentHolidayImpl()
        {
            DateTime currentDate = DateTime.Now;
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