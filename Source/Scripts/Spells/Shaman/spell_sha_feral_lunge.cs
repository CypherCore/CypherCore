using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // Feral Lunge - 196884
    [SpellScript(196884)]
    public class spell_sha_feral_lunge : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            if (Global.SpellMgr.GetSpellInfo(ShamanSpells.SPELL_SHAMAN_FERAL_LUNGE_DAMAGE, Difficulty.None) != null)
            {
                return false;
            }
            return true;
        }

        private void HandleDamage(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            caster.CastSpell(target, ShamanSpells.SPELL_SHAMAN_FERAL_LUNGE_DAMAGE, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }
}
