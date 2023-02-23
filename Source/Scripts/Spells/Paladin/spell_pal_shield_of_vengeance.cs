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
    // 184662 - Shield of Vengeance
    [SpellScript(184662)]
    public class spell_pal_shield_of_vengeance : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private int absorb;
        private int currentAbsorb;

        private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                canBeRecalculated = false;

                double ap = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
                absorb = (int)(ap * 20);
                amount += absorb;
            }
        }

        private void Absorb(AuraEffect aura, DamageInfo damageInfo, ref double absorbAmount)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            currentAbsorb += (int)damageInfo.GetDamage();
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (currentAbsorb < absorb)
            {
                return;
            }

            List<Unit> targets = new List<Unit>();
            caster.GetAttackableUnitListInRange(targets, 8.0f);

            uint targetSize = (uint)targets.Count;
            if (targets.Count != 0)
            {
                absorb /= (int)targetSize;
            }

            caster.CastSpell(caster, PaladinSpells.SHIELD_OF_VENGEANCE_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorb));
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
        }
    }

}
