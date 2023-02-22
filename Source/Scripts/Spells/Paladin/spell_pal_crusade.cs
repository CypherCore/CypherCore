// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // 231895
    [SpellScript(231895)]
    public class spell_pal_crusade : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
        {
            amount /= 10;
        }

        private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
        {
            var powerCosts = eventInfo.GetSpellInfo().CalcPowerCost(eventInfo.GetActor(), SpellSchoolMask.Holy);

            foreach (var powerCost in powerCosts)
            {
                if (powerCost.Power == PowerType.HolyPower)
                {
                    GetAura().ModStackAmount(powerCost.Amount, AuraRemoveMode.Default, false);
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.AddPctModifier));
            AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
        }
    }
}
