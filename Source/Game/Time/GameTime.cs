using System;

public class GameTime
{
    private static long StartTime = Time.UnixTime;

    private static long _gameTime = 0;
    private static uint _gameMSTime = 0;

    private static DateTime _gameTimeSystemPoint = DateTime.MinValue;
    private static DateTime _gameTimeSteadyPoint = DateTime.MinValue;

    private static DateTime _dateTime;

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

    public static DateTime GetDateAndTime()
    {
        return _dateTime;
    }

    public static void UpdateGameTimers()
    {
        _gameTime = Time.UnixTime;
        _gameMSTime = Time.GetMSTime();
        _gameTimeSystemPoint = DateTime.Now;
        _gameTimeSteadyPoint = DateTime.Now;

        _dateTime = Time.UnixTimeToDateTime(_gameTime);
    }
}
