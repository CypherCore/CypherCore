// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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