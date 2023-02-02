// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Text;

public enum TimeFormat
{
    FullText,       // 1 Days 2 Hours 3 Minutes 4 Seconds
    ShortText,      // 1d 2h 3m 4s
    Numeric         // 1:2:3:4
}

public static class Time
{
    public const int Minute = 60;
    public const int Hour = Minute * 60;
    public const int Day = Hour * 24;
    public const int Week = Day * 7;
    public const int Month = Day * 30;
    public const int Year = Month * 12;
    public const int InMilliseconds = 1000;

    public static readonly DateTime ApplicationStartTime = DateTime.Now;

    /// <summary>
    /// Gets the current Unix time.
    /// </summary>
    public static long UnixTime
    {
        get
        {
            return DateTimeToUnixTime(DateTime.Now);
        }
    }

    /// <summary>
    /// Gets the current Unix time, in milliseconds.
    /// </summary>
    public static long UnixTimeMilliseconds
    {
        get
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// Converts a TimeSpan to its equivalent representation in milliseconds (Int64).
    /// </summary>
    /// <param name="span">The time span value to convert.</param>
    public static long ToMilliseconds(this TimeSpan span)
    {
        return (long)span.TotalMilliseconds;
    }

    /// <summary>
    /// Gets the system uptime.
    /// </summary>
    /// <returns>the system uptime in milliseconds</returns>
    public static uint GetSystemTime()
    {
        return (uint)Environment.TickCount;
    }

    public static uint GetMSTime()
    {
        return (uint)(DateTime.Now - ApplicationStartTime).ToMilliseconds();
    }

    public static uint GetMSTimeDiff(uint oldMSTime, uint newMSTime)
    {
        if (oldMSTime > newMSTime)
            return (0xFFFFFFFF - oldMSTime) + newMSTime;
        else
            return newMSTime - oldMSTime;
    }

    public static uint GetMSTimeDiff(uint oldMSTime, DateTime newTime)
    {
        uint newMSTime = (uint)(newTime - ApplicationStartTime).TotalMilliseconds;
        return GetMSTimeDiff(oldMSTime, newMSTime);
    }

    public static uint GetMSTimeDiffToNow(uint oldMSTime)
    {
        var newMSTime = GetMSTime();
        if (oldMSTime > newMSTime)
            return (0xFFFFFFFF - oldMSTime) + newMSTime;
        else
            return newMSTime - oldMSTime;
    }

    public static DateTime UnixTimeToDateTime(long unixTime)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
    }

    public static long DateTimeToUnixTime(DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    public static long GetNextResetUnixTime(int hours)
    {
        return DateTimeToUnixTime((DateTime.Now.Date + new TimeSpan(hours, 0, 0)));
    }
    public static long GetNextResetUnixTime(int days, int hours)
    {
        return DateTimeToUnixTime((DateTime.Now.Date + new TimeSpan(days, hours, 0, 0)));
    }
    public static long GetNextResetUnixTime(int months, int days, int hours)
    {
        return DateTimeToUnixTime((DateTime.Now.Date + new TimeSpan(months + days, hours, 0)));
    }

    public static long GetLocalHourTimestamp(long time, uint hour, bool onlyAfterTime = true)
    {
        DateTime timeLocal = UnixTimeToDateTime(time);
        timeLocal = new DateTime(timeLocal.Year, timeLocal.Month, timeLocal.Day, 0, 0, 0, timeLocal.Kind);
        long midnightLocal = DateTimeToUnixTime(timeLocal);
        long hourLocal = midnightLocal + hour * Hour;

        if (onlyAfterTime && hourLocal <= time)
            hourLocal += Day;

        return hourLocal;
    }

    public static long LocalTimeToUTCTime(long time)
    {
        return DateTimeToUnixTime(UnixTimeToDateTime(time).ToUniversalTime());
    }

    public static string secsToTimeString(ulong timeInSecs, TimeFormat timeFormat = TimeFormat.FullText, bool hoursOnly = false)
    {
        ulong secs = timeInSecs % Minute;
        ulong minutes = timeInSecs % Hour / Minute;
        ulong hours = timeInSecs % Day / Hour;
        ulong days = timeInSecs / Day;

        if (timeFormat == TimeFormat.Numeric)
        {
            if (days != 0)
                return $"{days}:{hours}:{minutes}:{secs:2}";
            else if (hours != 0)
                return $"{hours}:{minutes}:{secs:2}";
            else if (minutes != 0)
                return $"{minutes}:{secs:2}";
            else
                return $"0:{secs:2}";
        }

        StringBuilder ss = new();
        if (days != 0)
        {
            ss.Append(days);
            switch (timeFormat)
            {
                case TimeFormat.ShortText:
                    ss.Append("d");
                    break;
                case TimeFormat.FullText:
                    if (days == 1)
                        ss.Append(" Day ");
                    else
                        ss.Append(" Days ");
                    break;
                default:
                    return "<Unknown time format>";
            }
        }

        if (hours != 0 || hoursOnly)
        {
            ss.Append(hours);
            switch (timeFormat)
            {

                case TimeFormat.ShortText:
                    ss.Append("h");
                    break;
                case TimeFormat.FullText:

                    if (hours <= 1)
                        ss.Append(" Hour ");
                    else
                        ss.Append(" Hours ");
                    break;
                default:
                    return "<Unknown time format>";
            }
        }

        if (!hoursOnly)
        {
            if (minutes != 0)
            {
                ss.Append(minutes);
                switch (timeFormat)
                {
                    case TimeFormat.ShortText:
                        ss.Append("m");
                        break;
                    case TimeFormat.FullText:
                        if (minutes == 1)
                            ss.Append(" Minute ");
                        else
                            ss.Append(" Minutes ");
                        break;
                    default:
                        return "<Unknown time format>";
                }
            }

            if (secs != 0 || (days == 0 && hours == 0 && minutes == 0))
            {
                ss.Append(secs);
                switch (timeFormat)
                {
                    case TimeFormat.ShortText:
                        ss.Append("s");
                        break;
                    case TimeFormat.FullText:
                        if (secs <= 1)
                            ss.Append(" Second.");
                        else
                            ss.Append(" Seconds.");
                        break;
                    default:
                        return "<Unknown time format>";
                }
            }
        }

        return ss.ToString();
    }

    public static uint TimeStringToSecs(string timestring)
    {
        int secs = 0;
        int buffer = 0;
        int multiplier;

        foreach (var c in timestring)
        {
            if (char.IsDigit(c))
            {
                buffer *= 10;
                buffer += c - '0';
            }
            else
            {
                switch (c)
                {
                    case 'd':
                        multiplier = Day;
                        break;
                    case 'h':
                        multiplier = Hour;
                        break;
                    case 'm':
                        multiplier = Minute;
                        break;
                    case 's':
                        multiplier = 1;
                        break;
                    default:
                        return 0;                         //bad format
                }
                buffer *= multiplier;
                secs += buffer;
                buffer = 0;
            }
        }

        return (uint)secs;
    }

    public static string GetTimeString(long time)
    {
        long days = time / Day;
        long hours = (time % Day) / Hour;
        long minute = (time % Hour) / Minute;

        return $"Days: {days} Hours: {hours} Minutes: {minute}";
    }

    public static long GetUnixTimeFromPackedTime(uint packedDate)
    {
        var time = new DateTime((int)((packedDate >> 24) & 0x1F) + 2000, (int)((packedDate >> 20) & 0xF) + 1, (int)((packedDate >> 14) & 0x3F) + 1, (int)(packedDate >> 6) & 0x1F, (int)(packedDate & 0x3F), 0);
        return (uint)DateTimeToUnixTime(time);
    }

    public static uint GetPackedTimeFromUnixTime(long unixTime)
    {
        var now = UnixTimeToDateTime(unixTime);
        return Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);
    }

