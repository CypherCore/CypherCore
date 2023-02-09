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
    //60561
    [CreatureScript(60561)]
    public class npc_earth_grab_totem : ScriptedAI
    {
        public npc_earth_grab_totem(Creature creature) : base(creature)
        {
        }

        public List<ObjectGuid> alreadyRooted = new List<ObjectGuid>();

        public override void Reset()
        {
            var time = TimeSpan.FromSeconds(2);

            me.m_Events.AddRepeatEventAtOffset(() =>
            {
                List<Unit> unitList = new List<Unit>();
                me.GetAttackableUnitListInRange(unitList, 10.0f);
                foreach (var target in unitList)
                {
                    if (target.HasAura(TotemSpells.SPELL_TOTEM_EARTH_GRAB_ROOT_EFFECT))
                    {
                        continue;
                    }

                    if (!alreadyRooted.Contains(target.GetGUID()))
                    {
                        alreadyRooted.Add(target.GetGUID());
                        me.CastSpell(target, TotemSpells.SPELL_TOTEM_EARTH_GRAB_ROOT_EFFECT, true);
                    }
                    else
                    {
                        me.CastSpell(target, TotemSpells.SPELL_TOTEM_EARTH_GRAB_SLOW_EFFECT, true);
                    }
                }

                return time;
            }, time);
        }
    }
}
