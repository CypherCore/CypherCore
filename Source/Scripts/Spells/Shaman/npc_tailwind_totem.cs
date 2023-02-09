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
    //NPC ID : 106321
    [CreatureScript(106321)]
    public class npc_tailwind_totem : ScriptedAI
    {
        public npc_tailwind_totem(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            var time = TimeSpan.FromSeconds(1);

            me.m_Events.AddRepeatEventAtOffset(() =>
            {
                me.CastSpell(me, TotemSpells.SPELL_TOTEM_TAIL_WIND_EFFECT, true);
                return time;
            }, time);
        }
    }
}