    public static uint GetPackedTimeFromDateTime(DateTime now)
    {
        return Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);
    }

    public static void Profile(string description, int iterations, Action func)
    {
        //Run at highest priority to minimize fluctuations caused by other processes/threads
        System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        // warm up 
        func();

        var watch = new System.Diagnostics.Stopwatch();

        // clean up
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        watch.Start();
        for (int i = 0; i < iterations; i++)
        {
            func();
        }
        watch.Stop();
        Console.Write(description);
        Console.WriteLine(" Time Elapsed {0} ms", watch.Elapsed.TotalMilliseconds);
    }
}

public class TimeTracker
{
    public TimeTracker(uint expiry = 0)
    {
        _expiryTime = TimeSpan.FromMilliseconds(expiry);
    }

    public TimeTracker(TimeSpan expiry)
    {
        _expiryTime = expiry;
    }

    public void Update(uint diff)
    {
        Update(TimeSpan.FromMilliseconds(diff));
    }

    public void Update(TimeSpan diff)
    {
        _expiryTime -= diff;
    }

    public bool Passed()
    {
        return _expiryTime <= TimeSpan.Zero;
    }

    public void Reset(uint expiry)
    {
        Reset(TimeSpan.FromMilliseconds(expiry));
    }

    public void Reset(TimeSpan expiry)
    {
        _expiryTime = expiry;
    }

    public TimeSpan GetExpiry()
    {
        return _expiryTime;
    }

    TimeSpan _expiryTime;
}

public class IntervalTimer
{
    public void Update(long diff)
    {
        _current += diff;
        if (_current < 0)
            _current = 0;
    }

    public bool Passed()
    {
        return _current >= _interval;
    }

    public void Reset()
    {
        if (_current >= _interval)
            _current %= _interval;
    }

    public void SetCurrent(long current)
    {
        _current = current;
    }

    public void SetInterval(long interval)
    {
        _interval = interval;
    }

    public long GetInterval()
    {
        return _interval;
    }

    public long GetCurrent()
    {
        return _current;
    }

    long _interval;
    long _current;
}

public class PeriodicTimer
{
    public PeriodicTimer(int period, int start_time)
    {
        i_period = period;
        i_expireTime = start_time;
    }

    public bool Update(int diff)
    {
        if ((i_expireTime -= diff) > 0)
            return false;

        i_expireTime += i_period > diff ? i_period : diff;
        return true;
    }

    public void SetPeriodic(int period, int start_time)
    {
        i_expireTime = start_time;
        i_period = period;
    }

    // Tracker interface
    public void TUpdate(int diff) { i_expireTime -= diff; }
    public bool TPassed() { return i_expireTime <= 0; }
    public void TReset(int diff, int period) { i_expireTime += period > diff ? period : diff; }

    int i_period;
    int i_expireTime;
}
