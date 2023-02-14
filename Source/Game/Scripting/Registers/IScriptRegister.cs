// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Registers
{
    public interface IScriptRegister
    {
        Type AttributeType { get; }
        void Register(ScriptAttribute attribute, IScriptObject script, string scriptName);
    }
}
