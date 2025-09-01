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

    public struct OptionalArg<T>
    {
        public T Value { get; set { field = value; HasValue = true; } }
        public bool HasValue { get; private set; }

        public OptionalArg(T value)
        {
            Value = value;
        }

        public T GetValueOrDefault(T defaultValue) => HasValue ? Value : defaultValue;

        public static implicit operator T(OptionalArg<T> optionalArg) => optionalArg.Value;
        public static implicit operator OptionalArg<T>(T value) => new OptionalArg<T>(value);
    }

    public class VariantArg<T1>
    {
        dynamic value;

        public dynamic GetValue() { return value; }

        public bool Set(dynamic val)
        {
            return CheckAndSet<T1>(val);
        }

        public bool Is<T>() { return value is T; }

        internal bool CheckAndSet<T>(dynamic val)
        {
            if (val is not T)
                return false;

            value = val;
            return true;
        }

        public static implicit operator T1(VariantArg<T1> variant) => variant.GetValue();
    }

    public class VariantArg<T1, T2> : VariantArg<T1>
    {
        public new bool Set(dynamic val)
        {
            return CheckAndSet<T1>(val) || CheckAndSet<T2>(val);
        }

        public static implicit operator T1(VariantArg<T1, T2> variant) => variant.GetValue();
        public static implicit operator T2(VariantArg<T1, T2> variant) => variant.Is<T2>() ? variant.GetValue() : default;
    }

    public class VariantArg<T1, T2, T3> : VariantArg<T1, T2>
    {
        public new bool Set(dynamic val)
        {
            return CheckAndSet<T1>(val) || CheckAndSet<T2>(val) || CheckAndSet<T3>(val);
        }

        public static implicit operator T1(VariantArg<T1, T2, T3> variant) => variant.GetValue();
        public static implicit operator T2(VariantArg<T1, T2, T3> variant) => variant.GetValue();
        public static implicit operator T3(VariantArg<T1, T2, T3> variant) => variant.GetValue();
    }
}
