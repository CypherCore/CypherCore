/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

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

    public static string secsToTimeString(ulong timeInSecs, bool shortText = false, bool hoursOnly = false)
    {
        ulong secs = timeInSecs % Minute;
        ulong minutes = timeInSecs % Hour / Minute;
        ulong hours = timeInSecs % Day / Hour;
        ulong days = timeInSecs / Day;

        string ss = "";
        if (days != 0)
            ss += days + (shortText ? "d" : " Day(s) ");
        if (hours != 0 || hoursOnly)
            ss += hours + (shortText ? "h" : " Hour(s) ");
        if (!hoursOnly)
        {
            if (minutes != 0)
                ss += minutes + (shortText ? "m" : " Minute(s) ");
            if (secs != 0 || (days == 0 && hours == 0 && minutes == 0))
                ss += secs + (shortText ? "s" : " Second(s).");
        }

        return ss;
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

public class TimeTrackerSmall
{
    public TimeTrackerSmall(int expiry = 0)
    {
        i_expiryTime = expiry;
    }

    public void Update(int diff)
    {
        i_expiryTime -= diff;
    }

    public bool Passed()
    {
        return i_expiryTime <= 0;
    }

    public void Reset(int interval)
    {
        i_expiryTime = interval;
    }

    public int GetExpiry()
    {
        return i_expiryTime;
    }
    int i_expiryTime;
}

public class TimeTracker
{
    public TimeTracker(long expiry = 0)
    {
        i_expiryTime = expiry;
    }

    public void Update(long diff)
    {
        i_expiryTime -= diff;
    }

    public bool Passed()
    {
        return i_expiryTime <= 0;
    }

    public void Reset(long interval)
    {
        i_expiryTime = interval;
    }

    public long GetExpiry()
    {
        return i_expiryTime;
    }

    long i_expiryTime;
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
