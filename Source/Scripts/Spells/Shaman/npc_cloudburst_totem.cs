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
    //NPC ID : 78001
    [CreatureScript(78001)]
    public class npc_cloudburst_totem : ScriptedAI
    {
        public npc_cloudburst_totem(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            if (me.GetOwner())
            {
                me.CastSpell(me.GetOwner(), TotemSpells.SPELL_TOTEM_CLOUDBURST_EFFECT, true);
            }
        }
    }
}
