using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.Spells
{
    [SpellScript(new uint[]{ 89268, 89740, 90561, 90562, 90563, 90564, 90565, 90582, 90583, 90584, 90585, 90586 })]
    public class spell_captain_cookie_throw_food_targeting : SpellScript, IAfterHit
    {
        public void AfterHit()
        {
            if (!GetCaster() || !GetHitUnit())
            {
                return;
            }

            uint spellId = 0;

            SpellInfo spellInfo = GetSpellInfo();
            if (spellInfo != null ) 
			{
                spellId = (uint)spellInfo.GetEffect(0).BasePoints;
            }

            if (Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty()) != null)
            {
                return;
            }

            GetCaster().CastSpell(GetHitUnit(), spellId);
        }
    }
}
