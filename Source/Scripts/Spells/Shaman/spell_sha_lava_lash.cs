using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // 60103 - Lava Lash
    [SpellScript(60103)]
    public class spell_sha_lava_lash : SpellScript, ISpellOnHit
    {
        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        public void OnHit()
        {
            GetCaster().CastSpell(GetHitUnit(), ShamanSpells.SPELL_SHAMAN_LAVA_LASH_SPREAD_FLAME_SHOCK, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.MaxTargets, GetEffectValue()));

            GetCaster().RemoveAurasDueToSpell(ShamanSpells.SPELL_SHAMAN_HOT_HAND);

            Unit target = GetHitUnit();
            if (target == null)
            {
                return;
            }
            if (GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASHING_STORM_DUMMY) && GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASH_LIGTHNING_AURA))
            {
                GetCaster().CastSpell(target, ShamanSpells.SPELL_SHAMAN_CRASHING_LIGHTNING_DAMAGE, true);
            }

            if (GetCaster() && GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASH_LIGTHNING_AURA))
            {
                GetCaster().CastSpell(null, ShamanSpells.SPELL_SHAMAN_CRASH_LIGHTNING_PROC, true);
            }
        }
    }
}
