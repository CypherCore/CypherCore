using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 104318 - Fel Firebolt @ Wild Imp
    [SpellScript(104318)]
    public class spell_warlock_fel_firebolt_wild_imp : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            // "Increases damage dealt by your Wild Imps' Firebolt by 10%."
            Unit owner = GetCaster().GetOwner();
            if (owner != null)
            {
                var pct = owner.GetAuraEffectAmount(WarlockSpells.INFERNAL_FURNACE, 0);
                if (pct != 0)
                {
                    SetHitDamage(GetHitDamage() + MathFunctions.CalculatePct(GetHitDamage(), pct));
                }

                if (owner.HasAura(WarlockSpells.STOLEN_POWER))
                {
                    Aura aur = owner.AddAura(WarlockSpells.STOLEN_POWER_COUNTER, owner);
                    if (aur != null)
                    {
                        if (aur.GetStackAmount() == 100)
                        {
                            owner.CastSpell(owner, WarlockSpells.STOLEN_POWER_BUFF, true);
                            aur.Remove();
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}
