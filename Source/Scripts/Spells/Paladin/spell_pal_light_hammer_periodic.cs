// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(114918)] // 114918 - Light's Hammer (Periodic)
    internal class spell_pal_light_hammer_periodic : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.LightHammerHealing, PaladinSpells.LightHammerDamage);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit lightHammer = GetTarget();
            Unit originalCaster = lightHammer.GetOwner();

            if (originalCaster != null)
            {
                originalCaster.CastSpell(lightHammer.GetPosition(), PaladinSpells.LightHammerDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
                originalCaster.CastSpell(lightHammer.GetPosition(), PaladinSpells.LightHammerHealing, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
            }
        }
    }
}
