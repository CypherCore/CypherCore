// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(387506)]
    public class aura_warl_mayhem : AuraScript, IAuraCheckProc, IAuraOnProc
    {
        public bool CheckProc(ProcEventInfo info)
        {
            if (info.GetProcTarget() != null)
                return RandomHelper.randChance(GetEffectInfo(0).BasePoints);

            return false;
        }

        public void OnProc(ProcEventInfo info)
        {
            GetCaster().CastSpell(info.GetProcTarget(), WarlockSpells.HAVOC, new CastSpellExtraArgs(SpellValueMod.Duration, GetEffectInfo(2).BasePoints * Time.InMilliseconds).SetIsTriggered(true));
        }
    }
}