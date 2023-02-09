using Framework.Constants;
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
    // 51490 - Thunderstorm
    [SpellScript(51490)]
    public class spell_sha_thunderstorm : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleKnockBack(uint effIndex)
        {
            // Glyph of Thunderstorm
            if (GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_GLYPH_OF_THUNDERSTORM))
            {
                PreventHitDefaultEffect(effIndex);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleKnockBack, 1, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
        }
    }
}
