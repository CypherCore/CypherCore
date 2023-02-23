// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IUnit
{
    public interface IUnitModifyPeriodicDamageAurasTick : IScriptObject
    {
        void ModifyPeriodicDamageAurasTick(Unit target, Unit attacker, ref double damage);
    }
}