// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Networking;
using System;

public struct WowTime : IComparable<WowTime>
{
    int _year = -1;
    int _month = -1;
    int _monthDay = -1;
    int _weekDay = -1;
    int _hour = -1;
    int _minute = -1;
    int _flags = -1;
    int _holidayOffset = 0;

    public WowTime() { }

    public uint GetPackedTime()
    {
        return (uint)(((_year % 100) & 0x1F) << 24
            | (_month & 0xF) << 20
            | (_monthDay & 0x3F) << 14
            | (_weekDay & 0x7) << 11
            | (_hour & 0x1F) << 6
            | (_minute & 0x3F)
            | (_flags & 0x3) << 29);
    }

    public void SetPackedTime(uint packedTime)
    {
        _year = (int)(packedTime >> 24) & 0x1F;
        if (_year == 31)
            _year = -1;

        _month = (int)((packedTime >> 20) & 0xF);
        if (_month == 15)
            _month = -1;

        _monthDay = (int)((packedTime >> 14) & 0x3F);
        if (_monthDay == 63)
            _monthDay = -1;

        _weekDay = (int)((packedTime >> 11) & 0x7);
        if (_weekDay == 7)
            _weekDay = -1;

        _hour = (int)((packedTime >> 6) & 0x1F);
        if (_hour == 31)
            _hour = -1;

        _minute = (int)(packedTime & 0x3F);
        if (_minute == 63)
            _minute = -1;

        _flags = (int)((packedTime >> 29) & 0x3);
        if (_flags == 3)
            _flags = -1;
    }

    public long GetUnixTimeFromUtcTime()
    {
        if (_year < 0 || _month < 0 || _monthDay < 0)
            return 0;

        return Time.DateTimeToUnixTime(new DateTime(_year + 100, _month, _monthDay + 1, _hour, _minute, 0, DateTimeKind.Utc));
    }

    public void SetUtcTimeFromUnixTime(long unixTime)
    {
        var dateTime = Time.UnixTimeToDateTime(unixTime);

        _year = (dateTime.Year - 100) % 100;
        _month = dateTime.Month;
        _monthDay = dateTime.Day - 1;
        _weekDay = (int)dateTime.DayOfWeek;
        _hour = dateTime.Hour;
        _minute = dateTime.Minute;
    }

    public bool IsInRange(WowTime from, WowTime to)
    {
        if (from.CompareTo(to) > 0)
            return this >= from || this < to;

        return this >= from && this < to;
    }

    public int GetYear() { return _year; }
    public void SetYear(int year)
    {
        Cypher.Assert(year == -1 || (year >= 0 && year < 32));
        _year = year;
    }

    public int GetMonth() { return _month; }
    public void SetMonth(int month)
    {
        Cypher.Assert(month == -1 || (month >= 0 && month < 12));
        _month = month;
    }

    public int GetMonthDay() { return _monthDay; }
    public void SetMonthDay(int monthDay)
    {
        Cypher.Assert(monthDay == -1 || (monthDay >= 0 && monthDay < 32));
        _monthDay = monthDay;
    }

    public int GetWeekDay() { return _weekDay; }
    public void SetWeekDay(int weekDay)
    {
        Cypher.Assert(weekDay == -1 || (weekDay >= 0 && weekDay < 7));
        _weekDay = weekDay;
    }

    public int GetHour() { return _hour; }
    public void SetHour(int hour)
    {
        Cypher.Assert(hour == -1 || (hour >= 0 && hour < 24));
        _hour = hour;
    }

    public int GetMinute() { return _minute; }
    public void SetMinute(int minute)
    {
        Cypher.Assert(minute == -1 || (minute >= 0 && minute < 60));
        _minute = minute;
    }

    public int GetFlags() { return _flags; }
    public void SetFlags(int flags)
    {
        Cypher.Assert(flags == -1 || (flags >= 0 && flags < 3));
        _flags = flags;
    }

    public int GetHolidayOffset() { return _holidayOffset; }
    public void SetHolidayOffset(int holidayOffset) { _holidayOffset = holidayOffset; }

    public static WowTime operator +(WowTime time, TimeSpan seconds)
    {
        long unixTime = time.GetUnixTimeFromUtcTime();
        unixTime += (long)seconds.TotalSeconds;
        time.SetUtcTimeFromUnixTime(unixTime);
        return time;
    }

    public static WowTime operator -(WowTime time, TimeSpan seconds)
    {
        long unixTime = time.GetUnixTimeFromUtcTime();
        unixTime -= (long)seconds.TotalSeconds;
        time.SetUtcTimeFromUnixTime(unixTime);
        return time;
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(GetPackedTime());
    }

    public void Read(WorldPacket data)
    {
        uint packedTime = data.ReadPackedTime();
        SetPackedTime(packedTime);
    }

    public int CompareTo(WowTime other)
    {
        var compareFieldIfSet = int (int left1, int right1) =>
        {
            if (left1 < 0 || right1 < 0)
                return 0;

            return left1.CompareTo(right1);
        };

        var cmp = compareFieldIfSet(_year, other._year);
        if (cmp == -1)
            return cmp;

        cmp = compareFieldIfSet(_month, other._month);
        if (cmp == -1)
            return cmp;

        cmp = compareFieldIfSet(_monthDay, other._monthDay);
        if (cmp == -1)
            return cmp;

        cmp = compareFieldIfSet(_weekDay, other._weekDay);
        if (cmp == -1)
            return cmp;

        cmp = compareFieldIfSet(_hour, other._hour);
        if (cmp == -1)
            return cmp;

        cmp = compareFieldIfSet(_minute, other._minute);
        if (cmp == -1)
            return cmp;

        return 0;
    }

    public static bool operator >=(WowTime left, WowTime right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(WowTime left, WowTime right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(WowTime left, WowTime right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(WowTime left, WowTime right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator ==(WowTime left, WowTime right)
    {
        return left.CompareTo(right) == 0;
    }

    public static bool operator !=(WowTime left, WowTime right)
    {
        return left.CompareTo(right) != 0;
    }
}
