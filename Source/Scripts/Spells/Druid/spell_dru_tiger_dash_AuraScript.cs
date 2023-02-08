// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 252216 - Tiger Dash (Aura)
    internal class spell_dru_tiger_dash_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            AuraEffect effRunSpeed = GetEffect(0);

            if (effRunSpeed != null)
            {
                int reduction = aurEff.GetAmount();
                effRunSpeed.ChangeAmount(effRunSpeed.GetAmount() - reduction);
            }
        }
    }
}