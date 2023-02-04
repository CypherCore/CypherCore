// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
    [Script] // 28845 - Cheat Death
    internal class spell_warr_t3_prot_8p_bonus : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActionTarget().HealthBelowPct(20))
                return true;

            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null &&
                damageInfo.GetDamage() != 0)
                if (GetTarget().HealthBelowPctDamaged(20, damageInfo.GetDamage()))
                    return true;

            return false;
        }
    }
}