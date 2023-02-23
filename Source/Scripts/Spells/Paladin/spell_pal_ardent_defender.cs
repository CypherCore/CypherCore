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
    // 31850 - ardent defender
    [SpellScript(31850)]
    public class spell_pal_ardent_defender : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public spell_pal_ardent_defender()
        {
            absorbPct = 0;
            healPct = 0;
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(PaladinSpells.ARDENT_DEFENDER);
        }

        public override bool Load()
        {
            absorbPct = GetSpellInfo().GetEffect(0).CalcValue();
            healPct = GetSpellInfo().GetEffect(1).CalcValue();
            return GetUnitOwner().IsPlayer();
        }

        public void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
        {
            amount = -1;
        }

        public void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
        {
            absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), absorbPct);

            Unit target = GetTarget();
            if (dmgInfo.GetDamage() < target.GetHealth())
            {
                return;
            }

            double healAmount = target.CountPctFromMaxHealth(healPct);
            target.CastSpell(target, PaladinSpells.ARDENT_DEFENDER_HEAL, (int)healAmount);
            aurEff.GetBase().Remove();
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
            AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1));
        }

        private double absorbPct;
        private double healPct;
    }
}
