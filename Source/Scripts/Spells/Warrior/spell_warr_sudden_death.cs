// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    // 52437 - Sudden Death
    [Script]
    internal class spell_warr_sudden_death : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarriorSpells.COLOSSUS_SMASH);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply)); // correct?
        }

        private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Remove cooldown on Colossus Smash
            Player player = GetTarget().ToPlayer();

            if (player)
                player.GetSpellHistory().ResetCooldown(WarriorSpells.COLOSSUS_SMASH, true);
        }
    }
}