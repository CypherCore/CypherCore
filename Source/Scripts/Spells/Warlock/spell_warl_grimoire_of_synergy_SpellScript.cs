// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Grimoire of Synergy - 171975
    [SpellScript(171975, "spell_warl_grimoire_of_synergy")]
    public class spell_warl_grimoire_of_synergy_SpellScript : SpellScript, IOnCast
    {
        public void OnCast()
        {
            Unit caster = GetCaster();

            if (caster == null)
                return;

            Player player = caster.ToPlayer();

            if (caster.ToPlayer())
            {
                Guardian pet = player.GetGuardianPet();
                player.AddAura(GetSpellInfo().Id, player);

                if (pet != null)
                    player.AddAura(GetSpellInfo().Id, pet);
            }
        }
    }
}