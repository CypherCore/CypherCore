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
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.Karazhan.Midnight
{
    struct Misc
    {
        public const uint MountedDisplayid = 16040;

        //Attumen (@Todo Use The Summoning Spell Instead Of Creature Id. It Works; But Is Not Convenient For Us)
        public const uint SummonAttumen = 15550;
    }

    struct TextIds
    {
        public const uint SayMidnightKill = 0;
        public const uint SayAppear = 1;
        public const uint SayMount = 2;

        public const uint SayKill = 0;
        public const uint SayDisarmed = 1;
        public const uint SayDeath = 2;
        public const uint SayRandom = 3;
    }

    struct SpellIds
    {
        public const uint Shadowcleave = 29832;
        public const uint IntangiblePresence = 29833;
        public const uint BerserkerCharge = 26561;                   //Only When Mounted
    }

    [Script]
    public class boss_attumen : ScriptedAI
    {
        public boss_attumen(Creature creature) : base(creature)
        {
            CleaveTimer = RandomHelper.URand(10000, 15000);
            CurseTimer = 30000;
            RandomYellTimer = RandomHelper.URand(30000, 60000);              //Occasionally yell
            ChargeTimer = 20000;
            ResetTimer = 0;
        }

        public override void Reset()
        {
            ResetTimer = 0;
            Midnight.Clear();
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            base.EnterEvadeMode(why);
            ResetTimer = 2000;
        }

        public override void EnterCombat(Unit who) { }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            Unit midnight = Global.ObjAccessor.GetUnit(me, Midnight);
            if (midnight)
                midnight.KillSelf();
        }

        public override void UpdateAI(uint diff)
        {
            if (ResetTimer != 0)
            {
                if (ResetTimer <= diff)
                {
                    ResetTimer = 0;
                    Unit pMidnight = Global.ObjAccessor.GetUnit(me, Midnight);
                    if (pMidnight)
                    {
                        pMidnight.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                        pMidnight.SetVisible(true);
                    }
                    Midnight.Clear();
                    me.SetVisible(false);
                    me.KillSelf();
                }
                else ResetTimer -= diff;
            }

            //Return since we have no target
            if (!UpdateVictim())
                return;

            if (me.HasFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable))
                return;

            if (CleaveTimer <= diff)
            {
                DoCastVictim(SpellIds.Shadowcleave);
                CleaveTimer = RandomHelper.URand(10000, 15000);
            }
            else CleaveTimer -= diff;

            if (CurseTimer <= diff)
            {
                DoCastVictim(SpellIds.IntangiblePresence);
                CurseTimer = 30000;
            }
            else CurseTimer -= diff;

            if (RandomYellTimer <= diff)
            {
                Talk(TextIds.SayRandom);
                RandomYellTimer = RandomHelper.URand(30000, 60000);
            }
            else RandomYellTimer -= diff;

            if (me.GetUInt32Value(UnitFields.DisplayId) == Misc.MountedDisplayid)
            {
                if (ChargeTimer <= diff)
                {
                    var t_list = me.GetThreatManager().getThreatList();
                    List<Unit> target_list = new List<Unit>();
                    foreach (var hostileRefe in t_list)
                    {
                        var unit = Global.ObjAccessor.GetUnit(me, hostileRefe.getUnitGuid());
                        if (unit && !unit.IsWithinDist(me, SharedConst.AttackDistance, false))
                            target_list.Add(unit);
                        unit = null;
                    }
                    Unit target = null;
                    if (!target_list.Empty())
                        target = target_list.SelectRandom();

                    DoCast(target, SpellIds.BerserkerCharge);
                    ChargeTimer = 20000;
                }
                else ChargeTimer -= diff;
            }
            else
            {
                if (HealthBelowPct(25))
                {
                    Creature pMidnight = ObjectAccessor.GetCreature(me, Midnight);
                    if (pMidnight && pMidnight.IsTypeId(TypeId.Unit))
                    {
                        ((boss_midnight)pMidnight.GetAI()).Mount(me);
                        me.SetHealth(pMidnight.GetHealth());
                        DoResetThreat();
                    }
                }
            }

            DoMeleeAttackIfReady();
        }

        public override void SpellHit(Unit source, SpellInfo spell)
        {
            if (spell.Mechanic == Mechanics.Disarm)
                Talk(TextIds.SayDisarmed);
        }

        public ObjectGuid Midnight;
        uint CleaveTimer;
        uint CurseTimer;
        uint RandomYellTimer;
        uint ChargeTimer;                                     //only when mounted
        uint ResetTimer;
    }

    [Script]
    public class boss_midnight : ScriptedAI
    {
        public boss_midnight(Creature creature) : base(creature) { }

        public override void Reset()
        {
            Phase = 1;
            Attumen.Clear();
            mountTimer = 0;

            me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
            me.SetVisible(true);
        }

        public override void EnterCombat(Unit who) { }

        public override void KilledUnit(Unit victim)
        {
            if (Phase == 2)
            {
                Unit unit = Global.ObjAccessor.GetUnit(me, Attumen);
                if (unit)
                    Talk(TextIds.SayMidnightKill, unit);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (Phase == 1 && HealthBelowPct(95))
            {
                Phase = 2;
                Creature attumen = me.SummonCreature(Misc.SummonAttumen, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedOrDeadDespawn, 30000);
                if (attumen)
                {
                    Attumen = attumen.GetGUID();
                    attumen.GetAI().AttackStart(me.GetVictim());
                    SetMidnight(attumen, me.GetGUID());
                    Talk(TextIds.SayAppear, attumen);
                }
            }
            else if (Phase == 2 && HealthBelowPct(25))
            {
                Unit pAttumen = Global.ObjAccessor.GetUnit(me, Attumen);
                if (pAttumen)
                    Mount(pAttumen);
            }
            else if (Phase == 3)
            {
                if (mountTimer != 0)
                {
                    if (mountTimer <= diff)
                    {
                        mountTimer = 0;
                        me.SetVisible(false);
                        me.GetMotionMaster().MoveIdle();
                        Unit pAttumen = Global.ObjAccessor.GetUnit(me, Attumen);
                        if (pAttumen)
                        {
                            pAttumen.SetDisplayId(Misc.MountedDisplayid);
                            pAttumen.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                            if (pAttumen.GetVictim())
                            {
                                pAttumen.GetMotionMaster().MoveChase(pAttumen.GetVictim());
                                pAttumen.SetTarget(pAttumen.GetVictim().GetGUID());
                            }
                            pAttumen.SetObjectScale(1);
                        }
                    }
                    else mountTimer -= diff;
                }
            }

            if (Phase != 3)
                DoMeleeAttackIfReady();
        }

        public void Mount(Unit pAttumen)
        {
            Talk(TextIds.SayMount, pAttumen);
            Phase = 3;
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
            pAttumen.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
            float angle = me.GetAngle(pAttumen);
            float distance = me.GetDistance2d(pAttumen);
            float newX = me.GetPositionX() + (float)Math.Cos(angle) * (distance / 2);
            float newY = me.GetPositionY() + (float)Math.Sin(angle) * (distance / 2);
            float newZ = 50;
            me.GetMotionMaster().Clear();
            me.GetMotionMaster().MovePoint(0, newX, newY, newZ);
            distance += 10;
            newX = me.GetPositionX() + (float)Math.Cos(angle) * (distance / 2);
            newY = me.GetPositionY() + (float)Math.Sin(angle) * (distance / 2);
            pAttumen.GetMotionMaster().Clear();
            pAttumen.GetMotionMaster().MovePoint(0, newX, newY, newZ);
            mountTimer = 1000;
        }

        void SetMidnight(Creature pAttumen, ObjectGuid value)
        {
            ((boss_attumen)pAttumen.GetAI()).Midnight = value;
        }

        ObjectGuid Attumen;
        byte Phase;
        uint mountTimer;
    }
}
