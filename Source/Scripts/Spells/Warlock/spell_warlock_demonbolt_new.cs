using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Warlock
{
    // 264178 - Demonbolt
    [SpellScript(264178)]
    public class spell_warlock_demonbolt_new : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            if (GetCaster())
            {
                GetCaster().CastSpell(GetCaster(), WarlockSpells.DEMONBOLT_ENERGIZE, true);
                GetCaster().CastSpell(GetCaster(), WarlockSpells.DEMONBOLT_ENERGIZE, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
        }
    }
}
