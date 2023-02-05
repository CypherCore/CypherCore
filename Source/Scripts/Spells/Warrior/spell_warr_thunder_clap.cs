using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
    // 6343 - Thunder Clap
    [SpellScript(6343)]
    public class spell_warr_thunder_clap : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Player _player = GetCaster().ToPlayer();
            if (_player != null)
            {
                Unit target = GetHitUnit();
                if (target != null)
                {
                    _player.CastSpell(target, WarriorSpells.WEAKENED_BLOWS, true);

                    if (_player.HasAura(WarriorSpells.THUNDERSTRUCK))
                    {
                        _player.CastSpell(target, WarriorSpells.THUNDERSTRUCK_STUN, true);
                    }
                }
            }
        }
    }
}
