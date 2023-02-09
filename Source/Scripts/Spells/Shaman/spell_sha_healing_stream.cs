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
    [SpellScript(52042)]
    public class spell_sha_healing_stream : SpellScript, ISpellOnHit
    {
        public override bool Validate(SpellInfo UnnamedParameter)
        {
            if (Global.SpellMgr.GetSpellInfo(ShamanSpells.SPELL_SHAMAN_HEALING_STREAM, Difficulty.None) != null)
            {
                return false;
            }
            return true;
        }

        public void OnHit()
        {
            if (!GetCaster().GetOwner())
            {
                return;
            }

            Player _player = GetCaster().GetOwner().ToPlayer();
            if (_player != null)
            {
                Unit target = GetHitUnit();
                if (target != null)
                {
                    // Glyph of Healing Stream Totem
                    if (target.GetGUID() != _player.GetGUID() && _player.HasAura(ShamanSpells.SPELL_SHAMAN_GLYPH_OF_HEALING_STREAM_TOTEM))
                    {
                        _player.CastSpell(target, ShamanSpells.SPELL_SHAMAN_GLYPH_OF_HEALING_STREAM, true);
                    }
                }
            }
        }
    }
}
