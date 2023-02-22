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
    // Holy Shield - 152261
    [SpellScript(152261)]
    public class spell_pal_holy_shield : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return (eventInfo.GetHitMask() & ProcFlagsHit.Block) != 0;
        }

        private void HandleCalcAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
        {
            amount = 0;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalcAmount, 2, AuraType.SchoolAbsorb));
        }
    }
}
