using System;
using System.Collections.Generic;
using System.Text;

public class GameTime
{
    static long StartTime = Time.UnixTime;

    static long _gameTime = 0;
    static uint _gameMSTime = 0;

    static DateTime _gameTimeSystemPoint = DateTime.MinValue;
    static DateTime _gameTimeSteadyPoint = DateTime.MinValue;

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

    public static DateTime GetGameTimeSystemPoint()
    {
        return _gameTimeSystemPoint;
    }

    public static DateTime GetGameTimeSteadyPoint()
    {
        return _gameTimeSteadyPoint;
    }

    public static uint GetUptime()
    {
        return (uint)(_gameTime - StartTime);
    }

    public static void UpdateGameTimers()
    {
        _gameTime = Time.UnixTime;
        _gameMSTime = Time.GetMSTime();
        _gameTimeSystemPoint = DateTime.Now;
        _gameTimeSteadyPoint = DateTime.Now;
    }
}
