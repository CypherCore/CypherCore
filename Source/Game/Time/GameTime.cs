// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework;
using System;

public class GameTime
{
    static long StartTime = Time.UnixTime;

    static long _gameTime = Time.UnixTime;
    static uint _gameMSTime = 0;

    static DateTime _gameTimeSystemPoint = DateTime.MinValue;
    static DateTime _gameTimeSteadyPoint = DateTime.MinValue;

    static DateTime _dateTime;

    static WowTime UtcWow;
    static WowTime Wow;

    public static long GetStartTime()
    {
        return StartTime;
    }

    public static long GetGameTime()
    {
        return _gameTime;
    }

    public static uint GetGameTimeMS()
    {
        return _gameMSTime;
    }

    public static DateTime GetSystemTime()
    {
        return _gameTimeSystemPoint;
    }

    public static DateTime Now()
    {
        return _gameTimeSteadyPoint;
    }

    public static uint GetUptime()
    {
        return (uint)(_gameTime - StartTime);
    }

    public static DateTime GetDateAndTime()
    {
        return _dateTime;
    }

    public static WowTime GetUtcWowTime()
    {
        return UtcWow;
    }

    public static WowTime GetWowTime()
    {
        return Wow;
    }

    public static void UpdateGameTimers()
    {
        _gameTime = Time.UnixTime;
        _gameMSTime = Time.GetMSTime();
        _gameTimeSystemPoint = DateTime.Now;
        _gameTimeSteadyPoint = DateTime.Now;

        _dateTime = Time.UnixTimeToDateTime(_gameTime);

        UtcWow.SetUtcTimeFromUnixTime(_gameTime);
        Wow = UtcWow + Timezone.GetSystemZoneOffsetAt(_gameTimeSystemPoint);
    }
}
