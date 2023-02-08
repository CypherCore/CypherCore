// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 329910 - Eclipse out of combat - SPELL_DRUID_ECLIPSE_OOC
    internal class spell_dru_eclipse_ooc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.EclipseDummy, DruidSpellIds.EclipseSolarSpellCnt, DruidSpellIds.EclipseLunarSpellCnt);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(Tick, 0, AuraType.PeriodicDummy));
        }

        private void Tick(AuraEffect aurEff)
        {
            Unit owner = GetTarget();
            AuraEffect auraEffDummy = owner.GetAuraEffect(DruidSpellIds.EclipseDummy, 0);

            if (auraEffDummy == null)
                return;

            if (!owner.IsInCombat() &&
                (!owner.HasAura(DruidSpellIds.EclipseSolarSpellCnt) || !owner.HasAura(DruidSpellIds.EclipseLunarSpellCnt)))
            {
                // Restore 2 stacks to each spell when out of combat
                spell_dru_eclipse_common.SetSpellCount(owner, DruidSpellIds.EclipseSolarSpellCnt, (uint)auraEffDummy.GetAmount());
                spell_dru_eclipse_common.SetSpellCount(owner, DruidSpellIds.EclipseLunarSpellCnt, (uint)auraEffDummy.GetAmount());
            }
        }
    }
}