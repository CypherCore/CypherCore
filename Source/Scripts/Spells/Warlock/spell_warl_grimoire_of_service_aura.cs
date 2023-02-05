// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // Grimoire of Service - 108501
    [SpellScript(108501)]
    internal class spell_warl_grimoire_of_service_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public void Handlearn(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player player = GetCaster().ToPlayer();

            if (GetCaster().ToPlayer())
            {
                player.LearnSpell(WarlockSpells.GRIMOIRE_IMP, false);
                player.LearnSpell(WarlockSpells.GRIMOIRE_VOIDWALKER, false);
                player.LearnSpell(WarlockSpells.GRIMOIRE_SUCCUBUS, false);
                player.LearnSpell(WarlockSpells.GRIMOIRE_FELHUNTER, false);

                if (player.GetPrimarySpecialization() == (uint)TalentSpecialization.WarlockDemonology)
                    player.LearnSpell(WarlockSpells.GRIMOIRE_FELGUARD, false);
            }
        }

        public void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player player = GetCaster().ToPlayer();

            if (GetCaster().ToPlayer())
            {
                player.RemoveSpell(WarlockSpells.GRIMOIRE_IMP, false, false);
                player.RemoveSpell(WarlockSpells.GRIMOIRE_VOIDWALKER, false, false);
                player.RemoveSpell(WarlockSpells.GRIMOIRE_SUCCUBUS, false, false);
                player.RemoveSpell(WarlockSpells.GRIMOIRE_FELHUNTER, false, false);
                player.RemoveSpell(WarlockSpells.GRIMOIRE_FELGUARD, false, false);
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(Handlearn, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}