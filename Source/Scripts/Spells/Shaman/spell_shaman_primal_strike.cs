using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
    //73899
    [SpellScript(73899)]
    public class spell_shaman_primal_strike : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
                GetHitUnit().CastSpell(GetHitUnit(), 73899, (int)(player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.34f));
        }
    }
}
