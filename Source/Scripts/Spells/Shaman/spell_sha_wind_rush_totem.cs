using Game.Scripting;
using System;
using Game.AI;
using Game.Entities;

namespace Scripts.Spells.Shaman
{
    //192077 - Wind Rush Totem
    //97285 - NPC ID
    [CreatureScript(97285)]
    public class spell_sha_wind_rush_totem : ScriptedAI
    {
        public spell_sha_wind_rush_totem(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            var time = TimeSpan.FromSeconds(1);

            me.m_Events.AddRepeatEventAtOffset(() =>
            {
                me.CastSpell(me, TotemSpells.SPELL_TOTEM_WIND_RUSH_EFFECT, true);
                return time;
            }, time);
        }
    }
}
