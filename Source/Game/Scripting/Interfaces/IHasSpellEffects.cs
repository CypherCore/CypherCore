// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Scripting.Interfaces.ISpell;

namespace Game.Scripting.Interfaces
{
    public interface IHasSpellEffects
    {
        List<ISpellEffect> SpellEffects { get; }
    }
}