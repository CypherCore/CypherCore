// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

class Logger
{
    public Logger(string _name, LogLevel _level)
    {
        name = _name;
        level = _level;
    }

    public void addAppender(byte id, Appender appender)
    {
        appenders[id] = appender;
    }

    public void delAppender(byte id)
    {
        appenders.Remove(id);
    }

    public void setLogLevel(LogLevel _level)
    {
        level = _level;
    }

    public string getName()
    {
        return name;
    }

    public LogLevel getLogLevel()
    {
        return level;
    }

    public void write(LogMessage message)
    {
        if (level == 0 || level > message.level || string.IsNullOrEmpty(message.text))
            return;

        foreach (var appender in appenders.Values)
            appender.Write(message);
    }

    string name;
    LogLevel level;
    Dictionary<byte, Appender> appenders = new();
}
