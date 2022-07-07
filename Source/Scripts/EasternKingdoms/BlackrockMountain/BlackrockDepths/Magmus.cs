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
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.Magmus
{
    struct SpellIds
    {
        //Magmus
        public const uint Fieryburst = 13900;
        public const uint Warstomp = 24375;

        //IronhandGuardian
        public const uint Goutofflame = 15529;
    }

    enum Phases
    {
        One = 1,
        Two = 2
    }

    [Script]
    class boss_magmus : ScriptedAI
    {
        Phases phase;

        public boss_magmus(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            InstanceScript instance = me.GetInstanceScript();
            if (instance != null)
                instance.SetData(DataTypes.TypeIronHall, (uint)EncounterState.InProgress);

            phase = Phases.One;
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.Fieryburst);
                task.Repeat(TimeSpan.FromSeconds(6));
            });
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(50, damage) && phase == Phases.One)
            {
                phase = Phases.Two;
                _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
                {
                    DoCastVictim(SpellIds.Warstomp);
                    task.Repeat(TimeSpan.FromSeconds(8));
                });
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }

        public override void JustDied(Unit killer)
        {
            InstanceScript instance = me.GetInstanceScript();
            if (instance != null)
            {
                instance.HandleGameObject(instance.GetGuidData(DataTypes.DataThroneDoor), true);
                instance.SetData(DataTypes.TypeIronHall, (uint)EncounterState.Done);
            }
        }
    }

    [Script]
    class npc_ironhand_guardianAI : ScriptedAI
    {
        InstanceScript _instance;
        bool _active;

        public npc_ironhand_guardianAI(Creature creature) : base(creature)
        {
            _instance = me.GetInstanceScript();
            _active = false;
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void UpdateAI(uint diff)
        {
            if (!_active)
            {
                if (_instance.GetData(DataTypes.TypeIronHall) == (uint)EncounterState.NotStarted)
                    return;
                // Once the boss is engaged, the guardians will stay activated until the next instance reset
                _scheduler.Schedule(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), task =>
                {
                    DoCastAOE(SpellIds.Goutofflame);
                    task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(21));
                });
                _active = true;
            }

            _scheduler.Update(diff);
        }
    }
}