// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IUnit
{
    public interface IUnitModifySpellDamageTaken : IScriptObject
    {
        void ModifySpellDamageTaken(Unit target, Unit attacker, ref double damage, SpellInfo spellInfo);
    }
}