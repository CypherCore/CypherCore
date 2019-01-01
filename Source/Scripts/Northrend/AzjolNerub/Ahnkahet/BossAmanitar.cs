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
using System;

namespace Scripts.Northrend.AzjolNerub.Ahnkahet.Amanitar
{
    struct SpellIds
    {
        public const uint Bash = 57094; // Victim
        public const uint EntanglingRoots = 57095; // Random Victim 100y
        public const uint Mini = 57055; // Self
        public const uint VenomBoltVolley = 57088; // Random Victim 100y
        public const uint HealthyMushroomPotentFungus = 56648; // Killer 3y
        public const uint PoisonousMushroomPoisonCloud = 57061; // Self - Duration 8 Sec
        public const uint PoisonousMushroomVisualArea = 61566; // Self
        public const uint PoisonousMushroomVisualAura = 56741; // Self
        public const uint PutridMushroom = 31690; // To Make The Mushrooms Visible
        public const uint PowerMushroomVisualAura = 56740;
    }

    struct CreatureIds
    {
        public const uint Trigger = 19656;
        public const uint HealthyMushroom = 30391;
        public const uint PoisonousMushroom = 30435;
    }

    [Script]
    class boss_amanitar : BossAI
    {
        public boss_amanitar(Creature creature) : base(creature, DataTypes.Amanitar) { }

        public override void Reset()
        {
            _Reset();
            me.SetMeleeDamageSchool(SpellSchools.Nature);
            me.ApplySpellImmune(0, SpellImmunity.School, SpellSchoolMask.Nature, true);
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                SpawnAdds();
                task.Repeat(TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(9), task =>
            {
                DoCast(SelectTarget(SelectAggroTarget.Random, 0, 100, true), SpellIds.EntanglingRoots, true);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14), task =>
            {
                DoCastVictim(SpellIds.Bash);
                task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(12));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(18), task =>
            {
                DoCast(SpellIds.Mini);
                task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), task =>
            {
                DoCast(SelectTarget(SelectAggroTarget.Random, 0, 100, true), SpellIds.VenomBoltVolley, true);
                task.Repeat(TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(22));
            });
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.Mini);
        }

        void SpawnAdds()
        {
            int u = 0;

            for (byte i = 0; i < 30; ++i)
            {
                Position pos = me.GetRandomNearPosition(30.0f);
                me.UpdateGroundPositionZ(pos.GetPositionX(), pos.GetPositionY(), ref pos.posZ);

                Creature trigger = me.SummonCreature(CreatureIds.Trigger, pos);
                if (trigger)
                {
                    Creature temp1 = trigger.FindNearestCreature(CreatureIds.HealthyMushroom, 4.0f, true);
                    Creature temp2 = trigger.FindNearestCreature(CreatureIds.PoisonousMushroom, 4.0f, true);
                    if (!temp1 && !temp2)
                    {
                        u = 1 - u;
                        me.SummonCreature(u > 0 ? CreatureIds.PoisonousMushroom : CreatureIds.HealthyMushroom, pos, TempSummonType.TimedOrCorpseDespawn, 60 * Time.InMilliseconds);
                    }
                    trigger.DespawnOrUnsummon();
                }
            }
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

    [Script]
    class npc_amanitar_mushrooms : ScriptedAI
    {
        public npc_amanitar_mushrooms(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (me.GetEntry() == CreatureIds.PoisonousMushroom)
                {
                    DoCast(me, SpellIds.PoisonousMushroomVisualArea, true);
                    DoCast(me, SpellIds.PoisonousMushroomPoisonCloud);
                }
                task.Repeat(TimeSpan.FromSeconds(7));
            });

            me.SetDisplayFromModel(1);
            DoCast(SpellIds.PutridMushroom);

            if (me.GetEntry() == CreatureIds.PoisonousMushroom)
                DoCast(SpellIds.PoisonousMushroomVisualAura);
            else
                DoCast(SpellIds.PowerMushroomVisualAura);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (damage >= me.GetHealth() && me.GetEntry() == CreatureIds.HealthyMushroom)
                DoCast(me, SpellIds.HealthyMushroomPotentFungus, true);
        }

        public override void EnterCombat(Unit who) { }
        public override void AttackStart(Unit victim) { }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;
        }
    }
}
