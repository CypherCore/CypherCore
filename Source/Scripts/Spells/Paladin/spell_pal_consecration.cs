// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Scripts.Spells.Paladin
{
    [SpellScript(26573)] // 26573 - Consecration
    internal class spell_pal_consecration : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.CONSECRATION_DAMAGE, PaladinSpells.ConsecrationProtectionAura,
                PaladinSpells.ConsecratedGroundPassive, PaladinSpells.ConsecratedGroundSlow);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            AreaTrigger at = GetTarget().GetAreaTrigger(PaladinSpells.CONSECRATION);

            if (at != null)
                GetTarget().CastSpell(at.GetPosition(), PaladinSpells.CONSECRATION_DAMAGE, new CastSpellExtraArgs());
        }
    }
}
