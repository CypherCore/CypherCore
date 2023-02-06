using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Warlock
{
    [SpellScript(17962)]
    public class spell_warl_conflagrate_aura : SpellScript
    {
        public void OnHit()
        {
            Player _player = GetCaster().ToPlayer();
            if (_player != null)
            {
                Unit target = GetHitUnit();
                if (target != null)
                {
                    if (!target.HasAura(WarlockSpells.IMMOLATE) && !_player.HasAura(WarlockSpells.GLYPH_OF_CONFLAGRATE))
                    {
                        if (target.GetAura(WarlockSpells.CONFLAGRATE) != null)
                        {
                            target.RemoveAura(WarlockSpells.CONFLAGRATE);
                        }
                    }

                    if (!target.HasAura(WarlockSpells.IMMOLATE_FIRE_AND_BRIMSTONE))
                    {
                        target.RemoveAura(WarlockSpells.CONFLAGRATE_FIRE_AND_BRIMSTONE);
                    }
                }
            }
        }
    }
}
