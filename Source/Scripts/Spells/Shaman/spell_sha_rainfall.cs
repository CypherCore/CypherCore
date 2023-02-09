using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Game.AI.SmartEvent;

namespace Scripts.Spells.Shaman
{
    // 215864 Rainfall
    [SpellScript(215864)]
    public class spell_sha_rainfall_SpellScript : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            WorldLocation pos = GetHitDest();

            if (pos != null)
            {
                GetCaster().SummonCreature(ShamanNpcs.NPC_RAINFALL, pos);
            }
        }
    }
}
