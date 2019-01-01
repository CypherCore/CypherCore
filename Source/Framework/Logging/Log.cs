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

using Framework.Collections;
using Framework.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

public class Log
{
    static Log()
    {
        m_logsDir = AppContext.BaseDirectory + ConfigMgr.GetDefaultValue("LogsDir", "");
        lowestLogLevel = LogLevel.Fatal;

        foreach (var appenderName in ConfigMgr.GetKeysByString("Appender."))
        {
            CreateAppenderFromConfig(appenderName);
        }

        foreach (var loggerName in ConfigMgr.GetKeysByString("Logger."))
        {
            CreateLoggerFromConfig(loggerName);
        }

        // Bad config configuration, creating default config
        if (!loggers.ContainsKey(LogFilter.Server))
        {
            Console.WriteLine("Wrong Loggers configuration. Review your Logger config section.\nCreating default loggers [Server (Info)] to console\n");

            loggers.Clear();
            appenders.Clear();

            byte id = NextAppenderId();

            var appender = new ConsoleAppender(id, "Console", LogLevel.Debug, AppenderFlags.None);
            appenders[id] = appender;

            var serverLogger = new Logger("Server", LogLevel.Error);
            serverLogger.addAppender(id, appender);
            loggers[LogFilter.Server] = serverLogger;
        }

        outInfo(LogFilter.Server, @" ____                    __                      ");
        outInfo(LogFilter.Server, @"/\  _`\                 /\ \                     ");
        outInfo(LogFilter.Server, @"\ \ \/\_\  __  __  _____\ \ \___      __   _ __  ");
        outInfo(LogFilter.Server, @" \ \ \/_/_/\ \/\ \/\ '__`\ \  _ `\  /'__`\/\`'__\");
        outInfo(LogFilter.Server, @"  \ \ \L\ \ \ \_\ \ \ \L\ \ \ \ \ \/\  __/\ \ \/ ");
        outInfo(LogFilter.Server, @"   \ \____/\/`____ \ \ ,__/\ \_\ \_\ \____\\ \_\ ");
        outInfo(LogFilter.Server, @"    \/___/  `/___/> \ \ \/  \/_/\/_/\/____/ \/_/ ");
        outInfo(LogFilter.Server, @"               /\___/\ \_\                       ");
        outInfo(LogFilter.Server, @"               \/__/  \/_/                   Core");
        outInfo(LogFilter.Server, "https://github.com/CypherCore/CypherCore \r\n");
    }

    static bool ShouldLog(LogFilter type, LogLevel level)
    {
        // Don't even look for a logger if the LogLevel is lower than lowest log levels across all loggers
        if (level < lowestLogLevel)
            return false;

        Logger logger = GetLoggerByType(type);
        if (logger == null)
            return false;

        LogLevel logLevel = logger.getLogLevel();
        return logLevel != LogLevel.Disabled && logLevel <= level;
    }

    public static void outInfo(LogFilter type, string text, params object[] args)
    {
        if (!ShouldLog(type, LogLevel.Info))
            return;

        outMessage(type, LogLevel.Info, text, args);
    }

    public static void outWarn(LogFilter type, string text, params object[] args)
    {
        if (!ShouldLog(type, LogLevel.Warn))
            return;

        outMessage(type, LogLevel.Warn, text, args);
    }

    [Conditional("DEBUG")]
    public static void outDebug(LogFilter type, string text, params object[] args)
    {
        if (!ShouldLog(type, LogLevel.Debug))
            return;

        outMessage(type, LogLevel.Debug, text, args);
    }

    public static void outError(LogFilter type, string text, params object[] args)
    {
        if (!ShouldLog(type, LogLevel.Error))
            return;

        outMessage(type, LogLevel.Error, text, args);
    }

    public static void outException(Exception ex, [CallerMemberName]string memberName = "")
    {
        if (!ShouldLog(LogFilter.Server, LogLevel.Fatal))
            return;

        outMessage(LogFilter.Server, LogLevel.Fatal, "CallingMember: {0} ExceptionMessage: {1}", memberName, ex.Message);
    }

    public static void outFatal(LogFilter type, string text, params object[] args)
    {
        if (!ShouldLog(type, LogLevel.Fatal))
            return;

        outMessage(type, LogLevel.Fatal, text, args);
    }

    public static void outTrace(LogFilter type, string text, params object[] args)
    {
        if (!ShouldLog(type, LogLevel.Trace))
            return;

        outMessage(type, LogLevel.Trace, text, args);
    }

    public static void outCommand(uint accountId, string text, params object[] args)
    {
        if (!ShouldLog(LogFilter.Commands, LogLevel.Info))
            return;

        var msg = new LogMessage(LogLevel.Info, LogFilter.Commands, string.Format(text, args));
        msg.dynamicName = accountId.ToString();

        Logger logger = GetLoggerByType(LogFilter.Commands);
        logger.write(msg);
    }

    static void outMessage(LogFilter type, LogLevel level, string text, params object[] args)
    {
        Logger logger = GetLoggerByType(type);
        logger.write(new LogMessage(level, type, string.Format(text, args)));
    }

    static byte NextAppenderId()
    {
        return AppenderId++;
    }

    static void CreateAppenderFromConfig(string appenderName)
    {
        if (string.IsNullOrEmpty(appenderName))
            return;

        string options = ConfigMgr.GetDefaultValue(appenderName, "");
        var tokens = new StringArray(options, ',');
        string name = appenderName.Substring(9);

        if (tokens.Length < 2)
        {
            Console.WriteLine("Log.CreateAppenderFromConfig: Wrong configuration for appender {0}. Config line: {1}", name, options);
            return;
        }

        AppenderFlags flags = AppenderFlags.None;
        AppenderType type = (AppenderType)uint.Parse(tokens[0]);
        LogLevel level = (LogLevel)uint.Parse(tokens[1]);

        if (level > LogLevel.Fatal)
        {
            Console.WriteLine("Log.CreateAppenderFromConfig: Wrong Log Level {0} for appender {1}\n", level, name);
            return;
        }

        if (tokens.Length > 2)
            flags = (AppenderFlags)uint.Parse(tokens[2]);

        byte id = NextAppenderId();
        switch (type)
        {
            case AppenderType.Console:
                {
                    var appender = new ConsoleAppender(id, name, level, flags);
                    appenders[id] = appender;
                    break;
                }
            case AppenderType.File:
                {
                    string filename = "";
                    if (tokens.Length < 4)
                    {
                        if (name != "Server")
                        {
                            Console.WriteLine("Log.CreateAppenderFromConfig: Missing file name for appender {0}", name);
                            return;
                        }

                        filename = Process.GetCurrentProcess().ProcessName + ".log";
                    }
                    else
                        filename = tokens[3];

                    appenders[id] = new FileAppender(id, name, level, filename, m_logsDir, flags);
                    break;
                }
            case AppenderType.DB:
                {
                    appenders[id] = new DBAppender(id, name, level);
                    break;
                }
            default:
                Console.WriteLine("Log.CreateAppenderFromConfig: Unknown type {0} for appender {1}", type, name);
                break;
        }
    }

    static void CreateLoggerFromConfig(string appenderName)
    {
        if (string.IsNullOrEmpty(appenderName))
            return;

        LogLevel level = LogLevel.Disabled;
        string name = appenderName.Substring(7);

        string options = ConfigMgr.GetDefaultValue(appenderName, "");
        if (string.IsNullOrEmpty(options))
        {
            Console.WriteLine("Log.CreateLoggerFromConfig: Missing config option Logger.{0}", name);
            return;
        }
        var tokens = new StringArray(options, ',');

        LogFilter type = name.ToEnum<LogFilter>();        
        if (loggers.ContainsKey(type))
        {
            Console.WriteLine("Error while configuring Logger {0}. Already defined", name);
            return;
        }

        level = (LogLevel)uint.Parse(tokens[0]);
        if (level > LogLevel.Fatal)
        {
            Console.WriteLine("Log.CreateLoggerFromConfig: Wrong Log Level {0} for logger {1}", type, name);
            return;
        }

        if (level < lowestLogLevel)
            lowestLogLevel = level;

        Logger logger = new Logger(name, level);

        int i = 0;
        var ss = new StringArray(tokens[1], ' ');
        while (i < ss.Length)
        {
            var str = ss[i++];
            Appender appender = GetAppenderByName(str);
            if (appender == null)
                Console.WriteLine("Error while configuring Appender {0} in Logger {1}. Appender does not exist", str, name);
            else
                logger.addAppender(appender.getId(), appender);
        }

        loggers[type] = logger;
    }

    static Appender GetAppenderByName(string name)
    {
        return appenders.First(p => p.Value.getName() == name).Value;
    }

    static Logger GetLoggerByType(LogFilter type)
    {
        if (loggers.ContainsKey(type))
            return loggers[type];

        string typeString = type.ToString();

        int index = 1;
        for (; index < typeString.Length - 1; ++index)
        {
            if (char.IsUpper(typeString[index]))
                break;
        }

        typeString = typeString.Substring(0, index);
        if (typeString.IsEmpty())
            return null;

        LogFilter parentLogger;
        if (!Enum.TryParse(typeString, out parentLogger))
            return null;

        return GetLoggerByType(parentLogger);
    }

    public static bool SetLogLevel(string name, string newLevelc, bool isLogger = true)
    {
        LogLevel newLevel = (LogLevel)uint.Parse(newLevelc);
        if (newLevel < 0)
            return false;

        if (isLogger)
        {
            foreach (var logger in loggers.Values)
            {
                if (logger.getName() == name)
                {
                    logger.setLogLevel(newLevel);
                    if (newLevel != LogLevel.Disabled && newLevel < lowestLogLevel)
                        lowestLogLevel = newLevel;
                    return true;
                }
            }
            return false;
        }
        else
        {
            Appender appender = GetAppenderByName(name);
            if (appender == null)
                return false;

            appender.setLogLevel(newLevel);
        }

        return true;
    }

    public static void SetRealmId(uint id)
    {
        foreach (var appender in appenders.Values)
            appender.setRealmId(id);
    }

    static string GetLogsDir() { return m_logsDir; }

    static Dictionary<byte, Appender> appenders = new Dictionary<byte, Appender>();
    static Dictionary<LogFilter, Logger> loggers = new Dictionary<LogFilter, Logger>();
    static string m_logsDir;
    static byte AppenderId;

    static LogLevel lowestLogLevel;
}

enum AppenderType
{
    None,
    Console,
    File,
    DB
}

[Flags]
enum AppenderFlags
{
    None = 0x00,
    PrefixTimestamp = 0x01,
    PrefixLogLevel = 0x02,
    PrefixLogFilterType = 0x04,
}

enum LogLevel
{
    Disabled = 0,
    Trace = 1,
    Debug = 2,
    Info = 3,
    Warn = 4,
    Error = 5,
    Fatal = 6,
    Max = 6
}

public enum LogFilter
{
    Misc,
    Achievement,
    Addon,
    Ahbot,
    AreaTrigger,
    Arena,
    Auctionhouse,
    Battlefield,
    Battleground,
    BattlegroundReportPvpAfk,
    Calendar,
    ChatLog,
    ChatSystem,
    Cheat,
    Commands,
    Condition,
    Conversation,
    Garrison,
    Gameevent,
    Guild,
    Lfg,
    Loot,
    MapsScript,
    Maps,
    Movement,
    Network,
    Outdoorpvp,
    Pet,
    Player,
    PlayerCharacter,
    PlayerDump,
    PlayerItems,
    PlayerLoading,
    PlayerSkills,
    Pool,
    Rbac,
    Realmlist,
    Scenario,
    Scenes,
    Scripts,
    ScriptsAi,
    Server,
    ServerLoading,
    ServiceProtobuf,
    Session,
    SessionRpc,
    Spells,
    SpellsPeriodic,
    Sql,
    SqlDev,
    SqlDriver,
    SqlUpdates,
    Transport,
    Unit,
    Vehicle,
    Warden,
}