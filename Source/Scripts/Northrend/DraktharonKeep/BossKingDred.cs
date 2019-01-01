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
using Game.Maps;
using Game.Scripting;
using System;

namespace Scripts.Northrend.DraktharonKeep.KingDred
{
    struct SpellIds
    {
        public const uint BellowingRoar = 22686; // Fears The Group; Can Be Resisted/Dispelled
        public const uint GrievousBite = 48920;
        public const uint ManglingSlash = 48873; // Cast On The Current Tank; Adds Debuf
        public const uint FearsomeRoar = 48849;
        public const uint PiercingSlash = 48878; // Debuff --> Armor Reduced By 75%
        public const uint RaptorCall = 59416; // Dummy
        public const uint GutRip = 49710;
        public const uint Rend = 13738;
    }

    struct Misc
    {
        public const int ActionRaptorKilled = 1;
        public const uint DataRaptorsKilled = 2;
    }

    [Script]
    class boss_king_dred : BossAI
    {
        public boss_king_dred(Creature creature) : base(creature, DTKDataTypes.KingDred)
        {
            Initialize();
        }

        void Initialize()
        {
            raptorsKilled = 0;
        }

        public override void Reset()
        {
            Initialize();
            _Reset();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(33), task =>
            {
                DoCastAOE(SpellIds.BellowingRoar);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.GrievousBite);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(18.5), task =>
            {
                DoCastVictim(SpellIds.ManglingSlash);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), task =>
            {
                DoCastAOE(SpellIds.FearsomeRoar);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCastVictim(SpellIds.PiercingSlash);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25), task =>
            {
                DoCastVictim(SpellIds.RaptorCall);

                float x, y, z;
                me.GetClosePoint(out x, out y, out z, me.GetObjectSize() / 3, 10.0f);
                me.SummonCreature(RandomHelper.RAND(DTKCreatureIds.DrakkariGutripper, DTKCreatureIds.DrakkariScytheclaw), x, y, z, 0, TempSummonType.DeadDespawn, 1000);
                task.Repeat();
            });
        }

        public override void DoAction(int action)
        {
            if (action == Misc.ActionRaptorKilled)
                ++raptorsKilled;
        }

        public override uint GetData(uint type)
        {
            if (type == Misc.DataRaptorsKilled)
                return raptorsKilled;

            return 0;
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
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

        byte raptorsKilled;
    }

    [Script]
    class npc_drakkari_gutripper : ScriptedAI
    {
        public npc_drakkari_gutripper(Creature creature) : base(creature)
        {
            Initialize();
            instance = me.GetInstanceScript();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.GutRip, false);
                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        public override void JustDied(Unit killer)
        {
            Creature dred = ObjectAccessor.GetCreature(me, instance.GetGuidData(DTKDataTypes.KingDred));
            if (dred)
                dred.GetAI().DoAction(Misc.ActionRaptorKilled);
        }

        InstanceScript instance;
    }

    [Script]
    class npc_drakkari_scytheclaw : ScriptedAI
    {
        public npc_drakkari_scytheclaw(Creature creature) : base(creature)
        {
            Initialize();
            instance = me.GetInstanceScript();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.Rend, false);
                task.Repeat();
            });
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            Initialize();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        public override void JustDied(Unit killer)
        {
            Creature dred = ObjectAccessor.GetCreature(me, instance.GetGuidData(DTKDataTypes.KingDred));
            if (dred)
                dred.GetAI().DoAction(Misc.ActionRaptorKilled);
        }

        InstanceScript instance;
    }

    [Script]
    class achievement_king_dred : AchievementCriteriaScript
    {
        public achievement_king_dred() : base("achievement_king_dred") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            Creature dred = target.ToCreature();
            if (dred)
                if (dred.GetAI().GetData(Misc.DataRaptorsKilled) >= 6)
                    return true;

            return false;
        }
    }
}
