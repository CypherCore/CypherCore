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
    // 5394 - Healing Stream Totem
    [SpellScript(5394)]
    public class spell_sha_healing_stream_totem : SpellScript, ISpellAfterCast
    {
        public void AfterCast()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(ShamanSpells.SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER))
            {
                AuraEffect auraeffx = caster.GetAura(ShamanSpells.SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER).GetEffect(0);
                int amount = auraeffx.GetAmount();
                caster.CastSpell(caster, ShamanSpells.SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER_AURA, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, amount));
            }
        }
    }
}
