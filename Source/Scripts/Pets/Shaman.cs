/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
    [Script]
    class npc_pet_shaman_earth_elemental : ScriptedAI
    {
        public npc_pet_shaman_earth_elemental(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventAngeredEarth, 0);
            me.ApplySpellImmune(0, SpellImmunity.School, SpellSchoolMask.Nature, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (_events.ExecuteEvent() == EventAngeredEarth)
            {
                DoCastVictim(SpellAngeredEarth);
                _events.ScheduleEvent(EventAngeredEarth, RandomHelper.URand(5000, 20000));
            }

            DoMeleeAttackIfReady();
        }

        const int EventAngeredEarth = 1;
        const uint SpellAngeredEarth = 36213;
    }

    [Script]
    public class npc_pet_shaman_fire_elemental : ScriptedAI
    {
        public npc_pet_shaman_fire_elemental(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventFireNova, RandomHelper.URand(5000, 20000));
            _events.ScheduleEvent(EventFireBlast, RandomHelper.URand(5000, 20000));
            _events.ScheduleEvent(EventFireShield, 0);
            me.ApplySpellImmune(0, SpellImmunity.School, SpellSchoolMask.Fire, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventFireNova:
                        DoCastVictim(SpellFireNova);
                        _events.ScheduleEvent(EventFireNova, RandomHelper.URand(5000, 20000));
                        break;
                    case EventFireShield:
                        DoCastVictim(SpellFireShield);
                        _events.ScheduleEvent(EventFireShield, 2000);
                        break;
                    case EventFireBlast:
                        DoCastVictim(SpellFireBlast);
                        _events.ScheduleEvent(EventFireBlast, RandomHelper.URand(5000, 20000));
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }

        const int EventFireNova = 1;
        const int EventFireShield = 2;
        const int EventFireBlast = 3;

        const uint SpellFireBlast = 57984;
        const uint SpellFireNova = 12470;
        const uint SpellFireShield = 13376;
    }
}
