// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // 84963  - Inquisition
    [SpellScript(84963)]
    public class spell_pal_inquisition : SpellScript, ISpellOnTakePower, ISpellAfterHit
    {
        private double m_powerTaken = 0.0f;

        public void TakePower(SpellPowerCost powerCost)
        {
            m_powerTaken = powerCost.Amount;
        }

        public void AfterHit()
        {
            Aura aura = GetCaster().GetAura(GetSpellInfo().Id);
            if (aura != null)
            {
                aura.SetDuration((int)(aura.GetDuration() * m_powerTaken));
            }
        }
    }
}
