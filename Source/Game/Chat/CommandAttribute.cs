// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;
using System.Numerics;

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

    public class VariantArgAttribute : Attribute
    {
        public Type[] Types { get; set; }

        public VariantArgAttribute(params Type[] types)
        {
            Types = types;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariantArg<T1> : VariantArgAttribute
    {
        public VariantArg() : base(typeof(T1)) { }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariantArg<T1, T2> : VariantArgAttribute
    {
        public VariantArg() : base(typeof(T1), typeof(T2)) { }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariantArg<T1, T2, T3> : VariantArgAttribute
    {
        public VariantArg() : base(typeof(T1), typeof(T2), typeof(T3)) { }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariantArg<T1, T2, T3, T4> : VariantArgAttribute
    {
        public VariantArg() : base(typeof(T1), typeof(T2), typeof(T3), typeof(T4)) { }
    }
}
