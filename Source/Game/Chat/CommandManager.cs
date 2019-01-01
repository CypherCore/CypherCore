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
            try
            {
                foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                {
                    if (type.Attributes.HasAnyFlag(TypeAttributes.NestedPrivate))
                        continue;

                    var groupAttribute = type.GetCustomAttribute<CommandGroupAttribute>(true);
                    if (groupAttribute != null)
                    {
                        _commands.Add(groupAttribute.Name, new ChatCommand(type, groupAttribute));
                    }

                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                    {
                        var commandAttribute = method.GetCustomAttribute<CommandNonGroupAttribute>(true);
                        if (commandAttribute != null)
                            _commands.Add(commandAttribute.Name, new ChatCommand(commandAttribute, (HandleCommandDelegate)method.CreateDelegate(typeof(HandleCommandDelegate))));
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
            catch (Exception ex)
            {
                Log.outException(ex);
            }
        }

        static bool SetDataForCommandInTable(ICollection<ChatCommand> table, string text, uint permission, string help, string fullcommand)
        {
            StringArguments args = new StringArguments(text);
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

        static SortedDictionary<string, ChatCommand> _commands = new SortedDictionary<string, ChatCommand>();
    }

    public delegate bool HandleCommandDelegate(StringArguments args, CommandHandler handler);

    public class ChatCommand
    {
        public ChatCommand(CommandAttribute attribute, HandleCommandDelegate handler)
        {
            Name = attribute.Name;
            Permission = attribute.RBAC;
            AllowConsole = attribute.AllowConsole;
            Handler = handler;
            Help = attribute.Help;
        }

        public ChatCommand(Type type, CommandAttribute attribute)
        {
            Name = attribute.Name;
            Permission = attribute.RBAC;
            AllowConsole = attribute.AllowConsole;
            Help = attribute.Help;

            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                CommandAttribute commandAttribute = method.GetCustomAttribute<CommandAttribute>(false);
                if (commandAttribute == null)
                    continue;

                if (commandAttribute.GetType() == typeof(CommandNonGroupAttribute))
                    continue;

                ChildCommands.Add(new ChatCommand(commandAttribute, (HandleCommandDelegate)method.CreateDelegate(typeof(HandleCommandDelegate))));
            }

            foreach (var nestedType in type.GetNestedTypes(BindingFlags.NonPublic))
            {
                var groupAttribute = nestedType.GetCustomAttribute<CommandGroupAttribute>(true);
                if (groupAttribute == null)
                    continue;

                ChildCommands.Add(new ChatCommand(nestedType, groupAttribute));
            }

            ChildCommands = ChildCommands.OrderBy(p => string.IsNullOrEmpty(p.Name)).ThenBy(p => p.Name).ToList();
        }

        public string Name;
        public RBACPermissions Permission;
        public bool AllowConsole;
        public HandleCommandDelegate Handler;
        public string Help;
        public List<ChatCommand> ChildCommands = new List<ChatCommand>();
    }
}
