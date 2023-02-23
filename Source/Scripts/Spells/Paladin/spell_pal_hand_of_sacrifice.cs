// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
    [SpellScript(6940)] // 6940 - Hand of Sacrifice
    internal class spell_pal_hand_of_sacrifice : AuraScript, IHasAuraEffects
    {
        private int remainingAmount;
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Load()
        {
            Unit caster = GetCaster();

            if (caster)
            {
                remainingAmount = (int)caster.GetMaxHealth();

                return true;
            }

            return false;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectSplitHandler(Split, 0));
        }

        private void Split(AuraEffect aurEff, DamageInfo dmgInfo, ref double splitAmount)
        {
            remainingAmount -= (int)splitAmount;

            if (remainingAmount <= 0)
                GetTarget().RemoveAura(PaladinSpells.HandOfSacrifice);
        }
    }
}
