using System;

namespace GitCredentialManager
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan ToTimeSpan(this int i, TimeUnit unit)
        {
            switch (unit)
            {
                case TimeUnit.Milliseconds:
                    return TimeSpan.FromMilliseconds(i);

                case TimeUnit.Seconds:
                    return TimeSpan.FromSeconds(i);

                case TimeUnit.Minutes:
                    return TimeSpan.FromMinutes(i);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static TimeSpan? ToTimeSpan(this int? i, TimeUnit unit)
        {
            return i.HasValue ? ToTimeSpan(i.Value, unit) : null;
        }

        public static TimeSpan ToTimeSpanOrDefault(this int? i, int defaultValue, TimeUnit unit)
        {
            return ToTimeSpan(i.GetValueOrDefault(defaultValue), unit);
        }
    }

    public enum TimeUnit
    {
        Milliseconds,
        Seconds,
        Minutes,
    }
}
