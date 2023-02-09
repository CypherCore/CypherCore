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
    //NPC ID : 97369
    [CreatureScript(97369)]
    public class npc_liquid_magma_totem : ScriptedAI
    {
        public npc_liquid_magma_totem(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            var time = TimeSpan.FromSeconds(15);

            me.m_Events.AddRepeatEventAtOffset(() =>
            {
                me.CastSpell(me, TotemSpells.SPELL_TOTEM_LIQUID_MAGMA_EFFECT, true);
                return time;
            }, time);
        }
    }
}
