using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Hand of Guldan damage - 86040
    [SpellScript(86040)]
    internal class spell_warl_hand_of_guldan_damage : SpellScript, IHasSpellEffects
    {

        private int _soulshards = 1;

        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        public override bool Load()
        {
            _soulshards += GetCaster().GetPower(PowerType.SoulShards);
            if (_soulshards > 4)
            {
                GetCaster().SetPower(PowerType.SoulShards, 1);
                _soulshards = 4;

            }
            else
            {
                GetCaster().SetPower(PowerType.SoulShards, 0);
            }
            return true;
        }

        private void HandleOnHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Unit target = GetHitUnit();
                if (target != null)
                {
                    int dmg = GetHitDamage();
                    SetHitDamage(dmg * _soulshards);

                    if (caster.HasAura(WarlockSpells.HAND_OF_DOOM))
                    {
                        caster.CastSpell(target, WarlockSpells.DOOM, true);
                    }
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}
