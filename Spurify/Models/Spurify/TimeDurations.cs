using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spurify.Models.Spurify {
    public struct TimeDurations {
        public const string SevenDays = "Seven Days";
        public const string OneMonth = "One Month";
        public const string ThreeMonths = "Three Months";
        public const string SixMonths = "Six Months";
        public const string OneYear = "One Year";

        public static string LastFMRepresentation(string time) {
            switch (time) {
                case SevenDays:
                    return "7day";
                case OneMonth:
                    return "1month";
                case ThreeMonths:
                    return "3month";
                case SixMonths:
                    return "6month";
                case OneYear:
                    return "12month";
                default:
                    return null;
            }
        }

        public static List<string> ListTimeDurations() {
            return new List<string>() {
                SevenDays,
                OneMonth,
                ThreeMonths,
                SixMonths,
                OneYear
            };
        }
    }
}