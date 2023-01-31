// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock
{
    // Grimoire of Synergy - 171975
    [SpellScript(171975, "spell_warl_grimoire_of_synergy")]
    public class spell_warl_grimoire_of_synergy_AuraScript : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit actor = eventInfo.GetActor();

            if (actor == null)
                return false;

            if (actor.IsPet() ||
                actor.IsGuardian())
            {
                Unit owner = actor.GetOwner();

                if (owner == null)
                    return false;

                if (RandomHelper.randChance(10))
                    owner.CastSpell(owner, SpellIds.GRIMOIRE_OF_SYNERGY_BUFF, true);

                return true;
            }

            Player player = actor.ToPlayer();

            if (actor.ToPlayer())
            {
                Guardian guardian = player.GetGuardianPet();

                if (guardian == null)
                    return false;

                if (RandomHelper.randChance(10))
                    player.CastSpell(guardian, SpellIds.GRIMOIRE_OF_SYNERGY_BUFF, true);

                return true;
            }

            return false;
        }
    }
}