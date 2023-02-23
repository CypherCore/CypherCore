// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.ISpellManager;
using Game.Spells;
using Scripts.Spells.Warlock;

namespace Scripts.SpellFixes.Warlock
{
    public class PowerOverwhelmingSpellFix : ISpellManagerSpellLateFix
    {
        public int[] SpellIds => new[] { (int)WarlockSpells.POWER_OVERWHELMING_AURA };

        public void ApplySpellFix(SpellInfo spellInfo)
        {
            if (Global.SpellMgr.TryGetSpellInfo(WarlockSpells.POWER_OVERWHELMING, out var power))
                spellInfo.GetEffect(0).BasePoints = power.GetEffect(1).BasePoints * .1f;
        }
    }
}
