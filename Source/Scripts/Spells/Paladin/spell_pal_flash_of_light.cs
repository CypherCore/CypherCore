// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Flash of Light - 19750
    [SpellScript(19750)]
    public class spell_pal_flash_of_light : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            GetCaster().RemoveAura(PaladinSpells.InfusionOfLightAura);
        }
    }
}
