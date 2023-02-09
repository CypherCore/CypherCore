using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_captain_cookie;

namespace Scripts.EasternKingdoms.Deadmines.Spells
{
    [SpellScript(89732)]
    public class spell_captain_cookie_nauseated : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();

        public void HandleScript(int UnnamedParameter)
        {
            if (!GetCaster() || !GetHitUnit())
            {
                return;
            }

            GetHitUnit().RemoveAuraFromStack(eSpell.SPELL_SETIATED);
            GetHitUnit().RemoveAuraFromStack(eSpell.SPELL_SETIATED_H);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        }
    }
}
