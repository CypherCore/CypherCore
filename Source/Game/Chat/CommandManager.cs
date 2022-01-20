/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
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
                    ChatCommand command = new(groupAttribute);
                    BuildChildCommandsForCommand(command, type);
                    _commands.Add(groupAttribute.Name, command);
                }
                else
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                    {
                        var commandAttribute = method.GetCustomAttribute<CommandNonGroupAttribute>(true);
                        if (commandAttribute != null)
                            _commands.Add(commandAttribute.Name, new ChatCommand(commandAttribute, method));
                    }
                }
            }

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_COMMANDS);
            SQLResult result = DB.World.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    string name = result.Read<string>(0);
                    SetDataForCommandInTable(GetCommands(), name, result.Read<uint>(1), result.Read<string>(2), name);
                }
                while (result.NextRow());
            }
        }

        static void BuildChildCommandsForCommand(ChatCommand command, Type type)
        {
            foreach (var nestedType in type.GetNestedTypes(BindingFlags.NonPublic))
            {
                var groupAttribute = nestedType.GetCustomAttribute<CommandGroupAttribute>(true);
                if (groupAttribute == null)
                    continue;

                ChatCommand childCommand = new(groupAttribute);
                BuildChildCommandsForCommand(childCommand, nestedType);
                command.AddChildCommand(childCommand);
            }

            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                CommandAttribute commandAttribute = method.GetCustomAttribute<CommandAttribute>(false);
                if (commandAttribute == null)
                    continue;

                if (commandAttribute.GetType() == typeof(CommandNonGroupAttribute))
                    continue;

                command.AddChildCommand(new ChatCommand(commandAttribute, method));
            }

            command.SortChildCommands();
        }

        static bool SetDataForCommandInTable(ICollection<ChatCommand> table, string text, uint permission, string help, string fullcommand)
        {
            StringArguments args = new(text);
            string cmd = args.NextString().ToLower();

            foreach (var command in table)
            {
                // for data fill use full explicit command names
                if (command.Name != cmd)
                    continue;

                // select subcommand from child commands list (including "")
                if (!command.ChildCommands.Empty())
                {
                    var arg = args.NextString("");
                    if (SetDataForCommandInTable(command.ChildCommands, arg, permission, help, fullcommand))
                        return true;
                    else if (!arg.IsEmpty())
                        return false;

                    // fail with "" subcommands, then use normal level up command instead
                }
                // expected subcommand by full name DB content
                else if (!args.NextString().IsEmpty())
                {
                    Log.outError(LogFilter.Sql, "Table `command` have unexpected subcommand '{0}' in command '{1}', skip.", text, fullcommand);
                    return false;
                }

                if (command.Permission != (RBACPermissions)permission)
                    Log.outInfo(LogFilter.Misc, "Table `command` overwrite for command '{0}' default permission ({1}) by {2}", fullcommand, command.Permission, permission);

                command.Permission = (RBACPermissions)permission;
                command.Help = help;
                return true;
            }

            // in case "" command let process by caller
            if (!cmd.IsEmpty())
            {
                if (table == GetCommands())
                    Log.outError(LogFilter.Sql, "Table `command` have not existed command '{0}', skip.", cmd);
                else
                    Log.outError(LogFilter.Sql, "Table `command` have not existed subcommand '{0}' in command '{1}', skip.", cmd, fullcommand);
            }

            return false;
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
                handler.ParseCommand(Console.ReadLine());
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Cypher>> ");
            }
        }

        public static ICollection<ChatCommand> GetCommands()
        {
            return _commands.Values;
        }

        static SortedDictionary<string, ChatCommand> _commands = new();
    }

    public delegate bool HandleCommandDelegate(CommandHandler handler, StringArguments args);

    public class ChatCommand
    {
        public string Name;
        public RBACPermissions Permission;
        public bool AllowConsole;
        public string Help;
        public List<ChatCommand> ChildCommands = new();

        MethodInfo _methodInfo;
        Type[] parameterTypes;

        public ChatCommand(CommandAttribute attribute)
        {
            Name = attribute.Name;
            Permission = attribute.RBAC;
            AllowConsole = attribute.AllowConsole;
            Help = attribute.Help;
        }

        public ChatCommand(CommandAttribute attribute, MethodInfo methodInfo) : this(attribute)
        {
            _methodInfo = methodInfo;
            parameterTypes = (from parameter in methodInfo.GetParameters() select parameter.ParameterType).ToArray();
        }

        public void AddChildCommand(ChatCommand command)
        {
            ChildCommands.Add(command);
        }

        public void SortChildCommands()
        {
            ChildCommands = ChildCommands.OrderBy(p => string.IsNullOrEmpty(p.Name)).ThenBy(p => p.Name).ToList();
        }

        public bool Invoke(CommandHandler handler, StringArguments args)
        {
            if (parameterTypes.Contains(typeof(StringArguments)))//Old system, can remove once all commands are changed.
                return (bool)_methodInfo.Invoke(null, new object[] { handler, args });
            else
                return (bool)_methodInfo.Invoke(null, new object[] { handler }.Combine(CommandArgs.Parse(parameterTypes, args)));

        }

        public bool HasHandler()
        {
            return _methodInfo != null;
        }

        public override string ToString()
        {
            return $"Name: {Name} Permission: {Permission} AllowConsole: {AllowConsole} ChildCommandCount: {ChildCommands.Count}";
        }
    }
}
