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
    [Script] // 40121 - Swift Flight Form (Passive)
    internal class spell_dru_swift_flight_passive : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseVehicleFlightSpeed));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player caster = GetCaster().ToPlayer();

            if (caster != null)
                if (caster.GetSkillValue(SkillType.Riding) >= 375)
                    amount = 310;
        }
    }
}