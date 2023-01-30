// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Scripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ScriptAttribute : Attribute
    {
        public ScriptAttribute(string name = "", params object[] args)
        {
            Name = name;
            Args = args;
        }

        public string Name { get; private set; }
        public object[] Args { get; private set; }
    }
}
