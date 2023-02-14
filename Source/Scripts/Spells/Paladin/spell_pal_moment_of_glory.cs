// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(327193)] // 327193 - Moment of Glory
    internal class spell_pal_moment_of_glory : SpellScript, ISpellOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.AvengersShield);
        }

        public void OnHit()
        {
            GetCaster().GetSpellHistory().ResetCooldown(PaladinSpells.AvengersShield);
        }
    }
}
