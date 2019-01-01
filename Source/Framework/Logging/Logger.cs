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
    Dictionary<byte, Appender> appenders = new Dictionary<byte, Appender>();
}
