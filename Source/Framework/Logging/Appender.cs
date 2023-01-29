// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Framework.Database;

internal class ConsoleAppender : Appender
{
    private readonly ConsoleColor[] _consoleColor;

    public ConsoleAppender(byte id, string name, LogLevel level, AppenderFlags flags) : base(id, name, level, flags)
    {
        _consoleColor = new[]
                        {
                            ConsoleColor.White, ConsoleColor.White, ConsoleColor.Gray, ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Blue
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
}

internal class FileAppender : Appender, IDisposable
{
    private readonly bool _dynamicName;

    private readonly string _fileName;
    private readonly string _logDir;
    private readonly FileStream _logStream;
    private readonly object locker = new();

    public FileAppender(byte id, string name, LogLevel level, string fileName, string logDir, AppenderFlags flags) : base(id, name, level, flags)
    {
        Directory.CreateDirectory(logDir);
        _fileName = fileName;
        _logDir = logDir;
        _dynamicName = _fileName.Contains("{0}");

        if (_dynamicName)
        {
            Directory.CreateDirectory(logDir + "/" + _fileName[..(_fileName.IndexOf('/') + 1)]);

            return;
        }

        _logStream = OpenFile(_fileName, FileMode.Create);
    }

    private FileStream OpenFile(string filename, FileMode mode)
    {
        return new FileStream(_logDir + "/" + filename, mode, FileAccess.Write, FileShare.ReadWrite);
    }

    public override void _write(LogMessage message)
    {
        lock (locker)
        {
            var logBytes = Encoding.UTF8.GetBytes(message.prefix + message.text + "\r\n");

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
                _logStream.Dispose();

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    #endregion
}

internal class DBAppender : Appender
{
    private bool enabled;

    private uint realmId;

    public DBAppender(byte id, string name, LogLevel level) : base(id, name, level)
    {
    }

    public override void _write(LogMessage message)
    {
        // Avoid infinite loop, PExecute triggers Logging with "sql.sql" type
        if (!enabled ||
            message.type == LogFilter.Sql)
            return;

        PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_LOG);
        stmt.AddValue(0, Time.DateTimeToUnixTime(message.mtime));
        stmt.AddValue(1, realmId);
        stmt.AddValue(2, message.type.ToString());
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
}

internal abstract class Appender
{
    private readonly AppenderFlags _flags;

    private readonly byte _id;
    private LogLevel _level;
    private readonly string _name;

    protected Appender(byte id, string name, LogLevel level = LogLevel.Disabled, AppenderFlags flags = AppenderFlags.None)
    {
        _id = id;
        _name = name;
        _level = level;
        _flags = flags;
    }

    public void Write(LogMessage message)
    {
        if (_level == LogLevel.Disabled ||
            (_level != LogLevel.Fatal && _level > message.level))
            return;

        StringBuilder ss = new();

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

    public virtual void setRealmId(uint realmId)
    {
    }

    public void setLogLevel(LogLevel level)
    {
        _level = level;
    }
}

internal class LogMessage
{
    public string dynamicName;

    public LogLevel level;
    public DateTime mtime;
    public string prefix;
    public string text;
    public LogFilter type;

    public LogMessage(LogLevel _level, LogFilter _type, string _text)
    {
        level = _level;
        type = _type;
        text = _text;
        mtime = DateTime.Now;
    }
}