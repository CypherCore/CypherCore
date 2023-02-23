// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.IUnit
{
    public interface IUnitOnMeleeAttack : IScriptObject
    {
        void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra);
    }
}
