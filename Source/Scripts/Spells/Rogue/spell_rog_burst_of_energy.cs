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

namespace Scripts.Spells.Rogue;

[SpellScript(24532)]
internal class spell_rog_burst_of_energy : SpellScript, ISpellEnergizedBySpell
{
    public void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType)
    {
        // Instantly increases your energy by ${60-4*$max(0,$min(15,$PL-60))}.
        amount -= 4 * Math.Max(0, Math.Min(15, target.GetLevel() - 60));
    }
}