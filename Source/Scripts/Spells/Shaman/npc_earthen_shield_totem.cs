using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    //100943
    [CreatureScript(100943)]
    public class npc_earthen_shield_totem : ScriptedAI
    {
        public npc_earthen_shield_totem(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            me.CastSpell(me, ShamanSpells.SPELL_SHAMAN_AT_EARTHEN_SHIELD_TOTEM, true);
        }
    }
}
