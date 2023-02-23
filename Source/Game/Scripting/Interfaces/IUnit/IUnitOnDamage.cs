// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IUnit
{
    public interface IUnitOnDamage : IScriptObject
    {
        void OnDamage(Unit attacker, Unit victim, ref double damage);
    }
}