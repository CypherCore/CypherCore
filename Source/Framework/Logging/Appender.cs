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

using Framework.Database;
using System;
using System.IO;
using System.Text;

class ConsoleAppender : Appender
{
    public ConsoleAppender(byte id, string name, LogLevel level, AppenderFlags flags) : base(id, name, level, flags)
    {
        _consoleColor = new[]
        {
            ConsoleColor.White,
            ConsoleColor.White,
            ConsoleColor.Gray,
            ConsoleColor.Green,
            ConsoleColor.Yellow,
            ConsoleColor.Red,
            ConsoleColor.Blue
        };
    }

    public override void _write(LogMessage message)
    {
        Console.ForegroundColor = _consoleColor[(int)message.level];
        Console.WriteLine(message.prefix + message.text);
        Console.ResetColor();
    }

    public override AppenderType GetAppenderType()
    {
        return AppenderType.Console;
    }

    ConsoleColor[] _consoleColor;
}

class FileAppender : Appender, IDisposable
{
    public FileAppender(byte id, string name, LogLevel level, string fileName, string logDir, AppenderFlags flags) : base(id, name, level, flags)
    {
        Directory.CreateDirectory(logDir);
        _fileName = fileName;
        _logDir = logDir;
        _dynamicName = _fileName.Contains("{0}");

        if (_dynamicName)
        {
            Directory.CreateDirectory(logDir + "/" + _fileName.Substring(0, _fileName.IndexOf('/') + 1));
            return;
        }

        _logStream = OpenFile(_fileName, FileMode.Create);
    }

    FileStream OpenFile(string filename, FileMode mode)
    {
        return new FileStream(_logDir + "/" + filename, mode, FileAccess.Write, FileShare.ReadWrite);
    }

    public override void _write(LogMessage message)
    {
        lock (locker)
        {
            var logBytes = Encoding.Unicode.GetBytes(message.prefix + message.text + "\r\n");

            if (_dynamicName)
            {
                var logStream = OpenFile(string.Format(_fileName, message.dynamicName), FileMode.Append);
                logStream.Write(logBytes, 0, logBytes.Length);
                logStream.Flush();
                logStream.Close();
                return;
            }

            _logStream.Write(logBytes, 0, logBytes.Length);
            _logStream.Flush();
        }
    }

    public override AppenderType GetAppenderType()
    {
        return AppenderType.File;
    }

    #region IDisposable Support
    private bool disposedValue;

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _logStream.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion

    string _fileName;
    string _logDir;
    bool _dynamicName;
    FileStream _logStream;
    object locker = new object();
}

class DBAppender : Appender
{
    public DBAppender(byte id, string name, LogLevel level) : base(id, name, level) { }

    public override void _write(LogMessage message)
    {
        // Avoid infinite loop, PExecute triggers Logging with "sql.sql" type
        if (!enabled || message.type == LogFilter.Sql)
            return;

        PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_LOG);
        stmt.AddValue(0, Time.DateTimeToUnixTime(message.mtime));
        stmt.AddValue(1, realmId);
        stmt.AddValue(2, message.type);
        stmt.AddValue(3, (byte)message.level);
        stmt.AddValue(4, message.text);
        DB.Login.Execute(stmt);
    }

    public override AppenderType GetAppenderType()
    {
        return AppenderType.DB;
    }

    public override void setRealmId(uint _realmId)
    {
        enabled = true;
        realmId = _realmId;
    }

    uint realmId;
    bool enabled;
}

abstract class Appender
{
    protected Appender(byte id, string name, LogLevel level = LogLevel.Disabled, AppenderFlags flags = AppenderFlags.None)
    {
        _id = id;
        _name = name;
        _level = level;
        _flags = flags;
    }

    public void Write(LogMessage message)
    {
        if (_level == LogLevel.Disabled || (_level != LogLevel.Fatal && _level > message.level))
            return;

        StringBuilder ss = new StringBuilder();

        if (_flags.HasAnyFlag(AppenderFlags.PrefixTimestamp))
            ss.AppendFormat("{0:MM/dd/yyyy HH:mm:ss} ", message.mtime);

        if (_flags.HasAnyFlag(AppenderFlags.PrefixLogLevel))
            ss.AppendFormat("{0}: ", message.level);

        if (_flags.HasAnyFlag(AppenderFlags.PrefixLogFilterType))
            ss.AppendFormat("[{0}] ", message.type);

        message.prefix = ss.ToString();
        _write(message);
    }

    public abstract void _write(LogMessage message);

    public byte getId()
    {
        return _id;
    }

    public string getName()
    {
        return _name;
    }

    public abstract AppenderType GetAppenderType();

    public virtual void setRealmId(uint realmId) { }

    public void setLogLevel(LogLevel level)
    {
        _level = level;
    }

    byte _id;
    string _name;
    LogLevel _level;
    AppenderFlags _flags;
}

class LogMessage
{
    public LogMessage(LogLevel _level, LogFilter _type, string _text)
    {
        level = _level;
        type = _type;
        text = _text;
        mtime = DateTime.Now;
    }

    public LogLevel level;
    public LogFilter type;
    public string text;
    public string prefix;
    public string dynamicName;
    public DateTime mtime;
}
