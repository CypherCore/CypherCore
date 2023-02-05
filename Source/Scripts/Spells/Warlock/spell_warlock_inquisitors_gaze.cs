using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bgs.Protocol.Notification.V1;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.SPELL_INQUISITORS_GAZE)]
    public class spell_warlock_inquisitors_gaze : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();

        private void HandleOnHit(uint effectIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                int damage = (GetCaster().SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * 15 * 16) / 100;
                GetCaster().CastSpell(target, WarlockSpells.SPELL_INQUISITORS_GAZE_EFFECT, new CastSpellExtraArgs(SpellValueMod.BasePoint0, damage));
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

}
