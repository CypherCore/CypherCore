// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using static System.Net.Mime.MediaTypeNames;

namespace Scripts.Spells.Warrior
{
	[SpellScript(24571)]
	internal class spell_warr_blood_fury : SpellScript, ISpellEnergizedBySpell
	{
        public void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType)
        {
            // Instantly increases your rage by ${(300-10*$max(0,$PL-60))/10}.
            amount -= 10 * Math.Max(0, Math.Min(30, target.GetLevel() - 60));
        }
    }
}