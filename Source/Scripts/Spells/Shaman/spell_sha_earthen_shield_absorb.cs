using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Shaman
{
    //201633 - Earthen Shield
    [SpellScript(201633)]
    public class spell_sha_earthen_shield_absorb : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void CalcAbsorb(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            if (!GetCaster())
            {
                return;
            }

            amount = (int)GetCaster().GetHealth();
        }

        private void HandleAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Unit caster = GetCaster();
            if (caster == null || !caster.IsTotem())
            {
                return;
            }

            Unit owner = caster.GetOwner();
            if (owner == null)
            {
                return;
            }

            if (dmgInfo.GetDamage() - owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true) > 0)
            {
                absorbAmount = (uint)owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true);
            }
            else
            {
                absorbAmount = dmgInfo.GetDamage();
            }

            //201657 - The damager
            caster.CastSpell(caster, 201657, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAbsorb, 0, AuraType.SchoolAbsorb));
            AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
        }
    }
}
