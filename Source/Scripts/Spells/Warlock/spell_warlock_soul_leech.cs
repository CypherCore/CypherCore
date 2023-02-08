using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 108370 - Soul Leech
    [SpellScript(108370)]
    public class spell_warlock_soul_leech : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Unit secondaryTarget = null;
			//  if (Player* player = caster->ToPlayer())
			// secondaryTarget = player->GetPet();
			Pet pet = caster.ToPet();
			if (pet != null) 
            {
                secondaryTarget = pet.GetOwner();
                if (secondaryTarget == null)
                {
                    return;
                }
            }

            Unit[] targets = { caster, secondaryTarget };

            foreach (Unit target in targets)
            {
                var finalAmount = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
                if (finalAmount > 0)
                {
                    int maxHealthPct = GetEffect(1).GetAmount();
                    if (GetEffect(1).GetAmount() != 0)
                    {
                        var soulLinkHeal = finalAmount; // save value for soul link

                        // add old amount
                        Aura aura = target.GetAura(WarlockSpells.SOUL_LEECH_SHIELD);
                        if (aura != null)
                        {
                            finalAmount += (uint)aura.GetEffect(0).GetAmount();
                        }

                        MathFunctions.AddPct(ref finalAmount, caster.GetAuraEffectAmount(WarlockSpells.ARENA_DAMPENING, 0));

                        var demonskinBonus = caster.GetAuraEffectAmount(WarlockSpells.DEMON_SKIN, 1);
                        if (demonskinBonus != 0)
                        {
                            maxHealthPct = demonskinBonus;
                        }
                        var args = new CastSpellExtraArgs(true);
                        finalAmount = Math.Min(finalAmount, (uint)MathFunctions.CalculatePct(target.GetMaxHealth(), maxHealthPct));

                        args.SpellValueOverrides.Add(SpellValueMod.BasePoint0, (int)finalAmount);
                        args.SpellValueOverrides.Add(SpellValueMod.BasePoint1, (int)finalAmount);
                        args.SpellValueOverrides.Add(SpellValueMod.BasePoint2, (int)finalAmount);
                        args.SpellValueOverrides.Add(SpellValueMod.BasePoint3, (int)finalAmount);
                        target.CastSpell(target, WarlockSpells.SOUL_LEECH_SHIELD, args);

                        if (target.ToPlayer() && target.HasAura(WarlockSpells.SOUL_LINK_BUFF))
                        {
                      
                            var playerHeal = MathFunctions.CalculatePct(soulLinkHeal, target.GetAura(WarlockSpells.SOUL_LINK_BUFF).GetEffect(1).GetAmount());
                            var petHeal = MathFunctions.CalculatePct(soulLinkHeal, target.GetAura(WarlockSpells.SOUL_LINK_BUFF).GetEffect(2).GetAmount());
                            args = new CastSpellExtraArgs(true);
                            args.SpellValueOverrides.Add(SpellValueMod.BasePoint0, (int)playerHeal);
                            args.SpellValueOverrides.Add(SpellValueMod.BasePoint1, (int)petHeal);

                            target.CastSpell(target, WarlockSpells.SOUL_LINK_HEAL, args);
                        }
                    }
                }

            }


        }
        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
