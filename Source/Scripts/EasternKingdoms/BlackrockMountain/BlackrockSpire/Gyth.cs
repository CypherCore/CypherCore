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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Gyth
{
    struct SpellIds
    {
        public const uint RendMounts = 16167; // Change model
        public const uint CorrosiveAcid = 16359; // Combat (self cast)
        public const uint Flamebreath = 16390; // Combat (Self cast)
        public const uint Freeze = 16350; // Combat (Self cast)
        public const uint KnockAway = 10101; // Combat
        public const uint SummonRend = 16328;  // Summons Rend near death
    }

    struct MiscConst
    {
        public const uint NefariusPath2 = 1379671;
        public const uint NefariusPath3 = 1379672;
        public const uint GythPath1 = 1379681;
    }

    [Script]
    class boss_gyth : BossAI
    {
        bool SummonedRend;

        public boss_gyth(Creature creature) : base(creature, DataTypes.Gyth)
        {
            Initialize();
        }

        void Initialize()
        {
            SummonedRend = false;
        }

        public override void Reset()
        {
            Initialize();
            if (instance.GetBossState(DataTypes.Gyth) == EncounterState.InProgress)
            {
                instance.SetBossState(DataTypes.Gyth, EncounterState.Done);
                me.DespawnOrUnsummon();
            }
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16), task =>
            {
                DoCast(me, SpellIds.CorrosiveAcid);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16), task =>
            {
                DoCast(me, SpellIds.Freeze);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16), task =>
            {
                DoCast(me, SpellIds.Flamebreath);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(18), task =>
            {
                DoCastVictim(SpellIds.KnockAway);
                task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(20));
            });
        }

        public override void JustDied(Unit killer)
        {
            instance.SetBossState(DataTypes.Gyth, EncounterState.Done);
        }

        public override void SetData(uint type, uint data)
        {
            switch (data)
            {
                case 1:
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                    {
                        me.AddAura(SpellIds.RendMounts, me);
                        GameObject portcullis = me.FindNearestGameObject(GameObjectsIds.DrPortcullis, 40.0f);
                        if (portcullis)
                            portcullis.UseDoorOrButton();
                        Creature victor = me.FindNearestCreature(CreaturesIds.LordVictorNefarius, 75.0f, true);
                        if (victor)
                            victor.GetAI().SetData(1, 1);

                        task.Schedule(TimeSpan.FromSeconds(2), summonTask2 =>
                        {
                            me.GetMotionMaster().MovePath(MiscConst.GythPath1, false);
                        });
                    });
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!SummonedRend && HealthBelowPct(5))
            {
                DoCast(me, SpellIds.SummonRend);
                me.RemoveAura(SpellIds.RendMounts);
                SummonedRend = true;
            }

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

