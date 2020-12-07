/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using System;

namespace Scripts.Pets
{
    namespace Shaman
    {
        struct SpellIds
        {
            //npc_pet_shaman_earth_elemental
            public const uint AngeredEarth = 36213;

            //npc_pet_shaman_fire_elemental
            public const uint FireBlast = 57984;
            public const uint FireNova = 12470;
            public const uint FireShield = 13376;
        }

        [Script]
        class npc_pet_shaman_earth_elemental : ScriptedAI
        {
            public npc_pet_shaman_earth_elemental(Creature creature) : base(creature) { }

            public override void Reset()
            {
                _scheduler.CancelAll();
                _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
                {
                    DoCastVictim(SpellIds.AngeredEarth);
                    task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                });
                me.ApplySpellImmune(0, SpellImmunity.School, SpellSchoolMask.Nature, true);
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _scheduler.Update(diff);

                DoMeleeAttackIfReady();
            }
        }

        [Script]
        public class npc_pet_shaman_fire_elemental : ScriptedAI
        {
            public npc_pet_shaman_fire_elemental(Creature creature) : base(creature) { }

            public override void Reset()
            {
                _scheduler.CancelAll();
                _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
                {
                    DoCastVictim(SpellIds.FireNova);
                    task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
                {
                    DoCastVictim(SpellIds.FireShield);
                    task.Repeat(TimeSpan.FromSeconds(2));
                });
                _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
                {
                    DoCastVictim(SpellIds.FireBlast);
                    task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
                });
                me.ApplySpellImmune(0, SpellImmunity.School, SpellSchoolMask.Fire, true);
            }

            public override void UpdateAI(uint diff)
            {
                if (!UpdateVictim())
                    return;

                _scheduler.Update(diff);

                if (me.HasUnitState(UnitState.Casting))
                    return;

                DoMeleeAttackIfReady();
            }
        }
    }
}

