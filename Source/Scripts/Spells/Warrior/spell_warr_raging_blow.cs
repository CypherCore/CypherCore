using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{

    // 85288
    [SpellScript(85288)]
    public class spell_warr_raging_blow : SpellScript, ISpellOnHit
    {
        private byte _targetHit;

        public void OnHit()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                player.CastSpell(player, WarriorSpells.ALLOW_RAGING_BLOW, true);
            }

            if (GetCaster().HasAura(WarriorSpells.BATTLE_TRANCE))
            {
                Unit target = GetCaster().ToPlayer().GetSelectedUnit();
                ObjectGuid targetGUID = target.GetGUID();
                _targetHit++;

                if (this._targetHit == 4)
                {
                    //targetGUID.Clear();
                    this._targetHit = 0;
                    GetCaster().CastSpell(null, WarriorSpells.BATTLE_TRANCE_BUFF, true);
                    AuraEffect battleTrance = GetCaster().GetAura(WarriorSpells.BATTLE_TRANCE_BUFF).GetEffect(0);
                    if (battleTrance != null)
                    {
                        battleTrance.GetAmount();
                    }
                }
            }

            if (RandomHelper.randChance(20))
            {
                GetCaster().GetSpellHistory().ResetCooldown(85288, true);
            }

            Aura whirlWind = GetCaster().GetAura(WarriorSpells.WHIRLWIND_PASSIVE);
            if (whirlWind != null)
            {
                whirlWind.ModStackAmount(-1, AuraRemoveMode.Default, false);
            }
        }


    }
}
