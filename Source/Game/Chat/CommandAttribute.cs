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

using Framework.Constants;
using System;

namespace Game.Chat
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string command)
        {
            Name = command.ToLower();
        }

        public CommandAttribute(string command, RBACPermissions rbac, bool allowConsole = false)
        {
            Name = command.ToLower();
            RBAC = rbac;
            AllowConsole = allowConsole;
        }

        public CommandAttribute(string command, CypherStrings help, RBACPermissions rbac, bool allowConsole = false)
        {
            Name = command.ToLower();
            Help = help;
            RBAC = rbac;
            AllowConsole = allowConsole;
        }

        /// <summary>
        /// Command's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Help String for command.
        /// </summary>
        public CypherStrings Help { get; set; }

        /// <summary>
        /// Allow Console?
        /// </summary>
        public bool AllowConsole { get; private set; }

        /// <summary>
        /// Minimum user level required to invoke the command.
        /// </summary>
        public RBACPermissions RBAC { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupAttribute : CommandAttribute
    {
        public CommandGroupAttribute(string command) : base(command) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandNonGroupAttribute : CommandAttribute
    {
        public CommandNonGroupAttribute(string command, CypherStrings help, RBACPermissions rbac, bool allowConsole = false) : base(command, help, rbac, allowConsole) { }
        public CommandNonGroupAttribute(string command, RBACPermissions rbac, bool allowConsole = false) : base(command, rbac, allowConsole) { }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class OptionalArgAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariantArgAttribute : Attribute
    {
        public Type[] Types { get; set; }

        public VariantArgAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}
