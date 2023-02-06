using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{

    // Soul Leech aura - 228974
    [SpellScript(228974)]
    public class spell_warl_soul_leech_aura : AuraScript, IAuraCheckProc
    {
        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(WarlockSpells.DEMONSKIN);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return false;
            }

            int basePoints = GetSpellInfo().GetEffect(0).BasePoints;
            int absorb = (int)(((eventInfo.GetDamageInfo() != null ? eventInfo.GetDamageInfo().GetDamage() : 0) * basePoints) / 100.0f);

            // Add remaining amount if already applied
            Aura aur = caster.GetAura(WarlockSpells.SOUL_LEECH_ABSORB);
            if (aur != null)
            {
                AuraEffect aurEff = aur.GetEffect(0);
                if (aurEff != null)
                {
                    absorb += aurEff.GetAmount();
                }
            }

            // Cannot go over 15% (or 20% with Demonskin) max health
            int basePointNormal = GetSpellInfo().GetEffect(1).BasePoints;
            int basePointDS = Global.SpellMgr.GetSpellInfo(WarlockSpells.DEMONSKIN, Difficulty.None).GetEffect(1).BasePoints;
            int totalBP = caster.HasAura(WarlockSpells.DEMONSKIN) ? basePointDS : basePointNormal;
            var threshold = ((int)caster.GetMaxHealth() * totalBP) / 100.0f;
            absorb = (int)Math.Min(absorb, threshold);

            caster.CastSpell(caster, WarlockSpells.SOUL_LEECH_ABSORB, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorb));
            return true;
        }
    }
}
