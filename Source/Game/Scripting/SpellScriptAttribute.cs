// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Scripting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SpellScriptAttribute : ScriptAttribute
    {
        public SpellScriptAttribute(string name = "", params object[] args) : base(name, args)
        {
        }

        public SpellScriptAttribute(uint spellId, string name = "", bool allRanks = false, params object[] args) : base(name, args)
        {
            SpellIds = new[]
                       {
                           spellId
                       };
            AllRanks = allRanks;
        }

        public SpellScriptAttribute(uint[] spellId, string name = "", bool allRanks = false,  params object[] args) : base(name, args)
        {
            SpellIds = spellId;
            AllRanks = allRanks;
        }

        public uint[] SpellIds { get; private set; }
        public bool AllRanks { get; private set; }
    }
}