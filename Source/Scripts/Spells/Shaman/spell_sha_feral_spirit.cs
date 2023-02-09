using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // 51533 - Feral Spirit
    [SpellScript(51533)]
    public class spell_sha_feral_spirit : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint UnnamedParameter)
        {
            Unit caster = GetCaster();

            caster.CastSpell(GetHitUnit(), ShamanSpells.SPELL_SHAMAN_FERAL_SPIRIT_SUMMON, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }
}
