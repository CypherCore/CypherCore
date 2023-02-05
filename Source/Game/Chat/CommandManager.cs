// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Game.Chat
{
    public class CommandManager
    {
        static CommandManager()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.Attributes.HasAnyFlag(TypeAttributes.NestedPrivate | TypeAttributes.NestedPublic))
                    continue;

                var groupAttribute = type.GetCustomAttribute<CommandGroupAttribute>(true);
                if (groupAttribute != null)
                {
                    ChatCommandNode command = new(groupAttribute);
                    BuildSubCommandsForCommand(command, type);
                    _commands.Add(groupAttribute.Name, command);
                }

                //This check for any command not part of that group,  but saves us from having to add them into a new class.
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var commandAttribute = method.GetCustomAttribute<CommandNonGroupAttribute>(true);
                    if (commandAttribute != null)
                        _commands.Add(commandAttribute.Name, new ChatCommandNode(commandAttribute, method));
                }
            }

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_COMMANDS);
            SQLResult result = DB.World.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    string name = result.Read<string>(0);
                    string help = result.Read<string>(1);

                    ChatCommandNode cmd = null;
                    var map = _commands;
                    foreach (var key in name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var it = map.LookupByKey(key);
                        if (it != null)
                        {
                            cmd = it;
                            map = cmd._subCommands;
                        }
                        else
                        {
                            Log.outError(LogFilter.Sql, $"Table `command` contains data for non-existant command '{name}'. Skipped.");
                            cmd = null;
                            break;
                        }
                    }

                    if (cmd == null)
                        continue;

                    if (!cmd._helpText.IsEmpty())
                        Log.outError(LogFilter.Sql, $"Table `command` contains duplicate data for command '{name}'. Skipped.");

                    if (cmd._helpString == 0)
                        cmd._helpText = help;
                    else
                        Log.outError(LogFilter.Sql, $"Table `command` contains legacy help text for command '{name}', which uses `trinity_string`. Skipped.");
                }
                while (result.NextRow());
            }

            foreach (var (name, cmd) in _commands)
                cmd.ResolveNames(name);
        }

        static void BuildSubCommandsForCommand(ChatCommandNode command, Type type)
        {
            foreach (var nestedType in type.GetNestedTypes(BindingFlags.NonPublic))
            {
                var groupAttribute = nestedType.GetCustomAttribute<CommandGroupAttribute>(true);
                if (groupAttribute == null)
                    continue;

                ChatCommandNode subCommand = new(groupAttribute);
                BuildSubCommandsForCommand(subCommand, nestedType);
                command.AddSubCommand(subCommand);
            }

            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                var commandAttributes = method.GetCustomAttributes<CommandAttribute>(false).ToArray();
                if (commandAttributes.Length == 0)
                    continue;

                foreach (var commandAttribute in commandAttributes)
                {
                    if (commandAttribute.GetType() == typeof(CommandNonGroupAttribute))
                        continue;

                    command.AddSubCommand(new ChatCommandNode(commandAttribute, method));
                }
            }
        }

        public static void InitConsole()
        {
            if (ConfigMgr.GetDefaultValue("BeepAtStart", true))
                Console.Beep();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Cypher>> ");

            var handler = new ConsoleHandler();
            while (!Global.WorldMgr.IsStopped)
            {
                handler.ParseCommands(Console.ReadLine());
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Cypher>> ");
            }
        }

        public static SortedDictionary<string, ChatCommandNode> GetCommands()
        {
            return _commands;
        }

        static SortedDictionary<string, ChatCommandNode> _commands = new();
    }

    public delegate bool HandleCommandDelegate(CommandHandler handler, StringArguments args);

    public class ChatCommandNode
    {
        public string _name;
        public CommandPermissions _permission;
        public string _helpText;
        public CypherStrings _helpString;
        public SortedDictionary<string, ChatCommandNode> _subCommands = new();

        MethodInfo _methodInfo;
        ParameterInfo[] parameters;

        public ChatCommandNode(CommandAttribute attribute)
        {
            _name = attribute.Name;
            _permission = new CommandPermissions(attribute.RBAC, attribute.AllowConsole);
            _helpString = attribute.Help;
        }

        public ChatCommandNode(CommandAttribute attribute, MethodInfo methodInfo) : this(attribute)
        {
            _methodInfo = methodInfo;
            parameters = methodInfo.GetParameters();
        }

        public static bool TryExecuteCommand(CommandHandler handler, string cmdStr)
        {
            ChatCommandNode cmd = null;
            var map = CommandManager.GetCommands();

            cmdStr = cmdStr.Trim(' ');

            string oldTail = cmdStr;
            while (!oldTail.IsEmpty())
            {
                /* oldTail = token DELIMITER newTail */
                var (token, newTail) = oldTail.Tokenize();
                Cypher.Assert(!token.IsEmpty());
                var listOfPossibleCommands = map.Where(p => p.Key.StartsWith(token) && p.Value.IsVisible(handler)).ToList();
                if (listOfPossibleCommands.Empty())
                    break; /* no matching subcommands found */

                if (!listOfPossibleCommands[0].Key.Equals(token, StringComparison.OrdinalIgnoreCase))
                { /* ok, so it1 points at a partially matching subcommand - let's see if there are others */

                    if (listOfPossibleCommands.Count > 1)
                    { /* there are multiple matching subcommands - print possibilities and return */
                        if (cmd != null)
                            handler.SendSysMessage(CypherStrings.SubcmdAmbiguous, cmd._name, ' ', token);
                        else
                            handler.SendSysMessage(CypherStrings.CmdAmbiguous, token);

                        handler.SendSysMessage(listOfPossibleCommands[0].Value.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, listOfPossibleCommands[0].Key);
                        foreach (var (name, command) in listOfPossibleCommands)
                            handler.SendSysMessage(command.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, name);

                        return true;
                    }
                }

                /* now we matched exactly one subcommand, and it1 points to it; go down the rabbit hole */
                cmd = listOfPossibleCommands[0].Value;
                map = cmd._subCommands;

                oldTail = newTail;
            }

            if (cmd != null)
            { /* if we matched a command at some point, invoke it */
                handler.SetSentErrorMessage(false);
                if (cmd.IsInvokerVisible(handler) && cmd.Invoke(handler, oldTail))
                { /* invocation succeeded, log this */
                    if (!handler.IsConsole())
                        LogCommandUsage(handler.GetSession(), (uint)cmd._permission.RequiredPermission, cmdStr);
                }
                else if (!handler.HasSentErrorMessage())
                { /* invocation failed, we should show usage */
                    cmd.SendCommandHelp(handler);
                }
                return true;
            }

            return false;
        }

        public static void SendCommandHelpFor(CommandHandler handler, string cmdStr)
        {
            ChatCommandNode cmd = null;
            var map = CommandManager.GetCommands();
            foreach (var token in cmdStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var listOfPossibleCommands = map.Where(p => p.Key.StartsWith(token) && p.Value.IsVisible(handler)).ToList();
                if (listOfPossibleCommands.Empty())
                { /* no matching subcommands found */
                    if (cmd != null)
                    {
                        cmd.SendCommandHelp(handler);
                        handler.SendSysMessage(CypherStrings.SubcmdInvalid, cmd._name, ' ', token);
                    }
                    else
                        handler.SendSysMessage(CypherStrings.CmdInvalid, token);
                    return;
                }

                if (!listOfPossibleCommands[0].Key.Equals(token, StringComparison.OrdinalIgnoreCase))
                { /* ok, so it1 points at a partially matching subcommand - let's see if there are others */

                    if (listOfPossibleCommands.Count > 1)
                    { /* there are multiple matching subcommands - print possibilities and return */
                        if (cmd != null)
                            handler.SendSysMessage(CypherStrings.SubcmdAmbiguous, cmd._name, ' ', token);
                        else
                            handler.SendSysMessage(CypherStrings.CmdAmbiguous, token);

                        handler.SendSysMessage(listOfPossibleCommands[0].Value.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, listOfPossibleCommands[0].Key);
                        foreach (var (name, command) in listOfPossibleCommands)
                            handler.SendSysMessage(command.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, name);

                        return;
                    }
                }

                cmd = listOfPossibleCommands[0].Value;
                map = cmd._subCommands;
            }

            if (cmd != null)
                cmd.SendCommandHelp(handler);
            else if (cmdStr.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.AvailableCmds);
                foreach (var (name, command) in map)
                {
                    if (!command.IsVisible(handler))
                        continue;

                    handler.SendSysMessage(command.HasVisibleSubCommands(handler) ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, name);
                }
            }
            else
                handler.SendSysMessage(CypherStrings.CmdInvalid, cmdStr);
        }

        bool IsInvokerVisible(CommandHandler who)
        {
            if (_methodInfo == null)
                return false;

            if (who.IsConsole() && !_permission.AllowConsole)
                return false;

            return who.HasPermission(_permission.RequiredPermission);
        }

        bool HasVisibleSubCommands(CommandHandler who)
        {
            foreach (var (_, command) in _subCommands)
                if (command.IsVisible(who))
                    return true;
            return false;
        }

        public void ResolveNames(string name)
        {
            if (_methodInfo != null && (_helpText.IsEmpty() && _helpString == 0))
                Log.outWarn(LogFilter.Sql, $"Table `command` is missing help text for command '{name}'.");

            _name = name;
            foreach (var (subToken, cmd) in _subCommands)
                cmd.ResolveNames($"{name} {subToken}");
        }

        static void LogCommandUsage(WorldSession session, uint permission, string cmdStr)
        {
            if (Global.AccountMgr.IsPlayerAccount(session.GetSecurity()))
                return;

            if (Global.AccountMgr.GetRBACPermission((uint)RBACPermissions.RolePlayer).GetLinkedPermissions().Contains(permission))
                return;

            Player player = session.GetPlayer();
            ObjectGuid targetGuid = player.GetTarget();
            uint areaId = player.GetAreaId();
            string areaName = "Unknown";
            string zoneName = "Unknown";

            var area = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (area != null)
            {
                Locale locale = session.GetSessionDbcLocale();
                areaName = area.AreaName[locale];
                var zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                if (zone != null)
                    zoneName = zone.AreaName[locale];
            }

            Log.outCommand(session.GetAccountId(), $"Command: {cmdStr} [Player: {player.GetName()} ({player.GetGUID()}) (Account: {session.GetAccountId()}) " +
                $"X: {player.GetPositionX()} Y: {player.GetPositionY()} Z: {player.GetPositionZ()} Map: {player.GetMapId()} ({(player.GetMap() ? player.GetMap().GetMapName() : "Unknown")}) " +
                $"Area: {areaId} ({areaName}) Zone: {zoneName} Selected: {(player.GetSelectedUnit() ? player.GetSelectedUnit().GetName() : "")} ({targetGuid})]");
        }

        public void SendCommandHelp(CommandHandler handler)
        {
            bool hasInvoker = IsInvokerVisible(handler);
            if (hasInvoker)
            {
                if (_helpString != 0)
                    handler.SendSysMessage(_helpString);
                else if (!_helpText.IsEmpty())
                    handler.SendSysMessage(_helpText);
                else
                {
                    handler.SendSysMessage(CypherStrings.CmdHelpGeneric, _name);
                    handler.SendSysMessage(CypherStrings.CmdNoHelpAvailable, _name);
                }
            }

            bool header = false;
            foreach (var (_, command) in _subCommands)
            {
                bool subCommandHasSubCommand = command.HasVisibleSubCommands(handler);
                if (!subCommandHasSubCommand && !command.IsInvokerVisible(handler))
                    continue;
                if (!header)
                {
                    if (!hasInvoker)
                        handler.SendSysMessage(CypherStrings.CmdHelpGeneric, _name);
                    handler.SendSysMessage(CypherStrings.SubcmdsList);
                    header = true;
                }
                handler.SendSysMessage(subCommandHasSubCommand ? CypherStrings.SubcmdsListEntryEllipsis : CypherStrings.SubcmdsListEntry, command._name);
            }
        }

        bool IsVisible(CommandHandler who) { return IsInvokerVisible(who) || HasVisibleSubCommands(who); }

        public void AddSubCommand(ChatCommandNode command)
        {
            if (command._name.IsEmpty())
            {
                _permission = command._permission;
                _helpText = command._helpText;
                _helpString = command._helpString;
                _methodInfo = command._methodInfo;
                parameters = command.parameters;
            }
            else
            {
                if (!_subCommands.TryAdd(command._name, command))
                    Log.outError(LogFilter.Commands, $"Error trying to add subcommand, Already exists Command: {_name} SubCommand: {command._name}");
            }
        }

        public bool Invoke(CommandHandler handler, string args)
        {
            if (parameters.Any(p => p.ParameterType == typeof(StringArguments)))//Old system, can remove once all commands are changed.
                return (bool)_methodInfo.Invoke(null, new object[] { handler, new StringArguments(args) });
            else
            {
                var parseArgs = new dynamic[parameters.Length];
                parseArgs[0] = handler;
                var result = CommandArgs.ConsumeFromOffset(parseArgs, 1, parameters, handler, args);
                if (result.IsSuccessful())
                    return (bool)_methodInfo.Invoke(null, parseArgs);
                else
                {
                    if (result.HasErrorMessage())
                    {
                        handler.SendSysMessage(result.GetErrorMessage());
                        handler.SetSentErrorMessage(true);
                    }
                    return false;
                }
            }
        }
    }

    public struct CommandPermissions
    {
        public RBACPermissions RequiredPermission;
        public bool AllowConsole;

        public CommandPermissions(RBACPermissions perm, bool allowConsole)
        {
            RequiredPermission = perm;
            AllowConsole = allowConsole;
        }
    }
}
